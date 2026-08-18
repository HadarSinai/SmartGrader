using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentGradingMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ברירת המחדל בעמודה היא "Method" (לא "FullProgram", ברירת המחדל של קוד חדש) —
            // כדי לשמר במדויק את התנהגות הבדיקה של תרגילים קיימים ללא ExpectedFiles.
            migrationBuilder.AddColumn<string>(
                name: "GradingMode",
                table: "Assignments",
                type: "TEXT",
                nullable: false,
                defaultValue: "Method");

            // תרגילים קיימים עם ExpectedFiles מוגדרים היו נבדקים עד כה במסלול הרב-קובצי —
            // מסיקים עבורם MultiFileMethod כדי לשמר את אותה התנהגות בדיוק.
            migrationBuilder.Sql(
                "UPDATE Assignments SET GradingMode = 'MultiFileMethod' " +
                "WHERE ExpectedFilesJson IS NOT NULL AND ExpectedFilesJson != '[]';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GradingMode",
                table: "Assignments");
        }
    }
}
