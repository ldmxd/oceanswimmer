-- ============================================================
-- Migration: add email/password auth columns to auth.Users
-- Run once against your RaceResult database.
-- ============================================================

ALTER TABLE auth.Users
    ADD PasswordHash           nvarchar(255) NULL,
        EmailVerified          bit           NOT NULL DEFAULT 1,   -- default 1 so existing Google/Facebook users are unaffected
        VerificationToken      nvarchar(100) NULL,
        VerificationTokenExpiry datetime2    NULL;

-- Confirm existing rows are marked verified (they should be via DEFAULT above, but belt-and-braces)
UPDATE auth.Users
SET EmailVerified = 1
WHERE AuthProvider IN ('Google', 'Facebook');
