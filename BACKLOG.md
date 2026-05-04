# Backlog

Future ideas — not committed to a date.

## Notify members of unclaimed results

When new race results land in the database (via the FarmResults
scrapers), attempt to match each result row against existing
members and email any matched member inviting them to claim the
result against their profile.

### Requirements

- **Match heuristic**: forename + surname is a starting point but
  collides on common names. Tighten with previously-claimed races
  at the same event (recurring swimmers), age bracket, or
  last-seen race number where available.
- **Opt-in, not opt-out**: add a checkbox to the signup flow
  ("notify me by email when a result might be mine to claim").
  Default to off — members must tick it. Store the preference and
  the signup-form opt-in timestamp on the member record (useful
  evidence if a complaint ever lands).
- **Existing members** (those who signed up before this feature
  ships) get a one-time email when the feature launches: "this is
  launching, do you want notifications?" with an opt-in link. If
  they don't click the link, never email them about this again.
  This single touch is permitted under existing-relationship
  precedent in the Spam Act; getting it right matters because we
  only get one shot. Track which existing members were emailed
  (date sent) and which clicked-to-opt-in.
- **Unsubscribe / preferences link**: every notification email must
  include a working unsubscribe (or "manage notifications") link.
  Use a per-message token, not the user id in the URL. Honour
  unsubscribes permanently — a member who's opted out should not
  be re-notified even if they later have new claimable results,
  unless they re-opt-in via the preferences page.
- **Throttling / digest**: don't send one email per race. If a
  member has multiple unclaimed results across recent scrapes,
  fold them into a single digest. Cap at e.g. one email per
  member per 24h.
- **False-positive safety**: tell the member "we think this might
  be you" rather than asserting the match. Show race name, date,
  time, and any other distinguishing fields so they can confirm
  before claiming.
- **Dry-run mode**: log the candidate emails to a table first
  rather than sending live, so we can eyeball the matches for a
  few weeks before turning on real send.
- **Suppression list**: bounces, complaints, hard fails — never
  retry. Probably keep this in the same table as unsubscribes.

### Trigger logic — ongoing notification job

Scheduled OceanSwimmer-side job, not a scraper-push, so the email
system can be down without affecting the scraper. Default cadence
nightly: a 9pm Sunday run catches that day's races; daily
afterwards mops up mid-week loads. An in-process
`IHostedService` / `BackgroundService` is enough for a single-
server deployment; the alternative is an external cron hitting an
admin-only endpoint, which gives you a manual trigger for testing
at the cost of more moving parts.

For each user where `NotifyUnclaimedResults = 1`:

```sql
SELECT o.oceanswimsid, r.RaceDescription, r.RaceDate,
       o.RaceTime, o.OverallPosition
FROM dbo.OceanSwims o
JOIN dbo.Race r        ON r.raceid = o.raceid
WHERE o.Forename = @forename
  AND o.Surname  = @surname
  AND NOT EXISTS (
      SELECT 1 FROM auth.AthleteResults ar
      WHERE ar.UserId = @userId AND ar.OceanSwimsId = o.oceanswimsid)
  AND o.datecreated > COALESCE(@lastNotifiedAt, @optedInAt)
ORDER BY r.RaceDate DESC;
```

If 0 candidates: skip the user. If 1+: one digest email listing
all of them, then `UPDATE auth.Users SET NotifyLastSentAt =
GETUTCDATE()`. Cap at one email per user per 24h.

The `COALESCE(@lastNotifiedAt, @optedInAt)` lower bound means a
brand-new opt-in only hears about results loaded *after* they
consented — no historical-backlog dump on the first email. If we
ever decide we DO want backfill on first opt-in, swap the COALESCE
for just `@lastNotifiedAt` and treat NULL as beginning of time.

### Schema — auth.Users

Already in place (migrations 003, 004):

- `NotifyUnclaimedResults  bit       NOT NULL DEFAULT 0`
- `NotifyOptedInAt         datetime2 NULL`
- `NotifyLastSentAt        datetime2 NULL`

Still to add when the launch email is built (migration 005+):

- `LaunchEmailSentAt   datetime2     NULL` — when we sent the
   one-time launch email; non-NULL means we've already had our one
   shot, never re-send.
- `LaunchOptInToken    nvarchar(100) NULL` — random token in the
   launch email link, NULL'd out on click.

### Email content

#### A. Launch email — one-time, to existing members

Sent once per existing member when the feature ships. They click
the link to opt in. If they don't click, they're never emailed
about this again.

**Subject**: `Get a heads-up when your race results land?`

**Plain text**:

```
Hi {Forename or "there"},

When we load fresh race results into OceanSwimmer, we can spot
which ones look like they might be yours and send you a quick
"this looks like you" email so you don't have to keep checking
back.

It'd typically be a few emails a year — after big swim weekends.

Want this on?

  Yes, email me about possible matches:
  {baseUrl}/auth/launch-opt-in?token={token}

If you'd rather not, just ignore this email. We won't ask again,
and we won't email you about this regardless.

You can change your mind any time at {baseUrl}/account.html.

— The OceanSwimmer team
```

**HTML**: same copy, render the link as the standard blue button
(`background:#0066cc;color:#fff;padding:12px 24px;border-
radius:6px`) used by the existing verification and password-reset
emails — see `SendVerificationEmailAsync` in `Program.cs` for the
exact pattern.

**Why no unsubscribe footer**: this email is *asking for* consent,
so the recipient hasn't yet opted into a stream — the "ignore this
and we won't ask again" line is the unsubscribe equivalent.

**Endpoint behaviour for `/auth/launch-opt-in?token=...`**:

- Look up the user by `LaunchOptInToken`. Unknown token → friendly
  "this link has already been used or has expired" page.
- Found: `UPDATE auth.Users SET NotifyUnclaimedResults = 1,
  NotifyOptedInAt = COALESCE(NotifyOptedInAt, GETUTCDATE()),
  LaunchOptInToken = NULL WHERE UserId = @userId`. Show a "Thanks
  — we'll email you when we spot a likely match" confirmation
  page with a link to `/account.html`.
- Token is NULL'd on click, so the link is one-shot.

#### B. Notification digest email — ongoing, to opted-in members

Triggered by the scheduled job above. Sent at most once per 24h
per member. Lists all unclaimed candidate results loaded since the
member's last notification.

Not yet drafted in detail. Key requirements when we get there:

- Personal greeting using `SwimmerForename` if available.
- Frame as "we think these might be yours" rather than asserting
  the match — the forename+surname heuristic has false positives
  on common names.
- Each candidate result shows race name, date, time, position, and
  a one-click claim link (plus a "not me" path so a wrong match
  trains the suppression).
- Footer: "manage notifications" link to `/account.html`. No per-
  email unsubscribe token needed — they're an authenticated member
  managing a preference, not a marketing-list recipient.

### Origin

Captured 2026-05-03 while adding the SportsEventServices and
Racesplitter scrapers in FarmResults. The author noted he had a
claimable result at South Curl Curl that day and would have liked
to be notified; another member exists in the system who didn't
swim today but should similarly get notified the next time a
result appears matching their profile.
