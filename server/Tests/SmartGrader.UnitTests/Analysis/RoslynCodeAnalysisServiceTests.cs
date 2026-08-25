using FluentAssertions;
using SmartGrader.Application.Services.CodeAnalysis;
using SmartGrader.Domain.Entities;
using SmartGrader.Infrastructure.Services.CodeAnalysis;
using Xunit;

namespace SmartGrader.UnitTests.Analysis
{
    /// <summary>
    /// מנוע הדרישות המבניות. פונקציה טהורה: (קוד, דרישות) → תוצאה, ולכן נבדק כאן עם
    /// המימוש האמיתי — fake של <c>ICodeAnalysisService</c> היה בודק את ה-fake.
    /// המקרים משועתקים מההערות בקוד ומארבע המלכודות בסקיל backend-roslyn-code-analysis.
    /// </summary>
    public class RoslynCodeAnalysisServiceTests
    {
        private static readonly RoslynCodeAnalysisService Analyzer = new();

        private static StructuralRule MustUse(CodeConstruct construct, RuleSeverity severity = RuleSeverity.Scored) =>
            new() { Kind = RuleKind.MustUse, Construct = construct, Severity = severity };

        /// <summary>מריץ דרישה אחת ומחזיר את התוצאה שלה.</summary>
        private static StructuralRuleResult Check(CodeConstruct construct, string source) =>
            Analyzer.Analyze(source, new[] { MustUse(construct) }).Results[0];

        // ── ספירה נכונה בקוד תואם: זוג עובר לכל מבנה בקטלוג ──

        [Theory]
        // תנאים — כולל if/else-if שנספר כשניים (⚠️ מתועד: זו הספירה הנכונה ל"לכל היותר 3 if")
        [InlineData(CodeConstruct.If, "class C { void M(int x) { if (x > 0) { } } }", 1)]
        [InlineData(CodeConstruct.If, "class C { void M(int x) { if (x > 0) { } else if (x < 0) { } else { } } }", 2)]
        [InlineData(CodeConstruct.Switch, "class C { void M(int x) { switch (x) { case 1: break; } } }", 1)]
        [InlineData(CodeConstruct.Switch, "class C { int M(int x) => x switch { _ => 1 }; }", 1)]
        [InlineData(CodeConstruct.Ternary, "class C { int M(int x) => x > 0 ? 1 : 2; }", 1)]
        // לולאות — כולל foreach על טאפל מפורק (מתועד: foreach לכל דבר עבור התלמידה)
        [InlineData(CodeConstruct.For, "class C { void M() { for (int i = 0; i < 3; i++) { } } }", 1)]
        [InlineData(CodeConstruct.While, "class C { void M() { while (true) { } } }", 1)]
        [InlineData(CodeConstruct.DoWhile, "class C { void M() { int i = 0; do { i++; } while (i < 3); } }", 1)]
        [InlineData(CodeConstruct.Foreach, "class C { void M(int[] a) { foreach (var x in a) { } } }", 1)]
        [InlineData(CodeConstruct.Foreach, "class C { void M((int, int)[] p) { foreach (var (a, b) in p) { } } }", 1)]
        [InlineData(CodeConstruct.AnyLoop, "class C { void M() { for (int i = 0; i < 3; i++) { } while (true) { } } }", 2)]
        // מתודות
        [InlineData(CodeConstruct.Method, "class C { void M() { } int N() => 1; }", 2)]
        [InlineData(CodeConstruct.Recursion, "class C { int F(int n) => n <= 1 ? 1 : F(n - 1); }", 1)]
        // אוספים — int[] a = new int[5] נספר פעם אחת, לא פעמיים (מתועד: הסינון של הכפילות)
        [InlineData(CodeConstruct.Array, "class C { void M() { int[] a = new int[5]; } }", 1)]
        [InlineData(CodeConstruct.Matrix, "class C { void M() { int[,] m = new int[2, 2]; } }", 1)]
        [InlineData(CodeConstruct.List, "using System.Collections.Generic; class C { List<int> Items; }", 1)]
        [InlineData(CodeConstruct.Dictionary, "using System.Collections.Generic; class C { Dictionary<int, string> Map; }", 1)]
        // סוגי משתנים — bool a, b הוא שני משתנים (מתועד: נספרים המשתנים, לא ההצהרות)
        [InlineData(CodeConstruct.BoolVariable, "class C { void M() { bool a, b; a = b = true; } }", 2)]
        [InlineData(CodeConstruct.BoolVariable, "class C { void M() { bool? maybe = null; } }", 1)]
        [InlineData(CodeConstruct.StringVariable, "class C { void M() { string s = \"x\"; } }", 1)]
        [InlineData(CodeConstruct.StringVariable, "class C { void M() { String s = \"x\"; } }", 1)]
        [InlineData(CodeConstruct.CharVariable, "class C { void M() { char c = 'a'; } }", 1)]
        [InlineData(CodeConstruct.LocalVariable, "class C { void M() { int x = 1; int y = 2; } }", 2)]
        [InlineData(CodeConstruct.Constant, "class C { void M() { const int X = 5; } }", 1)]
        // מונחה עצמים
        [InlineData(CodeConstruct.Class, "class A { } class B { }", 2)]
        [InlineData(CodeConstruct.Property, "class C { int X { get; set; } }", 1)]
        [InlineData(CodeConstruct.Constructor, "class C { public C() { } }", 1)]
        [InlineData(CodeConstruct.Field, "class C { int x; int y; }", 2)]
        [InlineData(CodeConstruct.Inheritance, "class A { } class B : A { }", 1)]
        [InlineData(CodeConstruct.Interface, "interface I { }", 1)]
        // מתקדם — LINQ הוא תחביר שאילתה או using System.Linq
        [InlineData(CodeConstruct.TryCatch, "class C { void M() { try { } catch { } } }", 1)]
        [InlineData(CodeConstruct.Linq, "class C { void M(int[] a) { var q = from x in a select x; } }", 1)]
        [InlineData(CodeConstruct.Linq, "using System.Linq; class C { }", 1)]
        // בקרת זרימה
        [InlineData(CodeConstruct.Break, "class C { void M() { while (true) { break; } } }", 1)]
        [InlineData(CodeConstruct.Continue, "class C { void M() { while (true) { continue; } } }", 1)]
        [InlineData(CodeConstruct.Goto, "class C { void M() { goto L; L: ; } }", 1)]
        public void Construct_IsCounted_InMatchingCode(CodeConstruct construct, string source, int expected)
        {
            Check(construct, source).ActualCount.Should().Be(expected);
        }

        // ── התחביר שכמעט תואם חייב לא להיספר — זו הבדיקה ששווה הכי הרבה ──

        [Theory]
        // תנאי אחד אינו תנאי אחר
        [InlineData(CodeConstruct.If, "class C { int M(int x) => x > 0 ? 1 : 2; }")]
        [InlineData(CodeConstruct.Switch, "class C { void M(int x) { if (x > 0) { } } }")]
        [InlineData(CodeConstruct.Ternary, "class C { void M(int x) { if (x > 0) { } } }")]
        // לולאה אחת אינה לולאה אחרת
        [InlineData(CodeConstruct.For, "class C { void M() { while (true) { } } }")]
        [InlineData(CodeConstruct.While, "class C { void M() { int i = 0; do { i++; } while (i < 3); } }")]
        [InlineData(CodeConstruct.DoWhile, "class C { void M() { while (true) { } } }")]
        [InlineData(CodeConstruct.Foreach, "class C { void M() { for (int i = 0; i < 3; i++) { } } }")]
        // 🔴 מלכודת 3: מטריצה ≠ מערך, לשני הכיוונים — וגם מערך משורג אינו מטריצה
        [InlineData(CodeConstruct.Array, "class C { void M() { int[,] m = new int[2, 2]; } }")]
        [InlineData(CodeConstruct.Matrix, "class C { void M() { int[] a = new int[5]; } }")]
        [InlineData(CodeConstruct.Matrix, "class C { void M() { int[][] j = new int[2][]; } }")]
        // אוסף בשם דומה אינו האוסף הנדרש
        [InlineData(CodeConstruct.List, "class MyList<T> { } class C { MyList<int> Items; }")]
        [InlineData(CodeConstruct.Dictionary, "using System.Collections.Generic; class C { List<int> Items; }")]
        // 🔴 מלכודת 4: var isSorted = true אינו משתנה בוליאני — אין מודל סמנטי
        [InlineData(CodeConstruct.BoolVariable, "class C { void M() { var isSorted = true; } }")]
        [InlineData(CodeConstruct.StringVariable, "class C { void M() { var s = \"x\"; } }")]
        [InlineData(CodeConstruct.CharVariable, "class C { void M() { string s = \"x\"; } }")]
        [InlineData(CodeConstruct.Constant, "class C { void M() { int x = 5; } }")]
        // מונחה עצמים: שדה אינו property, משתנה מקומי אינו שדה, מתודה אינה בנאי
        [InlineData(CodeConstruct.Class, "interface I { }")]
        [InlineData(CodeConstruct.Property, "class C { int x; }")]
        [InlineData(CodeConstruct.Field, "class C { void M() { int x = 5; } }")]
        [InlineData(CodeConstruct.Constructor, "class C { void M() { } }")]
        [InlineData(CodeConstruct.Inheritance, "class A { }")]
        [InlineData(CodeConstruct.Interface, "class C { }")]
        [InlineData(CodeConstruct.Method, "class C { int X { get; set; } }")]
        // 🔴 מלכודת 4: שרשור מתודות אינו נספר כ-LINQ — נראה כקריאת מתודה רגילה
        [InlineData(CodeConstruct.Linq, "class C { void M(int[] a) { var q = a.Where(x => x > 0).Select(x => x); } }")]
        public void Construct_IsNotCounted_InNearMissCode(CodeConstruct construct, string source)
        {
            Check(construct, source).ActualCount.Should().Be(0);
        }

        // ── 🔴 מלכודת 1: רקורסיה משווה מזהים שלמים, לא תת-מחרוזות ──

        // Sum שקוראת ל-SumDigits אינה רקורסיה — "Sum" מוכל ב-"SumDigits" והשוואת תת-מחרוזת הייתה מנקדת על כלום
        [Fact]
        public void Recursion_IsNotFound_WhenMethodCallsSimilarlyNamedHelper()
        {
            var source = "class C { int Sum(int n) { return SumDigits(n); } int SumDigits(int n) { return n; } }";

            Check(CodeConstruct.Recursion, source).ActualCount.Should().Be(0);
        }

        // מסלול Method: מתודה חשופה בלי מחלקה נפרשת כפונקציה מקומית — ורקורסיה חייבת להימצא גם שם
        [Fact]
        public void Recursion_IsFound_InBareMethodSubmission()
        {
            var source = "int F(int n) { if (n <= 1) return 1; return F(n - 1); }";

            Check(CodeConstruct.Recursion, source).ActualCount.Should().Be(1);
        }

        // ── 🔴 מלכודת 2: שגיאת תחביר אסור שתפעיל את השער החוסם ──

        // קוד עם נקודה-פסיק חסרה → HasSyntaxErrors, לא "אין רקורסיה"
        [Theory]
        [InlineData("class C { void M() { int x =  } }")]
        [InlineData("@@@ this is not C# at all !!!")]
        public void Analyze_FlagsSyntaxErrors_InsteadOfCountingZero(string brokenSource)
        {
            var result = Analyzer.Analyze(brokenSource, new[] { MustUse(CodeConstruct.Recursion, RuleSeverity.Blocking) });

            result.HasSyntaxErrors.Should().BeTrue();
        }

        // מקור ריק או null → "כשל ניתוח", לא ספירת אפס כנה
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Analyze_FlagsEmptySource_AsSyntaxError(string? source)
        {
            var result = Analyzer.Analyze(source, new[] { MustUse(CodeConstruct.If) });

            result.HasSyntaxErrors.Should().BeTrue();
            result.Results.Should().HaveCount(1);
        }

        // בלי דרישות אין מה לנתח — תוצאה ריקה בלי דגל שגיאה
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Analyze_ReturnsEmpty_WhenNoRules(bool nullRules)
        {
            var rules = nullRules ? null : Array.Empty<StructuralRule>();

            var result = Analyzer.Analyze("class C { }", rules);

            result.Results.Should().BeEmpty();
            result.HasSyntaxErrors.Should().BeFalse();
        }

        // ── עומק קינון: ערך של עומק, לא ספירה ──

        // שלוש לולאות אחיות → עומק 1, לא 3
        [Fact]
        public void NestedLoopDepth_IsOne_ForSiblingLoops()
        {
            var source = "class C { void M() { for (int i = 0; i < 3; i++) { } for (int j = 0; j < 3; j++) { } while (true) { } } }";

            Check(CodeConstruct.NestedLoopDepth, source).ActualCount.Should().Be(1);
        }

        // שתי לולאות מקוננות → עומק 2, והמשוב מצביע על הלולאה הפנימית
        [Fact]
        public void NestedLoopDepth_IsTwo_ForNestedLoops()
        {
            var source = "class C { void M() {\nfor (int i = 0; i < 3; i++) {\nwhile (true) { }\n} } }";

            var result = Check(CodeConstruct.NestedLoopDepth, source);

            result.ActualCount.Should().Be(2);
            result.Lines.Should().Equal(3);
        }

        // ── מספרי שורות: 1-based, ולכל היותר 10 ──

        // "בשורה 3" — לפי בני אדם, לא לפי Roslyn שמתחיל מאפס
        [Fact]
        public void Lines_AreOneBased()
        {
            var source = "class C {\nvoid M(int x) {\nif (x > 0) { }\n} }";

            Check(CodeConstruct.If, source).Lines.Should().Equal(3);
        }

        // המשוב אומר "בשורה 12", לא רשימה של ארבעים — לכל היותר 10 שורות
        [Fact]
        public void Lines_AreCappedAtTen()
        {
            var source = "class C { void M(int x) {\n" +
                "if (x > 0) { }\nif (x > 0) { }\nif (x > 0) { }\nif (x > 0) { }\n" +
                "if (x > 0) { }\nif (x > 0) { }\nif (x > 0) { }\nif (x > 0) { }\n" +
                "if (x > 0) { }\nif (x > 0) { }\nif (x > 0) { }\nif (x > 0) { }\n" +
                "} }";

            var result = Check(CodeConstruct.If, source);

            result.ActualCount.Should().Be(12);
            result.Lines.Should().HaveCount(10);
        }

        // ── FailedBlockingRules: רק חוסמות שנכשלו ──

        // דרישה חוסמת שנכשלה נאספת; מנוקדת שנכשלה וחוסמת שעברה — לא
        [Fact]
        public void FailedBlockingRules_ListsOnlyFailedBlockingRules()
        {
            var rules = new[]
            {
                MustUse(CodeConstruct.Recursion, RuleSeverity.Blocking),  // תיכשל — אין רקורסיה
                MustUse(CodeConstruct.If, RuleSeverity.Blocking),         // תעבור — יש if
                MustUse(CodeConstruct.For, RuleSeverity.Scored)           // תיכשל, אבל אינה חוסמת
            };

            var result = Analyzer.Analyze("class C { void M(int x) { if (x > 0) { } } }", rules);

            result.FailedBlockingRules.Should().HaveCount(1);
            result.FailedBlockingRules[0].Rule.Construct.Should().Be(CodeConstruct.Recursion);
        }

        // ── characterization: מגבלות מתועדות של ניתוח בלי מודל סמנטי ──

        // characterization — מתעד התנהגות קיימת, לא מאשר אותה: מימוש ממשק נראה בתחביר
        // כמו ירושה (שניהם BaseList), ולכן class A : IComparable מקיים "חובה ירושה"
        [Fact]
        public void Inheritance_IsSatisfiedByInterfaceImplementation()
        {
            Check(CodeConstruct.Inheritance, "class A : IComparable { }").ActualCount.Should().Be(1);
        }
    }
}
