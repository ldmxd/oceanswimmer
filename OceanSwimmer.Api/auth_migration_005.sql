-- ============================================================
-- Migration: add auth.LoginLog table.
--
-- One row per login attempt (successful or failed). Useful for
-- spotting brute-force attempts, supporting "last login" displays,
-- and answering "did this person actually log in last week?".
--
-- UserId
--     int, nullable. The auth.Users.UserId of the account that was
--     logged into. Nullable so we can still record failed password
--     attempts where the email didn't match any user (we don't want
--     to silently drop those — they're the most interesting from a
--     security standpoint).
--
-- Email
--     nvarchar(256), nullable. The email address that was supplied
--     (for password / email-verification logins) or returned by the
--     OAuth provider (Google / Facebook). Stored even on failure so
--     we can see what email the attacker was trying.
--
-- AuthProvider
--     nvarchar(50), required. 'Google', 'Facebook', 'Local', or
--     'EmailVerification' (the auto-sign-in that happens when a
--     user clicks the verify-email link).
--
-- Success
--     bit, required. 1 if the login completed and a cookie was
--     issued, 0 if the attempt failed.
--
-- FailureReason
--     nvarchar(200), nullable. Free-text reason when Success = 0
--     (e.g. 'invalid-password', 'unverified-email', 'no-such-user').
--     NULL when Success = 1.
--
-- IpAddress
--     nvarchar(64), nullable. Remote IP captured from
--     HttpContext.Connection.RemoteIpAddress (after forwarded-headers
--     middleware, so this is the real client IP, not Cloudflare's).
--     64 chars to comfortably fit IPv6 + scope.
--
-- UserAgent
--     nvarchar(500), nullable. Browser User-Agent header, truncated
--     to 500 chars. Useful for distinguishing "logged in on phone"
--     from "logged in on laptop" when investigating account misuse.
--
-- LoggedAt
--     datetime2, required, default GETUTCDATE(). UTC timestamp of
--     the login attempt.
--
-- Run once against your RaceResult database.
-- ============================================================

CREATE TABLE auth.LoginLog (
    LogId         bigint        IDENTITY(1,1) NOT NULL CONSTRAINT PK_LoginLog PRIMARY KEY,
    UserId        int           NULL,
    Email         nvarchar(256) NULL,
    AuthProvider  nvarchar(50)  NOT NULL,
    Success       bit           NOT NULL,
    FailureReason nvarchar(200) NULL,
    IpAddress     nvarchar(64)  NULL,
    UserAgent     nvarchar(500) NULL,
    LoggedAt      datetime2     NOT NULL CONSTRAINT DF_LoginLog_LoggedAt DEFAULT GETUTCDATE()
);

-- "Show me the last 20 logins for user X" / "did user X log in this week?"
CREATE INDEX IX_LoginLog_UserId_LoggedAt ON auth.LoginLog (UserId, LoggedAt DESC);

-- "Show me all login attempts in the last hour" (security monitoring)
CREATE INDEX IX_LoginLog_LoggedAt ON auth.LoginLog (LoggedAt DESC);
