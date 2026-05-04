-- ============================================================
-- Migration: add unclaimed-results notification preference to auth.Users.
--
-- NotifyUnclaimedResults
--     bit, NOT NULL, default 0. Member's preference for receiving
--     emails when a freshly-scraped race result might be theirs to
--     claim. Default 0 so existing members start opted-out — they
--     get a one-time launch email asking them to opt in, and
--     fresh signups can tick a checkbox at registration.
--
-- NotifyOptedInAt
--     datetime2, nullable. Stamped the moment NotifyUnclaimedResults
--     transitions 0 -> 1 (signup checkbox or click-through on the
--     launch email). Useful proof-of-consent if a Spam Act complaint
--     ever lands. Leave set even if the member later opts out, so
--     the historical record of "yes they did opt in on this date"
--     is preserved.
--
-- Run once against your RaceResult database.
-- ============================================================

ALTER TABLE auth.Users
    ADD NotifyUnclaimedResults bit       NOT NULL DEFAULT 0,
        NotifyOptedInAt        datetime2 NULL;
