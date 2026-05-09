-- ============================================================
-- Migration: add dbo.SearchLog table.
--
-- One row per call to a /swims/search* endpoint, regardless of
-- whether the caller is logged in. Useful for understanding which
-- swimmers / races are getting looked up most, spotting bot traffic,
-- and seeing what an anonymous user searched for before they decided
-- to register.
--
-- UserId
--     int, nullable. auth.Users.UserId of the caller if signed in,
--     NULL for anonymous searches.
--
-- SearchType
--     nvarchar(50), required. Which endpoint was hit:
--     'swims' (the main /swims/search) or 'swims-similar'
--     (the phonetic /swims/search-similar). Adding a column here
--     instead of separate tables keeps reporting easy.
--
-- Forename / Surname / Race / RaceId / Category / Gender / Page / PageSize
--     The raw query parameters as supplied (NOT the wildcarded
--     SQL form), so that ad-hoc reporting reads the way a human
--     entered the search.
--
-- ResultCount
--     int, nullable. Number of rows returned. Lets us spot empty
--     searches (UX problem) and runaway result sets.
--
-- IpAddress / UserAgent
--     Same shape as auth.LoginLog. UserAgent truncated to 500.
--
-- LoggedAt
--     datetime2, required, default GETUTCDATE().
--
-- Run once against your RaceResult database.
-- ============================================================

CREATE TABLE dbo.SearchLog (
    LogId       bigint        IDENTITY(1,1) NOT NULL CONSTRAINT PK_SearchLog PRIMARY KEY,
    UserId      int           NULL,
    SearchType  nvarchar(50)  NOT NULL,
    Forename    nvarchar(200) NULL,
    Surname     nvarchar(200) NULL,
    Race        nvarchar(200) NULL,
    RaceId      int           NULL,
    Category    nvarchar(50)  NULL,
    Gender      nvarchar(20)  NULL,
    Page        int           NULL,
    PageSize    int           NULL,
    ResultCount int           NULL,
    IpAddress   nvarchar(64)  NULL,
    UserAgent   nvarchar(500) NULL,
    LoggedAt    datetime2     NOT NULL CONSTRAINT DF_SearchLog_LoggedAt DEFAULT GETUTCDATE()
);

-- "What did this user search for recently?"
CREATE INDEX IX_SearchLog_UserId_LoggedAt ON dbo.SearchLog (UserId, LoggedAt DESC);

-- "What's been searched site-wide in the last hour / day?"
CREATE INDEX IX_SearchLog_LoggedAt ON dbo.SearchLog (LoggedAt DESC);
