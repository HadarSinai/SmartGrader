using FluentAssertions;
using SmartGrader.Domain.Entities;
using Xunit;

namespace SmartGrader.UnitTests.Domain
{
    /// <summary>
    /// הציון הסופי של תלמידה בשיעור — המספר שהיא באמת מקבלת. כל טסט משועתק מהערה
    /// קיימת ב-<c>LessonResult.cs</c>.
    /// </summary>
    public class LessonResultTests
    {
        private static LessonResult NewResult() => LessonResult.Create(studentId: 7, lessonId: 3);

        // ── יצירה ──

        // מזהים לא חוקיים נדחים ביצירה
        [Theory]
        [InlineData(0, 3)]
        [InlineData(7, 0)]
        [InlineData(-1, 3)]
        public void Create_RejectsInvalidIds(int studentId, int lessonId)
        {
            var act = () => LessonResult.Create(studentId, lessonId);

            act.Should().Throw<ArgumentException>();
        }

        // ── המסלול הרגיל: הציון הוא מה שהמערכת חישבה ──

        // סיכום רגיל: הציון הסופי והמחושב זהים, והשיעור נסגר
        [Fact]
        [Trait("Rule", "G-18")]
        public void CompleteWith_SetsBothScoresAndCompletes()
        {
            var result = NewResult();

            result.CompleteWith(81.7);

            result.FinalScore.Should().Be(81.7);
            result.ComputedScore.Should().Be(81.7);
            result.IsComplete.Should().BeTrue();
            result.IsFinalScoreOverridden.Should().BeFalse();
        }

        // אי אפשר לסכם שיעור שכבר סוכם
        [Fact]
        public void CompleteWith_Throws_WhenAlreadyComplete()
        {
            var result = NewResult();
            result.CompleteWith(80);

            var act = () => result.CompleteWith(90);

            act.Should().Throw<InvalidOperationException>();
        }

        // ── התקרה: 100 בלי בונוס, 150 איתו ──

        // ציון מחוץ לטווח נדחה, והטווח תלוי בקיום בונוס בשיעור
        [Theory]
        [InlineData(-0.5, false)]
        [InlineData(100.5, false)]
        [InlineData(150.5, true)]
        [Trait("Rule", "G-21")]
        public void CompleteWith_RejectsOutOfRange(double score, bool hasBonus)
        {
            var result = NewResult();

            var act = () => result.CompleteWith(score, hasBonus);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        // עם בונוס אפשר להגיע עד 150
        [Fact]
        [Trait("Rule", "G-21")]
        public void CompleteWith_AllowsBonusCeiling()
        {
            var result = NewResult();

            result.CompleteWith(150, hasBonus: true);

            result.FinalScore.Should().Be(150);
        }

        // ── דריסה: המורה קובעת ציון אחר מהמחושב ──

        // ⚠️ המחושב נשמר לצד הנדרס — בלעדיו אי אפשר לדעת בדיעבד ממה חרגו
        [Fact]
        [Trait("Rule", "G-24")]
        public void CompleteWithOverride_KeepsComputedScoreAlongsideOverride()
        {
            var result = NewResult();

            result.CompleteWithOverride(
                computedScore: 72.5,
                overrideScore: 85,
                teacherUserId: 3,
                reason: "הבדיקה האוטומטית נכשלה, נבדק ידנית");

            result.FinalScore.Should().Be(85);
            result.ComputedScore.Should().Be(72.5);
            result.IsFinalScoreOverridden.Should().BeTrue();
            result.FinalScoreOverriddenByUserId.Should().Be(3);
        }

        // אף תרגיל לא נבדק → אין מחושב, ובכל זאת אפשר לקבוע ציון מנומק
        [Fact]
        [Trait("Rule", "G-20")]
        public void CompleteWithOverride_AllowsNullComputedScore()
        {
            var result = NewResult();

            result.CompleteWithOverride(null, 90, 3, "כל ההגשות נכשלו טכנית");

            result.FinalScore.Should().Be(90);
            result.ComputedScore.Should().BeNull();
        }

        // דריסה בלי סיבה נדחית — הסיבה היא המעקב
        [Fact]
        [Trait("Rule", "G-24")]
        public void CompleteWithOverride_RequiresReason()
        {
            var result = NewResult();

            var act = () => result.CompleteWithOverride(72.5, 85, 3, "   ");

            act.Should().Throw<ArgumentException>();
        }

        // ── פתיחה מחדש: רשת הביטחון היחידה לטעות של מורה ──

        // פתיחה מחדש משאירה את הציון כהצעה לתיקון ומשחררת את השיעור
        [Fact]
        public void Reopen_ClearsCompletionButKeepsScore()
        {
            var result = NewResult();
            result.CompleteWith(80);

            result.Reopen();

            result.IsComplete.Should().BeFalse();
            result.FinalScore.Should().Be(80);
        }

        // אי אפשר לפתוח מחדש שיעור שלא סוכם
        [Fact]
        public void Reopen_Throws_WhenNotComplete()
        {
            var result = NewResult();

            var act = () => result.Reopen();

            act.Should().Throw<InvalidOperationException>();
        }

        // ⚠️ סיכום חוזר אחרי פתיחה מנקה דריסה קודמת — אחרת ציון מחושב היה נראה כידני
        [Fact]
        public void CompleteWith_ClearsPreviousOverride_AfterReopen()
        {
            var result = NewResult();
            result.CompleteWithOverride(72.5, 85, 3, "נבדק ידנית");
            result.Reopen();

            result.CompleteWith(72.5);

            result.IsFinalScoreOverridden.Should().BeFalse();
            result.FinalScoreOverrideReason.Should().BeNull();
            result.FinalScore.Should().Be(72.5);
        }
    }
}
