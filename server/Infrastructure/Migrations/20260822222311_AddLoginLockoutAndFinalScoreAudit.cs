using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginLockoutAndFinalScoreAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEndsAt",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ComputedScore",
                table: "LessonResults",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalScoreOverriddenAt",
                table: "LessonResults",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinalScoreOverriddenByUserId",
                table: "LessonResults",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalScoreOverrideReason",
                table: "LessonResults",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockoutEndsAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ComputedScore",
                table: "LessonResults");

            migrationBuilder.DropColumn(
                name: "FinalScoreOverriddenAt",
                table: "LessonResults");

            migrationBuilder.DropColumn(
                name: "FinalScoreOverriddenByUserId",
                table: "LessonResults");

            migrationBuilder.DropColumn(
                name: "FinalScoreOverrideReason",
                table: "LessonResults");
        }
    }
}
