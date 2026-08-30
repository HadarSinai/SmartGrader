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

        // ── ⚠️ התקרה נגזרת מהתרגילים בפועל, לא ממה שהלקוח שלח ──

        // התקרה היא 100 ועוד סכום ה-BonusValue — לא 150 קבוע, ולא דגל
        [Theory]
        [InlineData(false, 0, 100)]
        [InlineData(true, 20, 120)]
        [InlineData(true, 0, 100)]
        [InlineData(true, -5, 100)]   // BonusValue שלילי מנוטרל ולא מוריד את התקרה
        [Trait("Rule", "G-21")]
        public void MaxScore_IsOneHundredPlusTheBonusValues(
            bool isBonus, double bonusValue, double expected)
        {
            var assignments = new Assignment[]
            {
                new TestAssignment(1),
                new TestAssignment(2, isBonus: isBonus, bonusValue: bonusValue)
            };

            var result = LessonScoreCalculator.Calculate(assignments, NoSubmissions);

            result.MaxScore.Should().Be(expected);
        }

        // שני תרגילי בונוס מצטברים בתקרה
        [Fact]
        [Trait("Rule", "G-21")]
        public void MaxScore_SumsEveryBonusInTheLesson()
        {
            var assignments = new Assignment[]
            {
                new TestAssignment(1),
                new TestAssignment(2, isBonus: true, bonusValue: 20),
                new TestAssignment(3, isBonus: true, bonusValue: 5)
            };

            var result = LessonScoreCalculator.Calculate(assignments, NoSubmissions);

            result.MaxScore.Should().Be(125);
        }

        // ── מודל הבונוס: הבסיס הוא ממוצע החובה, והבונוס מתווסף אליו ──

        // שיעור עם 3 תרגילים, השלישי בונוס של 20. תקרה 120.
        // ⚠️ ארבע השורות של טבלת הדוגמאות בתוכנית, אחת לאחת.
        [Theory]
        [InlineData(100, 100, 100.0, 100, 20, 120)]   // עשתה הכול → 120, לא 106.7
        [InlineData(100, 100, 70.0, 100, 14, 114)]    // בונוס חלקי: 20 × 0.7
        [InlineData(80, 90, null, 85, 0, 85)]         // דילגה על הבונוס — אין עונש
        [InlineData(80, 90, 100.0, 85, 20, 105)]      // בסיס חלש, בונוס מלא
        [Trait("Rule", "G-18")]
        [Trait("Rule", "G-26")]
        public void BonusIsAddedToTheBaseAverage_NotAveragedIntoIt(
            double first, double second, double? bonus,
            double expectedBase, double expectedBonusPoints, double expectedTotal)
        {
            var assignments = new Assignment[]
            {
                new TestAssignment(1),
                new TestAssignment(2),
                new TestAssignment(3, isBonus: true, bonusValue: 20)
            };

            var submissions = new List<Submission>
            {
                new SubmissionBuilder(7, 1).Graded(first).Build(),
                new SubmissionBuilder(7, 2).Graded(second).Build()
            };

            if (bonus.HasValue)
                submissions.Add(new SubmissionBuilder(7, 3).Graded(bonus.Value).Build());

            var result = LessonScoreCalculator.Calculate(assignments, submissions);

            result.BaseScore.Should().Be(expectedBase);
            result.BonusPoints.Should().Be(expectedBonusPoints);
            result.ComputedScore.Should().Be(expectedTotal);
            result.MaxScore.Should().Be(120);
        }

        // ⚠️ בונוס שנעשה בלי אף תרגיל חובה שנבדק אינו מייצר ציון: אין בסיס להוסיף אליו,
        // וציון סופי אז אפשרי רק כדריסה מנומקת
        [Fact]
        [Trait("Rule", "G-20")]
        public void ComputedScore_IsNull_WhenOnlyTheBonusIsGraded()
        {
            var assignments = new Assignment[]
            {
                new TestAssignment(1),
                new TestAssignment(2, isBonus: true, bonusValue: 20)
            };
            var submissions = new[] { new SubmissionBuilder(7, 2).Graded(100).Build() };

            var result = LessonScoreCalculator.Calculate(assignments, submissions);

            result.ComputedScore.Should().BeNull();
            result.BaseScore.Should().BeNull();
            result.BonusPoints.Should().Be(20);
        }

        // שיעור שכולו בונוס אינו יכול לייצר בסיס — הדגל הוא מה שנותן ל-handler להסביר למה
        [Fact]
        [Trait("Rule", "G-20")]
        public void HasRequiredAssignment_IsFalse_WhenEveryAssignmentIsBonus()
        {
            var assignments = new Assignment[]
            {
                new TestAssignment(1, isBonus: true, bonusValue: 10),
                new TestAssignment(2, isBonus: true, bonusValue: 10)
            };

            var result = LessonScoreCalculator.Calculate(assignments, NoSubmissions);

            result.HasRequiredAssignment.Should().BeFalse();
            result.MaxScore.Should().Be(120);
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
