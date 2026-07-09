USE [RaceResult]
GO

/****** Object:  Table [auth].[RivalExclusions] ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- One-to-many off auth.Rivals: records which oceanswimsIds were unticked
-- ("not really them") in the Rivalry page's disambiguation review, so a
-- saved rival doesn't need to be re-confirmed every time.
CREATE TABLE [auth].[RivalExclusions](
	[RivalId] [INT] NOT NULL,
	[OceanSwimsId] [INT] NOT NULL,
PRIMARY KEY CLUSTERED
(
	[RivalId] ASC,
	[OceanSwimsId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

-- Deleting a saved rival cleans up its exclusions too.
ALTER TABLE [auth].[RivalExclusions] WITH CHECK ADD CONSTRAINT [FK_RivalExclusions_Rivals] FOREIGN KEY([RivalId])
REFERENCES [auth].[Rivals] ([RivalId])
ON DELETE CASCADE
GO

ALTER TABLE [auth].[RivalExclusions] CHECK CONSTRAINT [FK_RivalExclusions_Rivals]
GO
