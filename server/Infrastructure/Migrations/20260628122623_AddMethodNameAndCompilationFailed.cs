using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMethodNameAndCompilationFailed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompileError",
                table: "Submissions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MethodName",
                table: "Assignments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompileError",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "MethodName",
                table: "Assignments");
        }
    }
}
