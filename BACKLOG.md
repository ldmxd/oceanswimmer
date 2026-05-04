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
- **Unsubscribe**: every email must include a working unsubscribe
  link. Use a per-message token, not the user id in the URL.
  Honour unsubscribes permanently (a member who's opted out should
  not be re-notified even if they later have new claimable
  results).
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
