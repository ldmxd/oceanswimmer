-- ============================================================
-- Migration: add Url column to dbo.SearchLog.
--
-- Url
--     nvarchar(1000), nullable. The full request URL captured at
--     the moment of the search (scheme + host + path + query) via
--     HttpRequest.GetDisplayUrl(). Lets you paste it back into a
--     browser / Postman to replay the exact search someone made.
--     1000 chars is generous — typical values will be well under
--     200, but giving the column headroom avoids silent truncation
--     when bots tack on tracking params.
--
-- Run once against your RaceResult database.
-- ============================================================

ALTER TABLE dbo.SearchLog
    ADD Url nvarchar(1000) NULL;
