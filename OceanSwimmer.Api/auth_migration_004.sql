-- ============================================================
-- Migration: add notification high-water mark to auth.Users.
--
-- NotifyLastSentAt
--     datetime2, nullable. Stamped each time we send a member a
--     "results to claim" notification email. The next run of the
--     notification job uses this as a lower bound on which
--     OceanSwims rows to consider, so members never get notified
--     about the same result twice. NULL means we've never
--     contacted them — the first notification falls back to using
--     NotifyOptedInAt as the lower bound (no historical-backlog
--     dump on first email).
--
-- Run once against your RaceResult database.
-- ============================================================

ALTER TABLE auth.Users
    ADD NotifyLastSentAt datetime2 NULL;
