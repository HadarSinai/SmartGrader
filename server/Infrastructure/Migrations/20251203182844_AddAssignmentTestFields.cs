using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentTestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
      

            migrationBuilder.AddColumn<string>(
                name: "TestsJson",
                table: "Assignments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FunctionName",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "ReturnType",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "TestsJson",
                table: "Assignments");
        }
    }
}
