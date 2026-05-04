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

### Trigger / scheduling

Prefer a scheduled OceanSwimmer-side job that periodically scans
for new race rows and unmatched-but-claimable results, rather than
having the scraper push events. Keeps FarmResults deterministic
and unaffected by email-system outages.

### Origin

Captured 2026-05-03 while adding the SportsEventServices and
Racesplitter scrapers in FarmResults. The author noted he had a
claimable result at South Curl Curl that day and would have liked
to be notified; another member exists in the system who didn't
swim today but should similarly get notified the next time a
result appears matching their profile.
