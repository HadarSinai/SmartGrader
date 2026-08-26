using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SmartGrader.UnitTests.Helpers;
using Xunit;

namespace SmartGrader.UnitTests.Integration
{
    /// <summary>
    /// 🔴 הגשה אחת בדיוק לכל (תלמידה, תרגיל). הבדיקה ב-<c>CreateSubmissionHandler</c> לבדה
    /// אינה מספיקה: שתי לחיצות במקביל עוברות אותה שתיהן ויוצרות שתי שורות מנוקדות —
    /// ושתיהן נספרות בממוצע השיעור. האכיפה האמיתית היא האינדקס הייחודי במסד, ולכן רק
    /// בדיקה מול מסד אמיתי יכולה להראות שהוא קיים.
    /// </summary>
    public class SubmissionUniqueIndexTests
    {
        // הגשה שנייה לאותה תלמידה ולאותו תרגיל נדחית ברמת המסד
        [Fact]
        public void SecondSubmission_ForTheSameStudentAndAssignment_IsRejected()
        {
            using var db = new SchoolDatabase();
            var lesson = db.AddLesson(db.AddTeacher());
            var assignment = db.AddAssignment(lesson);
            var student = db.AddStudent(db.AddClass());
            db.Add(new SubmissionBuilder(student.Id, assignment.Id).Build());

            var act = () => db.Add(new SubmissionBuilder(student.Id, assignment.Id).Build());

            act.Should().Throw<DbUpdateException>();
        }

        // אותה תלמידה בתרגיל אחר — מותר, וזה המקרה הרגיל
        [Fact]
        public void SameStudent_InAnotherAssignment_IsAllowed()
        {
            using var db = new SchoolDatabase();
            var lesson = db.AddLesson(db.AddTeacher());
            var first = db.AddAssignment(lesson);
            var second = db.AddAssignment(lesson);
            var student = db.AddStudent(db.AddClass());
            db.Add(new SubmissionBuilder(student.Id, first.Id).Build());

            var act = () => db.Add(new SubmissionBuilder(student.Id, second.Id).Build());

            act.Should().NotThrow();
        }

        // שתי תלמידות באותו תרגיל — מותר
        [Fact]
        public void TwoStudents_InTheSameAssignment_AreAllowed()
        {
            using var db = new SchoolDatabase();
            var lesson = db.AddLesson(db.AddTeacher());
            var assignment = db.AddAssignment(lesson);
            var schoolClass = db.AddClass();
            db.Add(new SubmissionBuilder(db.AddStudent(schoolClass).Id, assignment.Id).Build());

            var act = () => db.Add(new SubmissionBuilder(db.AddStudent(schoolClass).Id, assignment.Id).Build());

            act.Should().NotThrow();
        }
    }
}
