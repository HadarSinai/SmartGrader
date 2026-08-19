using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGrader.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OneSubmissionPerStudentAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Submissions_StudentId",
                table: "Submissions");

            // ⚠️ ניקוי נתונים לפני האינדקס — בלעדיו המיגרציה נכשלת על כל DB שכבר יש בו כפילויות,
            // והן קיימות: עד עכשיו CreateSubmissionHandler לא בדק קיום כלל. שומרים את ההגשה
            // האחרונה לכל (תלמידה, תרגיל), בדיוק כמו הבחירה שכבר נעשית ב-CompleteLessonHandler,
            // ומוחקים את הקודמות. שובר שוויון לפי Id כדי שהתוצאה תהיה דטרמיניסטית.
            // נכתב ידנית: EF לא מייצר מיגרציית נתונים, ובלי זה שלב האינדקס לא ניתן להרצה.
            migrationBuilder.Sql(@"
                DELETE FROM Submissions
                WHERE Id NOT IN (
                    SELECT Id FROM (
                        SELECT Id,
                               ROW_NUMBER() OVER (
                                   PARTITION BY StudentId, AssignmentId
                                   ORDER BY SubmittedAt DESC, Id DESC
                               ) AS rn
                        FROM Submissions
                    )
                    WHERE rn = 1
                );");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_StudentId_AssignmentId",
                table: "Submissions",
                columns: new[] { "StudentId", "AssignmentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Submissions_StudentId_AssignmentId",
                table: "Submissions");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_StudentId",
                table: "Submissions",
                column: "StudentId");
        }
    }
}
