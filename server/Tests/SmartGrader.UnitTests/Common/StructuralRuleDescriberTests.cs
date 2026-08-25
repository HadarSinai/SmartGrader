using FluentAssertions;
using SmartGrader.Application.Services.CodeAnalysis;
using SmartGrader.Domain.Entities;
using Xunit;

namespace SmartGrader.UnitTests.Common
{
    /// <summary>
    /// הניסוח בעברית שהתלמידה קוראת. זהו החריג היחיד לכלל "לא לבדוק טקסט הודעות" —
    /// כאן <b>הנוסח עצמו הוא הנושא</b>: לשון נקבה היא דרישה קשיחה (בית ספר לבנות),
    /// ו"לא נמצא רקורסיה" היא שגיאה שקופצת לעין.
    /// </summary>
    public class StructuralRuleDescriberTests
    {
        private static StructuralRule Rule(RuleKind kind, CodeConstruct construct, int threshold = 0) =>
            new() { Kind = kind, Construct = construct, Threshold = threshold };

        private static StructuralRuleResult Result(CodeConstruct construct, int actualCount, params int[] lines) =>
            new()
            {
                Rule = Rule(RuleKind.MustUse, construct),
                ActualCount = actualCount,
                Passed = actualCount > 0,
                Lines = lines.ToList()
            };

        // ── ניסוח הדרישה ──

        // מילת מפתח באנגלית מקבלת מקף אחרי אות היחס: "ב-if" ולא "בif"
        [Fact]
        public void Describe_AddsHyphenBeforeLatinName()
        {
            StructuralRuleDescriber.Describe(Rule(RuleKind.MustUse, CodeConstruct.If))
                .Should().Be("חובה להשתמש ב-if");
        }

        // שם עברי נצמד בלי מקף
        [Fact]
        public void Describe_JoinsHebrewNameWithoutHyphen()
        {
            StructuralRuleDescriber.Describe(Rule(RuleKind.MustUse, CodeConstruct.Recursion))
                .Should().Be("חובה להשתמש ברקורסיה");
        }

        // ארבעת סוגי הדרישות מנוסחים כל אחד בדרכו
        [Fact]
        public void Describe_PhrasesForbiddenRule()
        {
            StructuralRuleDescriber.Describe(Rule(RuleKind.MustNotUse, CodeConstruct.Goto))
                .Should().Be("אסור להשתמש ב-goto");
        }

        [Fact]
        public void Describe_PhrasesAtMostRule()
        {
            StructuralRuleDescriber.Describe(Rule(RuleKind.AtMost, CodeConstruct.If, threshold: 3))
                .Should().Be("לכל היותר 3 if");
        }

        [Fact]
        public void Describe_PhrasesAtLeastRule()
        {
            StructuralRuleDescriber.Describe(Rule(RuleKind.AtLeast, CodeConstruct.For, threshold: 2))
                .Should().Be("לפחות 2 לולאת for");
        }

        // ── עומק קינון: אינו ספירת מופעים, ולכן מנוסח אחרת לגמרי ──

        // "לכל היותר 2 עומק קינון לולאות" לא היה נקרא — הניסוח שונה
        [Fact]
        public void Describe_PhrasesNestingDepthAsLimit()
        {
            StructuralRuleDescriber.Describe(Rule(RuleKind.AtMost, CodeConstruct.NestedLoopDepth, threshold: 2))
                .Should().Be("עומק קינון לולאות לא יעלה על 2");
        }

        [Fact]
        public void Describe_PhrasesForbiddenNesting()
        {
            StructuralRuleDescriber.Describe(Rule(RuleKind.MustNotUse, CodeConstruct.NestedLoopDepth))
                .Should().Be("אסור לקנן לולאות");
        }

        // ── לשון נקבה מול לשון זכר ──

        // ⚠️ "לא נמצאה רקורסיה" ולא "לא נמצא רקורסיה"
        [Theory]
        [InlineData(CodeConstruct.Recursion, "לא נמצאה רקורסיה בקוד")]
        [InlineData(CodeConstruct.Method, "לא נמצאה מתודה בקוד")]
        [InlineData(CodeConstruct.Class, "לא נמצאה מחלקה בקוד")]
        [InlineData(CodeConstruct.Inheritance, "לא נמצאה ירושה בקוד")]
        [InlineData(CodeConstruct.AnyLoop, "לא נמצאה לולאה בקוד")]
        public void DescribeFinding_UsesFeminineForm(CodeConstruct construct, string expected)
        {
            StructuralRuleDescriber.DescribeFinding(Result(construct, 0)).Should().Be(expected);
        }

        // מילים לועזיות ושמות זכר מקבלים "לא נמצא"
        [Theory]
        [InlineData(CodeConstruct.If, "לא נמצא if בקוד")]
        [InlineData(CodeConstruct.Linq, "לא נמצא LINQ בקוד")]
        [InlineData(CodeConstruct.Matrix, "לא נמצא מערך דו-ממדי בקוד")]
        [InlineData(CodeConstruct.Constructor, "לא נמצא בנאי בקוד")]
        public void DescribeFinding_UsesMasculineForm(CodeConstruct construct, string expected)
        {
            StructuralRuleDescriber.DescribeFinding(Result(construct, 0)).Should().Be(expected);
        }

        // ── ספירה ומספרי שורות ──

        // "נמצאו 1 מופעים" אינו עברית — מופע יחיד מנוסח בנפרד
        [Fact]
        public void DescribeFinding_PhrasesSingleOccurrence()
        {
            StructuralRuleDescriber.DescribeFinding(Result(CodeConstruct.If, 1, 3))
                .Should().Be("נמצא מופע אחד של if (בשורה 3)");
        }

        // כמה מופעים — לשון רבים ורשימת שורות
        [Fact]
        public void DescribeFinding_PhrasesMultipleOccurrences()
        {
            StructuralRuleDescriber.DescribeFinding(Result(CodeConstruct.If, 2, 3, 7))
                .Should().Be("נמצאו 2 מופעים של if (בשורות 3, 7)");
        }

        // בלי מספרי שורות אין סוגריים ריקים
        [Fact]
        public void DescribeFinding_OmitsLinesWhenNoneReported()
        {
            StructuralRuleDescriber.DescribeFinding(Result(CodeConstruct.If, 1))
                .Should().Be("נמצא מופע אחד של if");
        }

        // עומק קינון: אין לולאות בכלל מול עומק שנמדד
        [Fact]
        public void DescribeFinding_ReportsNoLoopsForZeroDepth()
        {
            StructuralRuleDescriber.DescribeFinding(Result(CodeConstruct.NestedLoopDepth, 0))
                .Should().Be("לא נמצאו לולאות בקוד");
        }

        [Fact]
        public void DescribeFinding_ReportsMeasuredDepth()
        {
            StructuralRuleDescriber.DescribeFinding(Result(CodeConstruct.NestedLoopDepth, 2, 5))
                .Should().Be("עומק הקינון בפועל: 2 (בשורה 5)");
        }

        // ── שורת הכישלון המוצגת לתלמידה ──

        // הדרישה ומה שנמצא בפועל, בשורה אחת
        [Fact]
        public void DescribeFailure_CombinesRuleAndFinding()
        {
            var result = new StructuralRuleResult
            {
                Rule = Rule(RuleKind.MustUse, CodeConstruct.Recursion),
                ActualCount = 0,
                Passed = false
            };

            StructuralRuleDescriber.DescribeFailure(result)
                .Should().Be("❌ הדרישה \"חובה להשתמש ברקורסיה\" לא התקיימה — לא נמצאה רקורסיה בקוד");
        }

        // ── שם המבנה ──

        // לכל ערך בקטלוג יש שם קריא, ואף אחד לא נופל לשם ה-enum באנגלית
        [Theory]
        [InlineData(CodeConstruct.Array, "מערך חד-ממדי")]
        [InlineData(CodeConstruct.Matrix, "מערך דו-ממדי")]
        [InlineData(CodeConstruct.BoolVariable, "משתנה בוליאני")]
        [InlineData(CodeConstruct.Constant, "קבוע (const)")]
        [InlineData(CodeConstruct.Ternary, "אופרטור תנאי (?:)")]
        public void ConstructName_ReturnsHebrewName(CodeConstruct construct, string expected)
        {
            StructuralRuleDescriber.ConstructName(construct).Should().Be(expected);
        }

        // מילות מפתח נשארות באנגלית — כך התלמידה כותבת אותן בפועל
        [Theory]
        [InlineData(CodeConstruct.If, "if")]
        [InlineData(CodeConstruct.Switch, "switch")]
        [InlineData(CodeConstruct.TryCatch, "try-catch")]
        [InlineData(CodeConstruct.List, "List")]
        public void ConstructName_KeepsKeywordsInEnglish(CodeConstruct construct, string expected)
        {
            StructuralRuleDescriber.ConstructName(construct).Should().Be(expected);
        }
    }
}
