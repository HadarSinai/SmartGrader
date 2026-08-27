using FluentAssertions;
using SmartGrader.Domain.Entities;
using SmartGrader.Domain.Services;
using SmartGrader.UnitTests.Helpers;
using Xunit;

namespace SmartGrader.UnitTests.Domain
{
    /// <summary>
    /// הציון הסופי של השיעור — הנוסחה שעברה מהדפדפן לשרת בדיוק כדי שלא תהיה עקיפה.
    /// כל טסט משועתק מהערה קיימת ב-<c>LessonScoreCalculator.cs</c>.
    /// </summary>
    public class LessonScoreCalculatorTests
    {
        private static readonly IReadOnlyList<Submission> NoSubmissions = Array.Empty<Submission>();

        // ── ⚠️ תרגיל בלי ציון מדולג ולא נספר כאפס (שורות 41-42) ──

        // תרגיל שעדיין בבדיקה לא מוריד את הממוצע — מדולג, לא אפס
        [Fact]
        [Trait("Rule", "G-19")]
        public void ComputedScore_SkipsUngradedAssignments()
        {
            var assignments = new Assignment[] { new TestAssignment(1), new TestAssignment(2) };
            var submissions = new[] { new SubmissionBuilder(7, 1).Graded(90).Build() };

            var result = LessonScoreCalculator.Calculate(assignments, submissions);

            result.ComputedScore.Should().Be(90);
            result.GradedCount.Should().Be(1);
            result.UngradedCount.Should().Be(1);
        }

        // הגשה שקיימת אבל בלי ציון (עדיין בבדיקה) נספרת כלא-נבדקה, לא כאפס
        [Fact]
        [Trait("Rule", "G-19")]
        public void UngradedCount_CountsSubmissionWithoutScore()
        {
            var assignments = new Assignment[] { new TestAssignment(1) };
            var submissions = new[] { new SubmissionBuilder(7, 1).Build() };

            var result = LessonScoreCalculator.Calculate(assignments, submissions);

            result.ComputedScore.Should().BeNull();
            result.GradedCount.Should().Be(0);
            result.UngradedCount.Should().Be(1);
        }

        // ── אף תרגיל לא נבדק → null, לא 0 ──

        // אין ממה לחשב → אין ציון מחושב בכלל, לא אפס מטעה
        [Fact]
        [Trait("Rule", "G-20")]
        public void ComputedScore_IsNull_WhenNothingGraded()
        {
            var assignments = new Assignment[] { new TestAssignment(1) };

            var result = LessonScoreCalculator.Calculate(assignments, NoSubmissions);

            result.ComputedScore.Should().BeNull();
            result.GradedCount.Should().Be(0);
            result.UngradedCount.Should().Be(1);
        }

        // רשימות null לא מפילות — מתנהגות כמו ריקות (שורות 25-26)
        [Fact]
        public void Calculate_TreatsNullListsAsEmpty()
        {
            var result = LessonScoreCalculator.Calculate(null!, null!);

            result.ComputedScore.Should().BeNull();
            result.GradedCount.Should().Be(0);
            result.UngradedCount.Should().Be(0);
        }

        // ── עיגול (שורה 51: אותו כלל כמו ScoreCalculator) ──

        // ממוצע 90, 80, 75 → 81.7, לא 81.66666666666667
        [Fact]
        [Trait("Rule", "G-13")]
        public void ComputedScore_IsRoundedToOneDecimal()
        {
            var assignments = new Assignment[] { new TestAssignment(1), new TestAssignment(2), new TestAssignment(3) };
            var submissions = new[]
            {
                new SubmissionBuilder(7, 1).Graded(90).Build(),
                new SubmissionBuilder(7, 2).Graded(80).Build(),
                new SubmissionBuilder(7, 3).Graded(75).Build()
            };

            var result = LessonScoreCalculator.Calculate(assignments, submissions);

            result.ComputedScore.Should().Be(81.7);
        }

        // ── ההגשה המאוחרת קובעת (שורות 28-34) ──

        // שתי הגשות לאותו תרגיל: המאוחרת קובעת, גם כשהמוקדמת ראשונה ברשימה
        [Fact]
        [Trait("Rule", "G-25")]
        public void ComputedScore_UsesLatestSubmissionPerAssignment()
        {
            var assignments = new Assignment[] { new TestAssignment(1) };
            var submissions = new[]
            {
                new SubmissionBuilder(7, 1).SubmittedAt(new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc)).Graded(50).Build(),
                new SubmissionBuilder(8, 1).SubmittedAt(new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc)).Graded(90).Build()
            };

            var result = LessonScoreCalculator.Calculate(assignments, submissions);

            result.ComputedScore.Should().Be(90);
        }

        // ── ⚠️ HasBonus נגזר מהתרגילים בפועל, לא ממה שהלקוח שלח (שורה 74) ──

        // תרגיל בונוס בשיעור → התקרה 150; בלעדיו → 100
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [Trait("Rule", "G-21")]
        public void HasBonus_DerivesFromAssignments(bool isBonus)
        {
            var assignments = new Assignment[] { new TestAssignment(1, isBonus: isBonus) };

            var result = LessonScoreCalculator.Calculate(assignments, NoSubmissions);

            result.HasBonus.Should().Be(isBonus);
        }

        // ── Matches: ההצעה המחושבת עצמה אינה "חריגה" (שורות 58-64) ──

        // הזנת הציון המחושב כלשונו לא דורשת נימוק דריסה
        [Theory]
        [InlineData(81.7, 81.7, true)]   // ההצעה עצמה
        [InlineData(81.7, 81.74, true)]  // בתוך סובלנות העיגול
        [InlineData(81.7, 81.8, false)]  // כבר ציון אחר
        [InlineData(81.7, 90.0, false)]  // דריסה מפורשת
        [Trait("Rule", "G-22")]
        public void Matches_ToleratesOneDecimalRounding(double computed, double entered, bool expected)
        {
            LessonScoreCalculator.Matches(computed, entered).Should().Be(expected);
        }

        // אין ציון מחושב → כל ציון שהוזן הוא דריסה מנומקת
        [Fact]
        [Trait("Rule", "G-22")]
        public void Matches_IsFalse_WhenNothingComputed()
        {
            LessonScoreCalculator.Matches(null, 90).Should().BeFalse();
        }
    }
}
