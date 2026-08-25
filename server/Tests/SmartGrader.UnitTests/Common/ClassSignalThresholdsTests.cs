using FluentAssertions;
using SmartGrader.Application.Services.Notifications;
using Xunit;

namespace SmartGrader.UnitTests.Common
{
    /// <summary>
    /// הכלל היחיד שמכריע מה נחשב "הרבה תלמידות" — שלושה מתוך ארבעה סיגנלים עוברים דרכו.
    /// באג כאן הוא פעמון שמצלצל על כל תקלה, וכזה מפסיקים לקרוא אחרי יומיים.
    /// </summary>
    public class ClassSignalThresholdsTests
    {
        private static ClassSignalThresholds Defaults() => new();

        // ⚠️ המינימום המוחלט: בלעדיו 2 מתוך 3 הוא 67% ועובר כל סף יחסי
        [Fact]
        public void IsMany_IsFalse_ForSmallClassEvenAtHighRatio()
        {
            Defaults().IsMany(affected: 2, total: 3).Should().BeFalse();
        }

        // בדיוק על שני הסיפים — שלוש תלמידות ו-50% — עובר
        [Fact]
        public void IsMany_IsTrue_AtBothThresholds()
        {
            Defaults().IsMany(affected: 3, total: 6).Should().BeTrue();
        }

        // מספיק תלמידות אבל מתחת ליחס — לא מתריע
        [Fact]
        public void IsMany_IsFalse_BelowRatio()
        {
            Defaults().IsMany(affected: 3, total: 7).Should().BeFalse();
        }

        // אף הגשה — אין על מה להתריע, ואין חלוקה באפס
        [Fact]
        public void IsMany_IsFalse_WhenNobodySubmitted()
        {
            Defaults().IsMany(affected: 0, total: 0).Should().BeFalse();
        }

        // ברירות המחדל הן מה שמתועד: 3 תלמידות, 50%, ו-3 הגשות ל"אף אחת לא עברה"
        [Fact]
        public void Defaults_MatchDocumentedValues()
        {
            var thresholds = Defaults();

            thresholds.MinAffectedStudents.Should().Be(3);
            thresholds.MinAffectedRatio.Should().Be(0.5);
            thresholds.MinSubmissionsForNobodyPassed.Should().Be(3);
        }

        // הסיפים ניתנים לכוונון מ-appsettings, והכלל מכבד אותם
        [Fact]
        public void IsMany_HonoursConfiguredThresholds()
        {
            var strict = new ClassSignalThresholds { MinAffectedStudents = 5, MinAffectedRatio = 0.8 };

            strict.IsMany(affected: 4, total: 4).Should().BeFalse();
            strict.IsMany(affected: 5, total: 6).Should().BeTrue();
        }
    }
}
