using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolClasses : Migration
    {
        // השנה העברית שאליה מומרות הכיתות הקיימות (תשפ"ו)
        private const int CurrentAcademicYear = 5786;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                table: "Students",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SchoolClasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AcademicYear = table.Column<int>(type: "INTEGER", nullable: false),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolClasses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LessonSchoolClasses",
                columns: table => new
                {
                    ClassesId = table.Column<int>(type: "INTEGER", nullable: false),
                    LessonsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonSchoolClasses", x => new { x.ClassesId, x.LessonsId });
                    table.ForeignKey(
                        name: "FK_LessonSchoolClasses_Lessons_LessonsId",
                        column: x => x.LessonsId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LessonSchoolClasses_SchoolClasses_ClassesId",
                        column: x => x.ClassesId,
                        principalTable: "SchoolClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // --- המרת נתונים: כל ClassName קיים → רשומת SchoolClass בשנה הנוכחית ---

            // 1. כיתה לכל שם כיתה ייחודי
            migrationBuilder.Sql($@"
                INSERT INTO SchoolClasses (Name, AcademicYear, IsArchived, CreatedAt)
                SELECT DISTINCT ClassName, {CurrentAcademicYear}, 0, datetime('now')
                FROM Students
                WHERE ClassName IS NOT NULL AND TRIM(ClassName) <> '';
            ");

            // 2. כיתת עוגן לתלמידים בלי שם כיתה (אם יש כאלה)
            migrationBuilder.Sql($@"
                INSERT INTO SchoolClasses (Name, AcademicYear, IsArchived, CreatedAt)
                SELECT 'ללא כיתה', {CurrentAcademicYear}, 0, datetime('now')
                WHERE EXISTS (SELECT 1 FROM Students WHERE ClassName IS NULL OR TRIM(ClassName) = '')
                  AND NOT EXISTS (SELECT 1 FROM SchoolClasses WHERE Name = 'ללא כיתה' AND AcademicYear = {CurrentAcademicYear});
            ");

            // 3. שיוך כל תלמיד לכיתה שלו
            migrationBuilder.Sql($@"
                UPDATE Students SET ClassId = (
                    SELECT sc.Id FROM SchoolClasses sc
                    WHERE sc.AcademicYear = {CurrentAcademicYear}
                      AND sc.Name = CASE
                            WHEN Students.ClassName IS NULL OR TRIM(Students.ClassName) = '' THEN 'ללא כיתה'
                            ELSE Students.ClassName
                          END
                );
            ");

            // 4. תאימות לאחור: כל השיעורים הקיימים מקושרים לכל הכיתות שנוצרו
            migrationBuilder.Sql(@"
                INSERT INTO LessonSchoolClasses (ClassesId, LessonsId)
                SELECT sc.Id, l.Id FROM SchoolClasses sc CROSS JOIN Lessons l;
            ");

            // --- סוף המרת נתונים ---

            migrationBuilder.DropColumn(
                name: "ClassName",
                table: "Students");

            migrationBuilder.CreateIndex(
                name: "IX_Students_ClassId",
                table: "Students",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_LessonSchoolClasses_LessonsId",
                table: "LessonSchoolClasses",
                column: "LessonsId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolClasses_Name_AcademicYear",
                table: "SchoolClasses",
                columns: new[] { "Name", "AcademicYear" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_SchoolClasses_ClassId",
                table: "Students",
                column: "ClassId",
                principalTable: "SchoolClasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_SchoolClasses_ClassId",
                table: "Students");

            migrationBuilder.AddColumn<string>(
                name: "ClassName",
                table: "Students",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // שחזור שם הכיתה מתוך רשומת הכיתה לפני מחיקת הטבלאות
            migrationBuilder.Sql(@"
                UPDATE Students SET ClassName = COALESCE(
                    (SELECT sc.Name FROM SchoolClasses sc WHERE sc.Id = Students.ClassId), '');
            ");

            migrationBuilder.DropTable(
                name: "LessonSchoolClasses");

            migrationBuilder.DropTable(
                name: "SchoolClasses");

            migrationBuilder.DropIndex(
                name: "IX_Students_ClassId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "Students");
        }
    }
}
