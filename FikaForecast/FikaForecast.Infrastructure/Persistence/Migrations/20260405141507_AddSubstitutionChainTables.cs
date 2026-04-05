using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FikaForecast.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubstitutionChainTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubstitutionChainRuns",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WeeklySummaryRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Duration = table.Column<double>(type: "REAL", nullable: false),
                    InputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    OutputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    RawAgentOutput = table.Column<string>(type: "TEXT", nullable: false),
                    RawMarkdownOutput = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubstitutionChainRuns", x => x.RunId);
                });

            migrationBuilder.CreateTable(
                name: "RotationChains",
                columns: table => new
                {
                    ChainId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CapitalFleeing = table.Column<string>(type: "TEXT", nullable: false),
                    FlowsToward = table.Column<string>(type: "TEXT", nullable: false),
                    Mechanism = table.Column<string>(type: "TEXT", nullable: false),
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotationChains", x => x.ChainId);
                    table.ForeignKey(
                        name: "FK_RotationChains_SubstitutionChainRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "SubstitutionChainRuns",
                        principalColumn: "RunId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RotationChains_RunId",
                table: "RotationChains",
                column: "RunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RotationChains");

            migrationBuilder.DropTable(
                name: "SubstitutionChainRuns");
        }
    }
}
