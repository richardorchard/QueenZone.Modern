using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Creates forum poll tables (issue #234). Hand-written with [Migration] for EF discovery.
/// </summary>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260711120000_AddForumPolls")]
public partial class AddForumPolls : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ForumPolls', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ForumPolls
                (
                    Id uniqueidentifier NOT NULL,
                    ThreadId bigint NOT NULL,
                    LegacyTopicId int NOT NULL,
                    Question nvarchar(300) NOT NULL,
                    IsMultiChoice bit NOT NULL,
                    MaxChoices int NULL,
                    ClosesAt datetimeoffset NULL,
                    ClosedAt datetimeoffset NULL,
                    CreatedByMemberId uniqueidentifier NOT NULL,
                    CreatedAt datetimeoffset NOT NULL,
                    CONSTRAINT PK_ForumPolls PRIMARY KEY (Id)
                );

                CREATE UNIQUE INDEX UQ_ForumPolls_LegacyTopicId ON dbo.ForumPolls (LegacyTopicId);
                CREATE UNIQUE INDEX UQ_ForumPolls_ThreadId ON dbo.ForumPolls (ThreadId);
            END

            IF OBJECT_ID(N'dbo.ForumPollOptions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ForumPollOptions
                (
                    Id uniqueidentifier NOT NULL,
                    PollId uniqueidentifier NOT NULL,
                    OptionText nvarchar(200) NOT NULL,
                    DisplayOrder int NOT NULL,
                    CONSTRAINT PK_ForumPollOptions PRIMARY KEY (Id),
                    CONSTRAINT FK_ForumPollOptions_ForumPolls_PollId
                        FOREIGN KEY (PollId) REFERENCES dbo.ForumPolls (Id) ON DELETE CASCADE
                );

                CREATE INDEX IX_ForumPollOptions_PollId_DisplayOrder
                    ON dbo.ForumPollOptions (PollId, DisplayOrder);
            END

            IF OBJECT_ID(N'dbo.ForumPollVotes', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.ForumPollVotes
                (
                    Id uniqueidentifier NOT NULL,
                    PollId uniqueidentifier NOT NULL,
                    OptionId uniqueidentifier NOT NULL,
                    MemberAccountId uniqueidentifier NOT NULL,
                    VotedAt datetimeoffset NOT NULL,
                    CONSTRAINT PK_ForumPollVotes PRIMARY KEY (Id),
                    CONSTRAINT FK_ForumPollVotes_ForumPolls_PollId
                        FOREIGN KEY (PollId) REFERENCES dbo.ForumPolls (Id) ON DELETE CASCADE,
                    CONSTRAINT FK_ForumPollVotes_ForumPollOptions_OptionId
                        FOREIGN KEY (OptionId) REFERENCES dbo.ForumPollOptions (Id),
                    CONSTRAINT FK_ForumPollVotes_MemberAccounts_MemberAccountId
                        FOREIGN KEY (MemberAccountId) REFERENCES dbo.MemberAccounts (Id)
                );

                CREATE UNIQUE INDEX UQ_ForumPollVotes_Poll_Member_Option
                    ON dbo.ForumPollVotes (PollId, MemberAccountId, OptionId);

                CREATE INDEX IX_ForumPollVotes_OptionId
                    ON dbo.ForumPollVotes (OptionId);
            END

            IF OBJECT_ID(N'dbo.ModernForumThread', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.ForumPolls', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = N'FK_ForumPolls_ModernForumThread_ThreadId')
            BEGIN
                ALTER TABLE dbo.ForumPolls
                ADD CONSTRAINT FK_ForumPolls_ModernForumThread_ThreadId
                    FOREIGN KEY (ThreadId) REFERENCES dbo.ModernForumThread (Id)
                    ON DELETE CASCADE;
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ForumPollVotes', N'U') IS NOT NULL DROP TABLE dbo.ForumPollVotes;
            IF OBJECT_ID(N'dbo.ForumPollOptions', N'U') IS NOT NULL DROP TABLE dbo.ForumPollOptions;
            IF OBJECT_ID(N'dbo.ForumPolls', N'U') IS NOT NULL DROP TABLE dbo.ForumPolls;
            """);
    }
}
