USE [RaceResult]
GO
/****** Object:  StoredProcedure [dbo].[PopulateRace_fromTypedResult_History_Multisport]    Script Date: 24/05/2026 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[PopulateRace_fromTypedResult_History_Multisport]
AS
BEGIN
    SET NOCOUNT ON

    DECLARE @desc            nvarchar(500)
    DECLARE @name            nvarchar(500)
    DECLARE @location        nvarchar(500)
    DECLARE @eventdate       datetime       = NULL
    DECLARE @distance        nvarchar(500)
    DECLARE @racedayofweek   nvarchar(500)  = NULL
    DECLARE @distancedec     decimal(10,2)  = 0
    DECLARE @comment         nvarchar(500)
    DECLARE @racetypeid      int            = 1   -- default Ocean Swim
    DECLARE @RaceDescription nvarchar(500)  = NULL
    DECLARE @canonicalName   nvarchar(500)  = NULL

    SELECT TOP 1 @RaceDescription = HST.RaceDescription
    FROM TypedResult_OceanSwims_History HST
    LEFT JOIN Race R ON HST.RaceDescription = R.RaceDescription
    WHERE R.RaceId IS NULL

    IF (@RaceDescription IS NOT NULL)
    BEGIN
        SELECT TOP 1
            @desc    = HST.[RaceDescription]
          , @name    = SUBSTRING(HST.RaceDescription, 0, CHARINDEX('|', HST.RaceDescription))
          , @comment = RIGHT(HST.RaceDescription, LEN(HST.RaceDescription) - CHARINDEX('|', HST.RaceDescription))
        FROM TypedResult_OceanSwims_History HST
        WHERE HST.[RaceDescription] = @RaceDescription

        -- Contest-label section, between first and second pipe
        SELECT @distance = LEFT(@comment, CHARINDEX('|', @comment + '|') - 1)

        -- 1) Classify race type from the whole description
        IF      (@desc LIKE '%run%')                                     SET @racetypeid = 3
        ELSE IF (@desc LIKE '%biathlon%' OR @desc LIKE '%aquathon%')     SET @racetypeid = 2
        ELSE                                                             SET @racetypeid = 1

        -- 2) Strip race-type words before distance parsing
        DECLARE @dist_for_parse nvarchar(500) = @distance
        SET @dist_for_parse = REPLACE(@dist_for_parse, 'Run',   '')
        SET @dist_for_parse = REPLACE(@dist_for_parse, 'Swim',  '')
        SET @dist_for_parse = REPLACE(@dist_for_parse, 'Ride',  '')
        SET @dist_for_parse = REPLACE(@dist_for_parse, 'Bike',  '')
        SET @dist_for_parse = REPLACE(@dist_for_parse, 'Cycle', '')
        SET @dist_for_parse = REPLACE(@dist_for_parse, 'Beach', '')
        SELECT @distancedec = dbo.ParseDistanceKm(@dist_for_parse)

        -- 3) Derive canonical name: strip trailing ' (YYYY)' if present
        --    e.g. 'Cole Classic Ocean Swim (2019) ' -> 'Cole Classic'
        --         'Bondi to Bronte (2025) '         -> 'Bondi to Bronte'
        DECLARE @trimmedName nvarchar(500) = RTRIM(@name)
        IF PATINDEX('%([0-9][0-9][0-9][0-9])', @trimmedName) > 0
            SET @canonicalName = RTRIM(LEFT(@trimmedName,
                                    PATINDEX('%([0-9][0-9][0-9][0-9])', @trimmedName) - 2))
        -- If the name has no year pattern, leave canonical NULL (Name is already canonical)

        -- 4) Insert the new race row
        INSERT INTO Race
            (RaceDescription, Name, Location, Distance, RaceDate, RaceDayOfWeek,
             datecreated, racetypeid, CanonicalRaceName)
        VALUES
            (@desc, @name, @location, @distancedec, @eventdate, @racedayofweek,
             GETDATE(), @racetypeid, @canonicalName)

        -- 5) Update competitor counts
        UPDATE R
            SET OverallCompetitors = P.OverallCompetitors
              , MaleCompetitors    = P.MaleCompetitors
              , FemaleCompetitors  = P.FemaleCompetitors
        FROM Race R
        INNER JOIN (
            SELECT RaceDescription
                 , MAX(OverallPosition)                                AS OverallCompetitors
                 , SUM(CASE WHEN Sex LIKE 'M%' THEN 1 ELSE 0 END)    AS MaleCompetitors
                 , SUM(CASE WHEN Sex LIKE 'F%' THEN 1 ELSE 0 END)    AS FemaleCompetitors
            FROM TypedResult_OceanSwims_History
            WHERE RaceDescription = @RaceDescription
            GROUP BY RaceDescription
        ) P ON R.RaceDescription = P.RaceDescription
        WHERE R.RaceDescription = @RaceDescription

        -- 6) Back-patch any Pete-format rows on the same year + matching event name
        --    so they merge with this new Farm Results entry in vw_OceanSwims_Rank.
        --    Only runs when we derived a canonical name and the year is known from it.
        IF @canonicalName IS NOT NULL
        BEGIN
            DECLARE @raceYear int = NULL
            IF PATINDEX('%([0-9][0-9][0-9][0-9])', @trimmedName) > 0
                SET @raceYear = CAST(
                    SUBSTRING(@trimmedName,
                        PATINDEX('%([0-9][0-9][0-9][0-9])', @trimmedName) + 1, 4)
                    AS int)

            IF @raceYear IS NOT NULL
            BEGIN
                -- Use first two words of the canonical name as match keywords
                -- (avoids touching unrelated events that happen to share a date)
                DECLARE @kw1 nvarchar(100) = ''
                DECLARE @kw2 nvarchar(100) = ''
                DECLARE @remaining nvarchar(500) = @canonicalName

                SET @kw1 = LEFT(@remaining, CHARINDEX(' ', @remaining + ' ') - 1)
                SET @remaining = LTRIM(SUBSTRING(@remaining, LEN(@kw1) + 2, 500))
                SET @kw2 = LEFT(@remaining, CHARINDEX(' ', @remaining + ' ') - 1)

                IF LEN(@kw1) > 2 AND LEN(@kw2) > 2
                    UPDATE dbo.Race
                    SET CanonicalRaceName = @canonicalName
                    WHERE CanonicalRaceName IS NULL
                      AND racetypeid      = @racetypeid
                      -- Pete-format rows don't have (YYYY) in their Name
                      AND PATINDEX('%([0-9][0-9][0-9][0-9])%', Name) = 0
                      AND YEAR(RaceDate)  = @raceYear
                      AND Name LIKE '%' + @kw1 + '%'
                      AND Name LIKE '%' + @kw2 + '%'
            END
        END

    END
END
