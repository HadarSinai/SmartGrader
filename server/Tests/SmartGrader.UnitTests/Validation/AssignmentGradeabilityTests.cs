using FluentAssertions;
using SmartGrader.Application.Common.Validation;
using SmartGrader.Application.Dtos.Assignments;
using SmartGrader.Domain.Entities;
using SmartGrader.UnitTests.Helpers;
using Xunit;

namespace SmartGrader.UnitTests.Validation
{
    /// <summary>
    /// הכללים שמונעים שמירת תרגיל שאי אפשר לנקד. 🔴 בלי הראשון שבהם, תרגיל בלי אף מקרה
    /// בדיקה נשמר בשקט, הציון מחושב על <c>Total == 0</c>, ו<b>כל התלמידות מקבלות 0</b>
    /// בלי שאף אחת עשתה משהו לא בסדר.
    /// </summary>
    public class AssignmentGradeabilityTests
    {
        private static TestCaseDto Test() =>
            new() { Input = "1", Expected = "2" };

        private static StructuralRuleDto Rule(string severity, int points = 0) =>
            new()
            {
                Kind = nameof(RuleKind.MustUse),
                Construct = nameof(CodeConstruct.Recursion),
                Severity = severity,
                Points = points
            };

        private static StructuralRuleDto Scored(int points) => Rule(nameof(RuleSeverity.Scored), points);
        private static StructuralRuleDto Blocking() => Rule(nameof(RuleSeverity.Blocking));

        // ── ⚠️ "או", לא "וגם" ──

        // תרגיל מחלקות — "כתבי מחלקה עם בנאי ושתי תכונות" — אין לו מה להריץ, והוא מנוקד
        // על המבנה בלבד. דרישה בלי מקרי בדיקה היא תרגיל חוקי לגמרי.
        [Fact]
        public void IsGradeable_AcceptsStructuralRulesWithoutTests()
        {
            AssignmentGradeability.IsGradeable(Array.Empty<TestCaseDto>(), new[] { Blocking() })
                .Should().BeTrue();
        }

        // וגם ההפך — מקרי בדיקה בלי דרישות מבניות
        [Fact]
        public void IsGradeable_AcceptsTestsWithoutStructuralRules()
        {
            AssignmentGradeability.IsGradeable(new[] { Test() }, Array.Empty<StructuralRuleDto>())
                .Should().BeTrue();
        }

        // 🔴 רק תרגיל שאין לו לא זה ולא זה נפסל — זה המקרה שנותן 0 לכולן
        [Fact]
        public void IsGradeable_RejectsAnAssignmentWithNeither()
        {
            AssignmentGradeability.IsGradeable(Array.Empty<TestCaseDto>(), Array.Empty<StructuralRuleDto>())
                .Should().BeFalse();
        }

        // רשימות null מתנהגות כמו ריקות ולא מפילות את הבדיקה
        [Fact]
        public void IsGradeable_TreatsNullListsAsEmpty()
        {
            AssignmentGradeability.IsGradeable(null, null).Should().BeFalse();
        }

        // ── הרובריקה מסתכמת בתקרה בדיוק ──

        // בלי דרישות מנוקדות הבדיקות מקבלות את כל 100 — תרגיל רגיל נשאר מהיר ליצירה
        [Fact]
        public void HasValidRubric_AcceptsTestsAloneAtFullAllocation()
        {
            AssignmentGradeability.HasValidRubric(100, 100, new[] { Test() }, Array.Empty<StructuralRuleDto>())
                .Should().BeTrue();
        }

        // בדיקות ודרישות מנוקדות שמסתכמות יחד בתקרה
        [Fact]
        public void HasValidRubric_AcceptsTestsAndScoredRulesThatSumToTheCeiling()
        {
            AssignmentGradeability.HasValidRubric(100, 80, new[] { Test() }, new[] { Scored(20) })
                .Should().BeTrue();
        }

        // סכום גבוה מהתקרה נפסל
        [Fact]
        public void HasValidRubric_RejectsAnOverAllocatedRubric()
        {
            AssignmentGradeability.HasValidRubric(100, 100, new[] { Test() }, new[] { Scored(10) })
                .Should().BeFalse();
        }

        // וגם סכום נמוך ממנה — "בדיוק", לא "לכל היותר"
        [Fact]
        public void HasValidRubric_RejectsAnUnderAllocatedRubric()
        {
            AssignmentGradeability.HasValidRubric(100, 70, new[] { Test() }, new[] { Scored(20) })
                .Should().BeFalse();
        }

        // ⚠️ המקרה שקל לפספס: בלי בדיקות ובלי דרישות מנוקדות — רק חוסמות. אין לְמה
        // להקצות נקודות, וזו הצורה הטבעית של תרגיל מחלקות. חייב לעבור.
        [Fact]
        public void HasValidRubric_AcceptsBlockingRulesOnlyWithNothingToAllocate()
        {
            AssignmentGradeability.HasValidRubric(100, 0, Array.Empty<TestCaseDto>(), new[] { Blocking() })
                .Should().BeTrue();
        }

        // בלי בדיקות, הדרישות המנוקדות לבדן חייבות לכסות את התקרה
        [Fact]
        public void HasValidRubric_RequiresScoredRulesAloneToCoverTheCeiling()
        {
            AssignmentGradeability.HasValidRubric(100, 0, Array.Empty<TestCaseDto>(), new[] { Scored(90) })
                .Should().BeFalse();
        }

        // ⚠️ יש בדיקות אך הוקצו להן 0 נקודות ואין דרישות מנוקדות: זו טעות הקלדה ולא
        // בחירה — התלמידה הייתה מריצה בדיקות שאינן שוות דבר
        [Fact]
        public void HasValidRubric_RejectsTestsWorthNothing()
        {
            AssignmentGradeability.HasValidRubric(100, 0, new[] { Test() }, Array.Empty<StructuralRuleDto>())
                .Should().BeFalse();
        }

        // הקצאה מחוץ לתחום נפסלת בשני הקצוות
        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void HasValidRubric_RejectsAllocationOutsideTheCeiling(int testsAllocation)
        {
            AssignmentGradeability.HasValidRubric(100, testsAllocation, new[] { Test() }, Array.Empty<StructuralRuleDto>())
                .Should().BeFalse();
        }

        // ⚠️ "התקרה", לא "100": בתרגיל בונוס הרובריקה מסתכמת גבוה יותר
        [Fact]
        public void HasValidRubric_UsesTheBonusCeiling()
        {
            AssignmentGradeability.HasValidRubric(120, 120, new[] { Test() }, Array.Empty<StructuralRuleDto>())
                .Should().BeTrue();
        }

        // ── התקרה בטופס והתקרה בישות הן אותו מספר ──

        // 🔴 שני חישובים נפרדים לאותו דבר: אם יסטו, הטופס והציון ימדדו שני דברים שונים
        [Theory]
        [InlineData(false, 0)]
        [InlineData(false, 20)]
        [InlineData(true, 0)]
        [InlineData(true, 20)]
        [InlineData(true, -5)]
        [InlineData(true, 19.6)]
        public void MaxScoreOf_AgreesWithTheEntity(bool isBonus, double bonusValue)
        {
            var assignment = new TestAssignment(1, isBonus) { BonusValue = bonusValue };

            AssignmentGradeability.MaxScoreOf(isBonus, bonusValue).Should().Be(assignment.MaxScore);
        }

        // ── דרישה מנוקדת חייבת לשאת נקודות ──

        // דרישה מנוקדת בלי נקודות אינה עושה כלום — כנראה נשכח למלא את השדה
        [Fact]
        public void ScoredRulesCarryPoints_RejectsAScoredRuleWorthNothing()
        {
            AssignmentGradeability.ScoredRulesCarryPoints(new[] { Scored(0) }).Should().BeFalse();
        }

        [Fact]
        public void ScoredRulesCarryPoints_AcceptsAScoredRuleWithPoints()
        {
            AssignmentGradeability.ScoredRulesCarryPoints(new[] { Scored(10) }).Should().BeTrue();
        }

        // דרישה חוסמת היא שער ואינה נושאת ניקוד — 0 נקודות בה תקין
        [Fact]
        public void ScoredRulesCarryPoints_IgnoresBlockingRules()
        {
            AssignmentGradeability.ScoredRulesCarryPoints(new[] { Blocking() }).Should().BeTrue();
        }

        // ⚠️ הדרגה מגיעה מהלקוח כמחרוזת, ולכן ההשוואה אינה תלוית רישיות
        [Fact]
        public void ScoredRulesCarryPoints_MatchesSeverityCaseInsensitively()
        {
            AssignmentGradeability.ScoredRulesCarryPoints(new[] { Rule("scored", points: 0) })
                .Should().BeFalse();
        }
    }
}
