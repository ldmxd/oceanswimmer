/****** Object:  View [dbo].[vw_OceanSwims_Rank]    Script Date: 24/05/2026 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Ranks ocean swim race editions by participation.
-- Each row = one race name + date combination (a single edition of the event).
--
--   RaceId             = raceid of the sub-event with the most competitors on that day
--                        (i.e. the main/longest race) — used for linking to results
--   Races              = number of distinct event categories within that edition
--                        (e.g. 1 km open + 2 km open on the same day = 2)
--   OverallCompetitors = total finishers across all categories for that edition
--
-- Grouping uses COALESCE(CanonicalRaceName, Name) so that old Pete-format rows
-- with a CanonicalRaceName set will merge with the matching Farm Results entry.
--
-- Used by the /leaderboard/races and /leaderboard/races/seasonal API endpoints.
CREATE OR ALTER VIEW [dbo].[vw_OceanSwims_Rank]
AS
SELECT
    (
        SELECT TOP 1 raceid
        FROM   dbo.Race r2
        WHERE  COALESCE(r2.CanonicalRaceName, r2.Name) = COALESCE(r.CanonicalRaceName, r.Name)
          AND  r2.RaceDate   = r.RaceDate
          AND  r2.racetypeid = 1
          AND  r2.IsVisible  = 1
        ORDER BY r2.OverallCompetitors DESC
    )                                           AS RaceId,
    COALESCE(r.CanonicalRaceName, r.Name)       AS RaceName,
    RaceDate,
    COUNT(RaceDescription)                      AS Races,
    SUM(OverallCompetitors)                     AS OverallCompetitors
FROM
    dbo.Race r
WHERE
    racetypeid = 1
    AND IsVisible = 1
GROUP BY
    COALESCE(r.CanonicalRaceName, r.Name), RaceDate
GO
