using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FikaForecast.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOpportunityScanTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpportunityScanRuns",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubstitutionChainRunId = table.Column<Guid>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_OpportunityScanRuns", x => x.RunId);
                });

            migrationBuilder.CreateTable(
                name: "RotationTargets",
                columns: table => new
                {
                    TargetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    SignalStrength = table.Column<string>(type: "TEXT", nullable: false),
                    Rationale = table.Column<string>(type: "TEXT", nullable: false),
                    RiskCaveat = table.Column<string>(type: "TEXT", nullable: false),
                    RunId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RotationTargets", x => x.TargetId);
                    table.ForeignKey(
                        name: "FK_RotationTargets_OpportunityScanRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "OpportunityScanRuns",
                        principalColumn: "RunId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RotationTargets_RunId",
                table: "RotationTargets",
                column: "RunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RotationTargets");

            migrationBuilder.DropTable(
                name: "OpportunityScanRuns");
        }
    }
}
