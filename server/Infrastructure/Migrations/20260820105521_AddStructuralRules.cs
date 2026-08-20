using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuralRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "Submissions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExtraAttemptGrantedAt",
                table: "Submissions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExtraAttemptGrantedByUserId",
                table: "Submissions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExtraAttemptReason",
                table: "Submissions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasUnusedExtraAttempt",
                table: "Submissions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSubmittedAt",
                table: "Submissions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ScoreBreakdownJson",
                table: "Submissions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScoreOverriddenAt",
                table: "Submissions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScoreOverriddenByUserId",
                table: "Submissions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScoreOverrideReason",
                table: "Submissions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StructuralResultsJson",
                table: "Submissions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RetryThreshold",
                table: "Assignments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 85);

            migrationBuilder.AddColumn<string>(
                name: "StructuralRulesJson",
                table: "Assignments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TestsAllocation",
                table: "Assignments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.CreateTable(
                name: "SubmissionAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SubmissionId = table.Column<int>(type: "INTEGER", nullable: false),
                    AttemptNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Score = table.Column<double>(type: "REAL", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    FeedbackJson = table.Column<string>(type: "TEXT", nullable: true),
                    TestResultsJson = table.Column<string>(type: "TEXT", nullable: false),
                    StructuralResultsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ScoreBreakdownJson = table.Column<string>(type: "TEXT", nullable: true),
                    SourceCode = table.Column<string>(type: "TEXT", nullable: false),
                    SourceFilesJson = table.Column<string>(type: "TEXT", nullable: false),
                    CompileError = table.Column<string>(type: "TEXT", nullable: true),
                    AiError = table.Column<string>(type: "TEXT", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GradedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsCollapsed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionAttempts_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionAttempts_SubmissionId_AttemptNumber",
                table: "SubmissionAttempts",
                columns: new[] { "SubmissionId", "AttemptNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubmissionAttempts");

            migrationBuilder.DropColumn(
                name: "AttemptNumber",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "ExtraAttemptGrantedAt",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "ExtraAttemptGrantedByUserId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "ExtraAttemptReason",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "HasUnusedExtraAttempt",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "LastSubmittedAt",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "ScoreBreakdownJson",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "ScoreOverriddenAt",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "ScoreOverriddenByUserId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "ScoreOverrideReason",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "StructuralResultsJson",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "RetryThreshold",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "StructuralRulesJson",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "TestsAllocation",
                table: "Assignments");
        }
    }
}
