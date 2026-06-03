USE [RaceResult];
GO

-- ══════════════════════════════════════════════════════
-- sp_PopulateLeaderboards
-- Truncates and repopulates LeaderboardAllTime and
-- LeaderboardSeasonal. Called weekly by
-- LeaderboardRefreshService (every Sunday ~14:00 UTC).
-- ══════════════════════════════════════════════════════
CREATE OR ALTER PROCEDURE [dbo].[sp_PopulateLeaderboards]
AS
BEGIN
    SET NOCOUNT ON;

    -- ── All-time ─────────────────────────────────────────────────────────
    TRUNCATE TABLE dbo.LeaderboardAllTime;

    WITH SwimAgg AS (
        SELECT
            os.Surname_Search,
            os.Forename_Search,
            COUNT(*)          AS TotalSwims,
            SUM(os.Distance)  AS TotalDistanceKm,
            MIN(r.RaceDate)   AS FirstSwimDate,
            MAX(r.RaceDate)   AS LastSwimDate,
            MAX(CASE WHEN os.Sex IN ('Male', 'Female') THEN os.Sex ELSE NULL END) AS Gender
        FROM dbo.OceanSwims os
        JOIN dbo.Race r ON os.raceid = r.raceid
        WHERE r.IsVisible = 1
          AND os.Surname_Search  IS NOT NULL
          AND os.Forename_Search IS NOT NULL
        GROUP BY os.Surname_Search, os.Forename_Search
    ),
    CanonicalName AS (
        SELECT
            os.Surname_Search,
            os.Forename_Search,
            os.Forename,
            os.Surname,
            ROW_NUMBER() OVER (
                PARTITION BY os.Surname_Search, os.Forename_Search
                ORDER BY r.RaceDate DESC
            ) AS rn
        FROM dbo.OceanSwims os
        JOIN dbo.Race r ON os.raceid = r.raceid
        WHERE r.IsVisible = 1
    )
    INSERT INTO dbo.LeaderboardAllTime
        (Forename, Surname, FullName, Gender, TotalSwims, TotalDistanceKm, FirstSwimDate, LastSwimDate)
    SELECT
        cn.Forename,
        cn.Surname,
        LTRIM(RTRIM(ISNULL(cn.Forename, '') + ' ' + ISNULL(cn.Surname, ''))) AS FullName,
        sa.Gender,
        sa.TotalSwims,
        sa.TotalDistanceKm,
        sa.FirstSwimDate,
        sa.LastSwimDate
    FROM SwimAgg sa
    JOIN CanonicalName cn
        ON  sa.Surname_Search  = cn.Surname_Search
        AND sa.Forename_Search = cn.Forename_Search
        AND cn.rn = 1
    ORDER BY sa.TotalSwims DESC;

    -- ── Seasonal (calendar year) ──────────────────────────────────────────
    TRUNCATE TABLE dbo.LeaderboardSeasonal;

    WITH SeasonAgg AS (
        SELECT
            YEAR(r.RaceDate)  AS Season,
            os.Surname_Search,
            os.Forename_Search,
            COUNT(*)          AS TotalSwims,
            SUM(os.Distance)  AS TotalDistanceKm,
            MAX(CASE WHEN os.Sex IN ('Male', 'Female') THEN os.Sex ELSE NULL END) AS Gender
        FROM dbo.OceanSwims os
        JOIN dbo.Race r ON os.raceid = r.raceid
        WHERE r.IsVisible = 1
          AND os.Surname_Search  IS NOT NULL
          AND os.Forename_Search IS NOT NULL
          AND r.RaceDate IS NOT NULL
        GROUP BY YEAR(r.RaceDate), os.Surname_Search, os.Forename_Search
    ),
    CanonicalName AS (
        SELECT
            os.Surname_Search,
            os.Forename_Search,
            os.Forename,
            os.Surname,
            ROW_NUMBER() OVER (
                PARTITION BY os.Surname_Search, os.Forename_Search
                ORDER BY r.RaceDate DESC
            ) AS rn
        FROM dbo.OceanSwims os
        JOIN dbo.Race r ON os.raceid = r.raceid
        WHERE r.IsVisible = 1
    )
    INSERT INTO dbo.LeaderboardSeasonal
        (Season, Forename, Surname, FullName, Gender, TotalSwims, TotalDistanceKm)
    SELECT
        sa.Season,
        cn.Forename,
        cn.Surname,
        LTRIM(RTRIM(ISNULL(cn.Forename, '') + ' ' + ISNULL(cn.Surname, ''))) AS FullName,
        sa.Gender,
        sa.TotalSwims,
        sa.TotalDistanceKm
    FROM SeasonAgg sa
    JOIN CanonicalName cn
        ON  sa.Surname_Search  = cn.Surname_Search
        AND sa.Forename_Search = cn.Forename_Search
        AND cn.rn = 1
    ORDER BY sa.Season DESC, sa.TotalSwims DESC;
END;
