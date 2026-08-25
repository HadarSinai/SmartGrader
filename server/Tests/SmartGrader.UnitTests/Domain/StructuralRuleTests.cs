using FluentAssertions;
using SmartGrader.Domain.Entities;
using Xunit;

namespace SmartGrader.UnitTests.Domain
{
    /// <summary>
    /// <see cref="StructuralRule.IsSatisfiedBy"/> — הפונקציה היחידה שמכריעה אם דרישה
    /// התקיימה. גם המנתח וגם הטסטים עוברים דרכה, כדי שלא תהיה פרשנות שנייה לאותו כלל.
    /// </summary>
    public class StructuralRuleTests
    {
        // הכרעת הדרישה בכל ארבעת הסוגים, כולל הגבולות
        [Theory]
        [InlineData(RuleKind.MustUse, 0, 0, false)]     // חובה — אפס מופעים נכשל
        [InlineData(RuleKind.MustUse, 0, 1, true)]      // חובה — מופע אחד מספיק
        [InlineData(RuleKind.MustNotUse, 0, 0, true)]   // אסור — אפס מופעים עובר
        [InlineData(RuleKind.MustNotUse, 0, 1, false)]  // אסור — מופע אחד נכשל
        [InlineData(RuleKind.AtLeast, 2, 1, false)]     // לפחות 2 — אחד לא מספיק
        [InlineData(RuleKind.AtLeast, 2, 2, true)]      // לפחות 2 — בדיוק על הסף עובר
        [InlineData(RuleKind.AtLeast, 2, 3, true)]      // לפחות 2 — יותר עובר
        [InlineData(RuleKind.AtMost, 3, 3, true)]       // לכל היותר 3 — בדיוק על הסף עובר
        [InlineData(RuleKind.AtMost, 3, 4, false)]      // לכל היותר 3 — ארבעה נכשל
        [InlineData(RuleKind.AtMost, 3, 0, true)]       // לכל היותר 3 — אפס עובר
        public void IsSatisfiedBy_DecidesPerKind(RuleKind kind, int threshold, int actual, bool expected)
        {
            var rule = new StructuralRule { Kind = kind, Threshold = threshold };

            rule.IsSatisfiedBy(actual).Should().Be(expected);
        }

        // הספירה הצפויה לניסוח ההסבר — 1 לחובה, 0 לאיסור, הסף לשאר
        [Theory]
        [InlineData(RuleKind.MustUse, 5, 1)]
        [InlineData(RuleKind.MustNotUse, 5, 0)]
        [InlineData(RuleKind.AtLeast, 3, 3)]
        [InlineData(RuleKind.AtMost, 2, 2)]
        public void ExpectedCount_FollowsKind(RuleKind kind, int threshold, int expected)
        {
            var rule = new StructuralRule { Kind = kind, Threshold = threshold };

            rule.ExpectedCount.Should().Be(expected);
        }
    }
}
