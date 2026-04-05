using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FikaForecast.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewsBriefRuns",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", nullable: false),
                    DeploymentName = table.Column<string>(type: "TEXT", nullable: false),
                    PromptName = table.Column<string>(type: "TEXT", nullable: false),
                    Duration = table.Column<double>(type: "REAL", nullable: false),
                    InputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    OutputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    RawAgentOutput = table.Column<string>(type: "TEXT", nullable: false),
                    RawMarkdownOutput = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsBriefRuns", x => x.RunId);
                });

            migrationBuilder.CreateTable(
                name: "WeeklySummaryRuns",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WeekStart = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    WeekEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Duration = table.Column<double>(type: "REAL", nullable: false),
                    InputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    OutputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    RawAgentOutput = table.Column<string>(type: "TEXT", nullable: false),
                    RawMarkdownOutput = table.Column<string>(type: "TEXT", nullable: false),
                    NetMood = table.Column<string>(type: "TEXT", nullable: false),
                    MoodSummary = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklySummaryRuns", x => x.RunId);
                });

            migrationBuilder.CreateTable(
                name: "NewsItems",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Mood = table.Column<string>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsItems", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_NewsItems_NewsBriefRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "NewsBriefRuns",
                        principalColumn: "RunId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeeklySummaryThemes",
                columns: table => new
                {
                    ThemeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    Confidence = table.Column<string>(type: "TEXT", nullable: false),
                    Sentiment = table.Column<string>(type: "TEXT", nullable: false),
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklySummaryThemes", x => x.ThemeId);
                    table.ForeignKey(
                        name: "FK_WeeklySummaryThemes_WeeklySummaryRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "WeeklySummaryRuns",
                        principalColumn: "RunId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CategoryAssessments",
                columns: table => new
                {
                    AssessmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Headline = table.Column<string>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    Sentiment = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryAssessments", x => x.AssessmentId);
                    table.ForeignKey(
                        name: "FK_CategoryAssessments_NewsItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "NewsItems",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAssessments_ItemId",
                table: "CategoryAssessments",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_NewsItems_RunId",
                table: "NewsItems",
                column: "RunId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklySummaryThemes_RunId",
                table: "WeeklySummaryThemes",
                column: "RunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryAssessments");

            migrationBuilder.DropTable(
                name: "WeeklySummaryThemes");

            migrationBuilder.DropTable(
                name: "NewsItems");

            migrationBuilder.DropTable(
                name: "WeeklySummaryRuns");

            migrationBuilder.DropTable(
                name: "NewsBriefRuns");
        }
    }
}
