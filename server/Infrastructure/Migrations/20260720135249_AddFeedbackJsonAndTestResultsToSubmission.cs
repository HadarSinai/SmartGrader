using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackJsonAndTestResultsToSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Comments",
                table: "Submissions",
                newName: "FeedbackJson");

            migrationBuilder.AddColumn<string>(
                name: "TestResultsJson",
                table: "Submissions",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TestResultsJson",
                table: "Submissions");

            migrationBuilder.RenameColumn(
                name: "FeedbackJson",
                table: "Submissions",
                newName: "Comments");
        }
    }
}
