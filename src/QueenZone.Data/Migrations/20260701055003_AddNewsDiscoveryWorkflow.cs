using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsDiscoveryWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewsDiscoverySources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HomepageUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    FeedOrSiteUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TrustTier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PollIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    RelevanceKeywords = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastFetchedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsDiscoverySources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsCandidates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceId = table.Column<int>(type: "int", nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CanonicalUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CanonicalUrlHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SourcePublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiscoveredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RelevanceScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    DuplicateOfCandidateId = table.Column<int>(type: "int", nullable: true),
                    PromotedNewsId = table.Column<int>(type: "int", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsCandidates_NewsCandidates_DuplicateOfCandidateId",
                        column: x => x.DuplicateOfCandidateId,
                        principalTable: "NewsCandidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NewsCandidates_NewsDiscoverySources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "NewsDiscoverySources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NewsAiRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModelProvider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModelId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PromptVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InputTokens = table.Column<int>(type: "int", nullable: true),
                    OutputTokens = table.Column<int>(type: "int", nullable: true),
                    EstimatedCostUsd = table.Column<decimal>(type: "decimal(10,6)", precision: 10, scale: 6, nullable: true),
                    StructuredResultJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsAiRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsAiRuns_NewsCandidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "NewsCandidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NewsCandidateEvidence",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<int>(type: "int", nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CanonicalUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SourceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceTrustTier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FetchedTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FetchedPublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Excerpt = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FetchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Etag = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsCandidateEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsCandidateEvidence_NewsCandidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "NewsCandidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NewsAgentDrafts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<int>(type: "int", nullable: false),
                    AiRunId = table.Column<int>(type: "int", nullable: true),
                    ProposedTitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProposedSlug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProposedExcerpt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ProposedBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttributionText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SourceNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConfidenceNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SuggestedPublishAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsAgentDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsAgentDrafts_NewsAiRuns_AiRunId",
                        column: x => x.AiRunId,
                        principalTable: "NewsAiRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NewsAgentDrafts_NewsCandidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "NewsCandidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsAgentDrafts_AiRunId",
                table: "NewsAgentDrafts",
                column: "AiRunId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsAgentDrafts_CandidateId",
                table: "NewsAgentDrafts",
                column: "CandidateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsAiRuns_CandidateId_StartedAt",
                table: "NewsAiRuns",
                columns: new[] { "CandidateId", "StartedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_NewsCandidateEvidence_CandidateId_FetchedAt",
                table: "NewsCandidateEvidence",
                columns: new[] { "CandidateId", "FetchedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_NewsCandidates_CanonicalUrlHash",
                table: "NewsCandidates",
                column: "CanonicalUrlHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsCandidates_ContentHash",
                table: "NewsCandidates",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_NewsCandidates_DuplicateOfCandidateId",
                table: "NewsCandidates",
                column: "DuplicateOfCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsCandidates_SourceId",
                table: "NewsCandidates",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsCandidates_Status_DiscoveredAt",
                table: "NewsCandidates",
                columns: new[] { "Status", "DiscoveredAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_NewsDiscoverySources_Key",
                table: "NewsDiscoverySources",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsAgentDrafts");

            migrationBuilder.DropTable(
                name: "NewsCandidateEvidence");

            migrationBuilder.DropTable(
                name: "NewsAiRuns");

            migrationBuilder.DropTable(
                name: "NewsCandidates");

            migrationBuilder.DropTable(
                name: "NewsDiscoverySources");
        }
    }
}
