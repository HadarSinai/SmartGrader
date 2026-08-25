using FluentAssertions;
using SmartGrader.Application.Dtos.Lessons;
using SmartGrader.Application.UseCases.Lessons.CreateLesson;
using Xunit;

namespace SmartGrader.UnitTests.Validation
{
    /// <summary>
    /// הוולידטור היחיד מבין 56 שנבדק כאן ברמת הוולידטור עצמו, ולא דרך הכלל הטהור שמתחתיו.
    /// הסיבה: <see cref="SmartGrader.Application.Common.HebrewDate.HebrewDateConverter"/>
    /// נבדק בנפרד ב-<c>Common/</c>, אבל <b>הכלל שמחבר אותו לטופס יכול פשוט לא להיות שם</b> —
    /// ואז ל' טבת נשמר בשקט ומתגלגל לתאריך לועזי אחר לגמרי.
    /// <para>
    /// ⚠️ שאר הוולידטורים במערכת הם <c>NotEmpty</c> על שם ו-<c>GreaterThan(0)</c> על מזהה.
    /// בדיקה שלהם מנסחת מחדש את הקוד ואינה יכולה למצוא בו תקלה — ר' טבלת "מה לא נבדק".
    /// </para>
    /// </summary>
    public class CreateLessonCommandValidatorTests
    {
        private readonly CreateLessonCommandValidator _validator = new();

        private static CreateLessonCommand Command(int year, int month, int day) =>
            new(
                new CreateLessonRequestDto
                {
                    CourseId = 1,
                    Subject = "מבוא למדעי המחשב",
                    HebrewYear = year,
                    HebrewMonth = month,
                    HebrewDay = day,
                    ClassIds = new List<int> { 20 }
                },
                TeacherId: 7);

        // תאריך עברי קיים עובר
        [Fact]
        public void Validate_AcceptsARealHebrewDate()
        {
            _validator.Validate(Command(5786, 9, 14)).IsValid.Should().BeTrue();
        }

        // ⚠️ ל' טבת אינו קיים — טבת הוא תמיד בן 29 יום. הרכיבים עצמם בטווח, ולכן רק
        // הבדיקה מול הלוח תופסת את זה.
        [Fact]
        public void Validate_RejectsADayThatDoesNotExistInThatMonth()
        {
            _validator.Validate(Command(5786, 4, 30)).IsValid.Should().BeFalse();
        }

        // ⚠️ החודש ה-13 קיים רק בשנה מעוברת. 5786 פשוטה, 5784 מעוברת — אותו מספר חודש,
        // תשובה אחרת.
        [Fact]
        public void Validate_RejectsTheThirteenthMonthInASimpleYear()
        {
            _validator.Validate(Command(5786, 13, 1)).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_AcceptsTheThirteenthMonthInALeapYear()
        {
            _validator.Validate(Command(5784, 13, 1)).IsValid.Should().BeTrue();
        }

        // שיעור בלי כיתה משויכת אינו נגיש לאף תלמידה — ר' LessonAccess
        [Fact]
        public void Validate_RejectsALessonWithNoClasses()
        {
            var command = Command(5786, 9, 14);
            command.Dto.ClassIds = new List<int>();

            _validator.Validate(command).IsValid.Should().BeFalse();
        }
    }
}
