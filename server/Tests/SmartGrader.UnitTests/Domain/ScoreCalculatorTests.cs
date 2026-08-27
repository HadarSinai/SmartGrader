using FluentAssertions;
using SmartGrader.Domain.Entities;
using SmartGrader.Domain.Services;
using Xunit;

namespace SmartGrader.UnitTests.Domain
{
    /// <summary>
    /// הקוד היחיד שמייצר את המספר בתעודה. כל טסט כאן משועתק מהערה קיימת
    /// ב-<c>ScoreCalculator.cs</c> — הטסטים לא ממציאים כללים, הם נועלים אותם.
    /// </summary>
    public class ScoreCalculatorTests
    {
        // ── עוזרי בנייה: ערכים מילוליים, בלי לוגיקה ──

        private static TestCaseResult PassedTest(bool isCore = true) =>
            new(Input: "1", Expected: "2", Actual: "2", Passed: true, Error: null, IsCore: isCore);

        private static TestCaseResult FailedTest(bool isCore = true) =>
            new(Input: "1", Expected: "2", Actual: "9", Passed: false, Error: null, IsCore: isCore);

        private static StructuralRuleResult Rule(RuleSeverity severity, int points, bool passed) =>
            new()
            {
                Rule = new StructuralRule { Severity = severity, Points = points, Kind = RuleKind.MustUse },
                Passed = passed
            };

        private static readonly IReadOnlyList<TestCaseResult> NoTests = Array.Empty<TestCaseResult>();
        private static readonly IReadOnlyList<StructuralRuleResult> NoRules = Array.Empty<StructuralRuleResult>();

        // ── מקרה "הכול שערים" (ScoreCalculator.cs שורות 63-79) ──

        // תרגיל שכולו שערים: אפס טסטים ואפס דרישות מנוקדות → ציון מלא, לא אפס
        [Fact]
        [Trait("Rule", "G-9")]
        public void Total_IsMaxScore_WhenNoTestsAndNoScoredRules()
        {
            var result = ScoreCalculator.Calculate(80, NoTests, NoRules);

            result.Total.Should().Be(100);
            result.TestPoints.Should().Be(0);
            result.RulePoints.Should().Be(0);
        }

        // דרישות חוסמות בלבד הן עדיין "הכול שערים" — חוסמת אינה נושאת נקודות
        [Fact]
        [Trait("Rule", "G-9")]
        public void Total_IsMaxScore_WhenOnlyBlockingRulesExist()
        {
            var rules = new[] { Rule(RuleSeverity.Blocking, 0, passed: true) };

            var result = ScoreCalculator.Calculate(0, NoTests, rules);

            result.Total.Should().Be(100);
        }

        // תרגיל בונוס שכולו שערים → התקרה המוגדלת, לא 100
        [Fact]
        [Trait("Rule", "G-9")]
        public void Total_IsBonusMaxScore_WhenAllGatesAndMaxScoreAbove100()
        {
            var result = ScoreCalculator.Calculate(0, NoTests, NoRules, maxScore: 120);

            result.Total.Should().Be(120);
        }

        // רשימות null לא מפילות — מתנהגות כמו ריקות (שורות 36-37)
        [Fact]
        public void Calculate_TreatsNullListsAsEmpty()
        {
            var result = ScoreCalculator.Calculate(80, null!, null!);

            result.Total.Should().Be(100);
        }

        // ── total == 0 אינו "נכשל בכול" (שורות 53-54, הרגרסיה המתועדת) ──

        // אין טסטים אבל יש דרישות מנוקדות → הציון מהדרישות בלבד, לא אפס גורף
        [Fact]
        [Trait("Rule", "G-8")]
        public void Total_IsRulePointsOnly_WhenNoTestsRan()
        {
            var rules = new[] { Rule(RuleSeverity.Scored, 20, passed: true) };

            var result = ScoreCalculator.Calculate(80, NoTests, rules);

            result.Total.Should().Be(20);
            result.TestPoints.Should().Be(0);
            result.RulePoints.Should().Be(20);
        }

        // ── שער מקרי הליבה (שורות 46-57) ──

        // מקרה ליבה אחד נכשל → אפס נקודות טסטים, גם כשאחרים עברו
        [Fact]
        [Trait("Rule", "G-7")]
        public void TestPoints_IsZero_WhenAnyCoreTestFails()
        {
            var tests = new[] { FailedTest(isCore: true), PassedTest(isCore: false), PassedTest(isCore: false) };

            var result = ScoreCalculator.Calculate(80, tests, NoRules);

            result.TestPoints.Should().Be(0);
            result.Total.Should().Be(0);
            result.AllCorePassed.Should().BeFalse();
        }

        // כישלון במקרה שאינו ליבה לא סוגר את השער — ניקוד יחסי על כל המקרים
        [Fact]
        [Trait("Rule", "G-6")]
        public void TestPoints_IsProportional_WhenOnlyNonCoreTestsFail()
        {
            var tests = new[]
            {
                PassedTest(), PassedTest(), PassedTest(), PassedTest(),
                FailedTest(isCore: false)
            };

            var result = ScoreCalculator.Calculate(80, tests, NoRules);

            result.TestPoints.Should().Be(64);
            result.AllCorePassed.Should().BeTrue();
        }

        // ── עיגול (שורות 85-86: הערך הגולמי הוצג כמו שהוא בשישה מסכים) ──

        // 2 מתוך 3 → 66.7, לא 66.66666666666666
        [Fact]
        [Trait("Rule", "G-13")]
        public void Total_IsRoundedToOneDecimal()
        {
            var tests = new[] { PassedTest(), PassedTest(), FailedTest(isCore: false) };

            var result = ScoreCalculator.Calculate(100, tests, NoRules);

            result.Total.Should().Be(66.7);
            result.TestPoints.Should().Be(66.7);
        }

        // ── התקרה (שורות 81-83) ──

        // רובריקה עקומה שסוכמת מעל התקרה נחתכת בתקרה, לא זולגת מעליה
        [Fact]
        [Trait("Rule", "G-12")]
        public void Total_IsCappedAtMaxScore()
        {
            var tests = new[] { PassedTest() };
            var rules = new[] { Rule(RuleSeverity.Scored, 40, passed: true) };

            var result = ScoreCalculator.Calculate(80, tests, rules);

            result.Total.Should().Be(100);
        }

        // תרגיל בונוס: תקרה מעל 100 מכובדת — החיתוך הוא ב-maxScore, לא ב-100
        [Fact]
        [Trait("Rule", "G-12")]
        public void Total_HonoursBonusMaxScore()
        {
            var tests = new[] { PassedTest() };
            var rules = new[] { Rule(RuleSeverity.Scored, 30, passed: true) };

            var result = ScoreCalculator.Calculate(100, tests, rules, maxScore: 120);

            result.Total.Should().Be(120);
        }

        // ── דרישה היא תנאי, לא מדידה (שורה 59) ──

        // דרישה מנוקדת שנכשלה מפסידה את כל הנקודות שלה — אין ניקוד חלקי
        [Fact]
        [Trait("Rule", "G-10")]
        public void RulePoints_ExcludesFailedRules()
        {
            var rules = new[]
            {
                Rule(RuleSeverity.Scored, 10, passed: true),
                Rule(RuleSeverity.Scored, 15, passed: false)
            };

            var result = ScoreCalculator.Calculate(0, NoTests, rules);

            result.RulePoints.Should().Be(10);
            result.Total.Should().Be(10);
        }

        // רק דרישות מנוקדות נושאות נקודות — חוסמות והמלצות לא, גם אם מולאו להן נקודות בטעות
        [Fact]
        [Trait("Rule", "G-11")]
        public void RulePoints_IgnoresBlockingAndAdvisoryRules()
        {
            var rules = new[]
            {
                Rule(RuleSeverity.Scored, 10, passed: true),
                Rule(RuleSeverity.Blocking, 50, passed: true),
                Rule(RuleSeverity.Advisory, 50, passed: true)
            };

            var result = ScoreCalculator.Calculate(0, NoTests, rules);

            result.RulePoints.Should().Be(10);
            result.RulesAllocation.Should().Be(10);
            result.Total.Should().Be(10);
        }

        // ── הפירוק עצמו: המסך מציג "בדיקות X · דרישות Y · סה"כ Z" ──

        // הפירוק מדווח הקצאות וספירות כפי שהיו בפועל
        [Fact]
        public void Breakdown_ReportsCountsAndAllocations()
        {
            var tests = new[] { PassedTest(), FailedTest(isCore: false) };
            var rules = new[] { Rule(RuleSeverity.Scored, 20, passed: true) };

            var result = ScoreCalculator.Calculate(80, tests, rules);

            result.PassedTests.Should().Be(1);
            result.TotalTests.Should().Be(2);
            result.TestsAllocation.Should().Be(80);
            result.RulesAllocation.Should().Be(20);
            result.TestPoints.Should().Be(40);
            result.RulePoints.Should().Be(20);
            result.Total.Should().Be(60);
        }
    }
}
