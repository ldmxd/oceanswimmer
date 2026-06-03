-- ============================================================
-- Podium Leaderboard tables + stored procedure
-- Run this once to set up the schema, then the background
-- service (LeaderboardRefreshService) will call
-- sp_PopulatePodiumLeaderboards each Sunday night.
-- ============================================================

USE [RaceResult];
GO

-- 1. Pre-computed all-time podium table
IF OBJECT_ID('dbo.PodiumLeaderboardAllTime', 'U') IS NOT NULL
    DROP TABLE dbo.PodiumLeaderboardAllTime;

CREATE TABLE dbo.PodiumLeaderboardAllTime
(
    Forename        NVARCHAR(100)   NULL,
    Surname         NVARCHAR(100)   NULL,
    FullName        NVARCHAR(201)   NOT NULL,
    Gender          NVARCHAR(10)    NULL,
    TotalSwims      INT             NOT NULL DEFAULT 0,
    Firsts          INT             NOT NULL DEFAULT 0,
    Seconds         INT             NOT NULL DEFAULT 0,
    Thirds          INT             NOT NULL DEFAULT 0,
    TopTens         INT             NOT NULL DEFAULT 0,
    AvgPercentile   FLOAT           NULL,
    HasOverallPodium BIT            NOT NULL DEFAULT 0
);

-- 2. Pre-computed seasonal podium table
IF OBJECT_ID('dbo.PodiumLeaderboardSeasonal', 'U') IS NOT NULL
    DROP TABLE dbo.PodiumLeaderboardSeasonal;

CREATE TABLE dbo.PodiumLeaderboardSeasonal
(
    Season          INT             NOT NULL,
    Forename        NVARCHAR(100)   NULL,
    Surname         NVARCHAR(100)   NULL,
    FullName        NVARCHAR(201)   NOT NULL,
    Gender          NVARCHAR(10)    NULL,
    TotalSwims      INT             NOT NULL DEFAULT 0,
    Firsts          INT             NOT NULL DEFAULT 0,
    Seconds         INT             NOT NULL DEFAULT 0,
    Thirds          INT             NOT NULL DEFAULT 0,
    TopTens         INT             NOT NULL DEFAULT 0,
    AvgPercentile   FLOAT           NULL,
    HasOverallPodium BIT            NOT NULL DEFAULT 0
);

GO

-- 3. Stored procedure to (re)populate both tables
CREATE OR ALTER PROCEDURE dbo.sp_PopulatePodiumLeaderboards
AS
BEGIN
    SET NOCOUNT ON;

    -- ── All-time ────────────────────────────────────────────
    TRUNCATE TABLE dbo.PodiumLeaderboardAllTime;

    INSERT INTO dbo.PodiumLeaderboardAllTime
        (Forename, Surname, FullName, Gender, TotalSwims, Firsts, Seconds, Thirds, TopTens, AvgPercentile, HasOverallPodium)
    SELECT
        s.Forename,
        s.Surname,
        ISNULL(s.Forename, '') + ' ' + ISNULL(s.Surname, '')     AS FullName,
        -- MAX(Gender) is fast (single pass); gender is consistent per swimmer in practice
        MAX(s.Gender)                                             AS Gender,
        COUNT(*)                                                  AS TotalSwims,
        COUNT(CASE WHEN s.OverallPosition = 1  THEN 1 END)       AS Firsts,
        COUNT(CASE WHEN s.OverallPosition = 2  THEN 1 END)       AS Seconds,
        COUNT(CASE WHEN s.OverallPosition = 3  THEN 1 END)       AS Thirds,
        COUNT(CASE WHEN s.OverallPosition <= 10 THEN 1 END)      AS TopTens,
        -- Avg across ALL swims (AVG ignores NULLs)
        AVG(CAST(s.OverallPercentile AS FLOAT))                  AS AvgPercentile,
        CASE WHEN COUNT(CASE WHEN s.OverallPosition IN (1,2,3) THEN 1 END) >= 1 THEN 1 ELSE 0 END AS HasOverallPodium
    FROM dbo.vw_OceanSwims_Search s
    WHERE s.OverallPosition IS NOT NULL
      AND s.RaceTypeId = 1                          -- Ocean Swims only
      AND ISNULL(s.Category, '') NOT LIKE '%team%'  -- Exclude team entries
      AND ISNULL(s.Category, '') NOT LIKE '%relay%'
    GROUP BY s.Forename, s.Surname;

    -- ── Seasonal ────────────────────────────────────────────
    TRUNCATE TABLE dbo.PodiumLeaderboardSeasonal;

    INSERT INTO dbo.PodiumLeaderboardSeasonal
        (Season, Forename, Surname, FullName, Gender, TotalSwims, Firsts, Seconds, Thirds, TopTens, AvgPercentile, HasOverallPodium)
    SELECT
        YEAR(s.RaceDate)                                          AS Season,
        s.Forename,
        s.Surname,
        ISNULL(s.Forename, '') + ' ' + ISNULL(s.Surname, '')     AS FullName,
        MAX(s.Gender)                                             AS Gender,
        COUNT(*)                                                  AS TotalSwims,
        COUNT(CASE WHEN s.OverallPosition = 1  THEN 1 END)       AS Firsts,
        COUNT(CASE WHEN s.OverallPosition = 2  THEN 1 END)       AS Seconds,
        COUNT(CASE WHEN s.OverallPosition = 3  THEN 1 END)       AS Thirds,
        COUNT(CASE WHEN s.OverallPosition <= 10 THEN 1 END)      AS TopTens,
        AVG(CAST(s.OverallPercentile AS FLOAT))                  AS AvgPercentile,
        CASE WHEN COUNT(CASE WHEN s.OverallPosition IN (1,2,3) THEN 1 END) >= 1 THEN 1 ELSE 0 END AS HasOverallPodium
    FROM dbo.vw_OceanSwims_Search s
    WHERE s.OverallPosition IS NOT NULL
      AND s.RaceTypeId = 1                          -- Ocean Swims only
      AND ISNULL(s.Category, '') NOT LIKE '%team%'  -- Exclude team entries
      AND ISNULL(s.Category, '') NOT LIKE '%relay%'
    GROUP BY YEAR(s.RaceDate), s.Forename, s.Surname;
END;
GO

-- 4. Seed the tables immediately (run after the proc is created)
EXEC dbo.sp_PopulatePodiumLeaderboards;
