USE [RaceResult]
GO

/****** Object:  Table [auth].[Rivals] ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [auth].[Rivals](
	[RivalId] [INT] IDENTITY(1,1) NOT NULL,
	[UserId] [INT] NOT NULL,
	[RivalForename] [NVARCHAR](100) NOT NULL,
	[RivalSurname] [NVARCHAR](100) NOT NULL,
	[CreatedAt] [DATETIME] NOT NULL,
PRIMARY KEY CLUSTERED
(
	[RivalId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [auth].[Rivals] ADD DEFAULT (GETDATE()) FOR [CreatedAt]
GO

-- One-to-many: a user can have multiple saved rivals; deleting the user cleans these up too.
ALTER TABLE [auth].[Rivals] WITH CHECK ADD CONSTRAINT [FK_Rivals_Users] FOREIGN KEY([UserId])
REFERENCES [auth].[Users] ([UserId])
ON DELETE CASCADE
GO

ALTER TABLE [auth].[Rivals] CHECK CONSTRAINT [FK_Rivals_Users]
GO

-- Prevent saving the same name twice for the same user.
CREATE UNIQUE INDEX [UX_Rivals_UserId_Name] ON [auth].[Rivals] ([UserId], [RivalForename], [RivalSurname])
GO
