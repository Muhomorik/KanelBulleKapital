using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FikaForecast.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // WeeklySummaryRuns: rename existing columns + add PeriodIsoWeek.
            migrationBuilder.RenameColumn(
                name: "WeekStart",
                table: "WeeklySummaryRuns",
                newName: "PeriodStart");

            migrationBuilder.RenameColumn(
                name: "WeekEnd",
                table: "WeeklySummaryRuns",
                newName: "PeriodEnd");

            migrationBuilder.AddColumn<string>(
                name: "PeriodIsoWeek",
                table: "WeeklySummaryRuns",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // SubstitutionChainRuns: add denormalized period fields (copied from parent at sync/orchestration).
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PeriodStart",
                table: "SubstitutionChainRuns",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PeriodEnd",
                table: "SubstitutionChainRuns",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "PeriodIsoWeek",
                table: "SubstitutionChainRuns",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // OpportunityScanRuns: same denormalization.
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PeriodStart",
                table: "OpportunityScanRuns",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PeriodEnd",
                table: "OpportunityScanRuns",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "PeriodIsoWeek",
                table: "OpportunityScanRuns",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PeriodIsoWeek",
                table: "WeeklySummaryRuns");

            migrationBuilder.DropColumn(
                name: "PeriodStart",
                table: "SubstitutionChainRuns");

            migrationBuilder.DropColumn(
                name: "PeriodEnd",
                table: "SubstitutionChainRuns");

            migrationBuilder.DropColumn(
                name: "PeriodIsoWeek",
                table: "SubstitutionChainRuns");

            migrationBuilder.DropColumn(
                name: "PeriodStart",
                table: "OpportunityScanRuns");

            migrationBuilder.DropColumn(
                name: "PeriodEnd",
                table: "OpportunityScanRuns");

            migrationBuilder.DropColumn(
                name: "PeriodIsoWeek",
                table: "OpportunityScanRuns");

            migrationBuilder.RenameColumn(
                name: "PeriodEnd",
                table: "WeeklySummaryRuns",
                newName: "WeekEnd");

            migrationBuilder.RenameColumn(
                name: "PeriodStart",
                table: "WeeklySummaryRuns",
                newName: "WeekStart");
        }
    }
}
