using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SmartGrader.Application.Services.CodeAnalysis;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Infrastructure.Services.CodeAnalysis;

/// <summary>
/// בודק דרישות מבניות בעזרת מנתח התחביר של C# (Roslyn).
/// <para>
/// <b>הוספת מבנה לקטלוג = ערך אחד ב-<see cref="CodeConstruct"/> + <c>case</c> אחד
/// ב-<see cref="Measure"/>.</b> מלמדים מבנה אחר כל שבוע, והפתיחות הזו היא הדרישה עצמה.
/// </para>
/// <para>
/// ⚠️ ניתוח תחבירי בלבד, בלי מודל סמנטי — אין כאן הידור ואין רזולוציה של טיפוסים. המשמעות
/// המעשית מצוינת בהערה של כל <c>case</c> שבו יש לה מחיר.
/// </para>
/// </summary>
public class RoslynCodeAnalysisService : ICodeAnalysisService
{
    /// <summary>כמה מספרי שורה לכל היותר נשמרים לתוצאה. המשוב אומר "בשורה 12", לא רשימה של 40.</summary>
    private const int MaxReportedLines = 10;

    public CodeAnalysisResult Analyze(string? sourceCode, IReadOnlyList<StructuralRule>? rules)
    {
        if (rules is null || rules.Count == 0)
            return CodeAnalysisResult.Empty;

        if (string.IsNullOrWhiteSpace(sourceCode))
            return NothingFound(rules);

        try
        {
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetRoot();

            // אבחנות תחביר בלבד (אין Compilation, ולכן אין כאן שגיאות סמנטיות).
            // ⚠️ הדגל הזה הוא מה שמונע "התרגיל דרש רקורסיה" על קוד שחסרה בו נקודה-פסיק —
            // ר' CodeAnalysisResult.HasSyntaxErrors.
            var hasSyntaxErrors = tree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);

            var results = rules.Select(rule => Evaluate(root, rule)).ToList();
            return new CodeAnalysisResult(results, hasSyntaxErrors);
        }
        catch (Exception)
        {
            // Roslyn משחזר כמעט מכל קלט, אבל כשל כאן לא ייקח איתו את בדיקת ההגשה:
            // ההגשה תמשיך ל-Judge0 ותקבל את השגיאה האמיתית.
            return NothingFound(rules);
        }
    }

    /// <summary>לא נמצא דבר, ומסומן ככשל ניתוח כדי שהשער החוסם לא יופעל על סמך אפס מזויף.</summary>
    private static CodeAnalysisResult NothingFound(IReadOnlyList<StructuralRule> rules) =>
        new(
            rules.Select(rule => new StructuralRuleResult
            {
                Rule = rule,
                ActualCount = 0,
                Passed = rule.IsSatisfiedBy(0)
            }).ToList(),
            HasSyntaxErrors: true);

    private static StructuralRuleResult Evaluate(SyntaxNode root, StructuralRule rule)
    {
        var (count, nodes) = Measure(root, rule.Construct);

        return new StructuralRuleResult
        {
            Rule = rule,
            ActualCount = count,
            // ההכרעה עצמה יושבת ב-StructuralRule.IsSatisfiedBy ולא כאן — פרשנות שנייה
            // לאותו כלל היא בדיוק מה שגורם לציון להשתנות בין מסלולים.
            Passed = rule.IsSatisfiedBy(count),
            Lines = nodes
                .Select(LineOf)
                .Distinct()
                .OrderBy(line => line)
                .Take(MaxReportedLines)
                .ToList()
        };
    }

    // ── הקטלוג ───────────────────────────────────────────────────────────────

    private static (int Count, IReadOnlyList<SyntaxNode> Nodes) Measure(
        SyntaxNode root,
        CodeConstruct construct)
    {
        switch (construct)
        {
            // ── תנאים ──
            // ⚠️ else if הוא IfStatementSyntax מקונן, ולכן if/else-if/else נספר כשניים.
            // זו הספירה הנכונה: "לכל היותר 3 if" מתייחס גם לענפי else-if.
            case CodeConstruct.If:
                return Found(root.DescendantNodes().OfType<IfStatementSyntax>());

            case CodeConstruct.Switch:
                return Found(root.DescendantNodes()
                    .Where(n => n is SwitchStatementSyntax or SwitchExpressionSyntax));

            case CodeConstruct.Ternary:
                return Found(root.DescendantNodes().OfType<ConditionalExpressionSyntax>());

            // ── לולאות ──
            case CodeConstruct.For:
                return Found(root.DescendantNodes().OfType<ForStatementSyntax>());

            case CodeConstruct.While:
                return Found(root.DescendantNodes().OfType<WhileStatementSyntax>());

            case CodeConstruct.DoWhile:
                return Found(root.DescendantNodes().OfType<DoStatementSyntax>());

            // CommonForEachStatementSyntax ולא ForEachStatementSyntax: foreach על טאפל
            // מפורק (ForEachVariableStatementSyntax) הוא foreach לכל דבר עבור התלמידה.
            case CodeConstruct.Foreach:
                return Found(root.DescendantNodes().OfType<CommonForEachStatementSyntax>());

            case CodeConstruct.AnyLoop:
                return Found(root.DescendantNodes().Where(IsLoop));

            // ── מתודות ──
            // מסלול Method מגיש מתודות בלי מחלקה עוטפת, ו-Roslyn מפרש אותן כפונקציות
            // מקומיות בתוך top-level statements. בלי LocalFunctionStatementSyntax כאן
            // "חובה מתודה" ו"חובה רקורסיה" היו נכשלים על כל הגשה במסלול הזה.
            case CodeConstruct.Method:
                return Found(root.DescendantNodes().Where(IsMethodLike));

            case CodeConstruct.Recursion:
                return Found(FindRecursiveMethods(root));

            // ── אוספים ──
            case CodeConstruct.Array:
                return Found(ArrayTypes(root).Where(t => !IsMatrix(t)));

            // ⚠️ מטריצה אינה מערך. int[] לא ייספר כאן, וזו כל הנקודה.
            case CodeConstruct.Matrix:
                return Found(ArrayTypes(root).Where(IsMatrix));

            case CodeConstruct.List:
                return Found(GenericTypesNamed(root, "List"));

            case CodeConstruct.Dictionary:
                return Found(GenericTypesNamed(root, "Dictionary"));

            // ── סוגי משתנים ──
            // נספרים המשתנים עצמם ולא ההצהרות: bool a, b; הוא שני משתנים בוליאניים.
            // ⚠️ var isSorted = true; אינו נספר — בלי מודל סמנטי אין דרך לדעת את הטיפוס,
            // והתרגיל שמבקש משתנה בוליאני מבקש אותו במפורש ממילא.
            case CodeConstruct.BoolVariable:
                return Found(DeclaratorsOfType(root, "bool", "Boolean"));

            case CodeConstruct.StringVariable:
                return Found(DeclaratorsOfType(root, "string", "String"));

            case CodeConstruct.CharVariable:
                return Found(DeclaratorsOfType(root, "char", "Char"));

            case CodeConstruct.LocalVariable:
                return Found(root.DescendantNodes()
                    .OfType<LocalDeclarationStatementSyntax>()
                    .SelectMany(d => d.Declaration.Variables));

            case CodeConstruct.Constant:
                return Found(root.DescendantNodes()
                    .Where(n => n is LocalDeclarationStatementSyntax or FieldDeclarationSyntax)
                    .Where(HasConstModifier));

            // ── מונחה עצמים ──
            case CodeConstruct.Class:
                return Found(root.DescendantNodes().OfType<ClassDeclarationSyntax>());

            case CodeConstruct.Property:
                return Found(root.DescendantNodes().OfType<PropertyDeclarationSyntax>());

            case CodeConstruct.Constructor:
                return Found(root.DescendantNodes().OfType<ConstructorDeclarationSyntax>());

            case CodeConstruct.Field:
                return Found(root.DescendantNodes()
                    .OfType<FieldDeclarationSyntax>()
                    .SelectMany(f => f.Declaration.Variables));

            // ⚠️ מימוש ממשק נראה בתחביר בדיוק כמו ירושה — שניהם BaseList. בלי מודל סמנטי
            // אי אפשר להפריד ביניהם, ו-"חובה ירושה" יתקיים גם על class A : IComparable.
            case CodeConstruct.Inheritance:
                return Found(root.DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .Where(t => t.BaseList is { Types.Count: > 0 }));

            case CodeConstruct.Interface:
                return Found(root.DescendantNodes().OfType<InterfaceDeclarationSyntax>());

            // ── מתקדם ──
            case CodeConstruct.TryCatch:
                return Found(root.DescendantNodes().OfType<TryStatementSyntax>());

            // תחביר השאילתה (from … select) ו-using System.Linq. שרשור מתודות
            // (.Where(...).Select(...)) אינו מזוהה כאן — הוא נראה כקריאת מתודה רגילה.
            case CodeConstruct.Linq:
                return Found(root.DescendantNodes()
                    .Where(n => n is QueryExpressionSyntax
                        || (n is UsingDirectiveSyntax u && u.Name?.ToString() == "System.Linq")));

            // ── בקרת זרימה ──
            case CodeConstruct.Break:
                return Found(root.DescendantNodes().OfType<BreakStatementSyntax>());

            case CodeConstruct.Continue:
                return Found(root.DescendantNodes().OfType<ContinueStatementSyntax>());

            case CodeConstruct.Goto:
                return Found(root.DescendantNodes().OfType<GotoStatementSyntax>());

            // ── יעילות ──
            // ⚠️ הערך אינו ספירה אלא עומק. "לכל היותר קינון 2" מתקיים על שלוש לולאות
            // אחיות ונכשל על שתיים מקוננות.
            case CodeConstruct.NestedLoopDepth:
                return MeasureLoopDepth(root);

            default:
                return (0, Array.Empty<SyntaxNode>());
        }
    }

    // ── עזרי זיהוי ────────────────────────────────────────────────────────────

    private static (int Count, IReadOnlyList<SyntaxNode> Nodes) Found(IEnumerable<SyntaxNode> nodes)
    {
        var list = nodes.ToList();
        return (list.Count, list);
    }

    private static bool IsLoop(SyntaxNode node) =>
        node is ForStatementSyntax
            or WhileStatementSyntax
            or DoStatementSyntax
            or CommonForEachStatementSyntax;

    private static bool IsMethodLike(SyntaxNode node) =>
        node is MethodDeclarationSyntax or LocalFunctionStatementSyntax;

    private static int LineOf(SyntaxNode node) =>
        node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    private static bool HasConstModifier(SyntaxNode node) => node switch
    {
        LocalDeclarationStatementSyntax local => local.Modifiers.Any(SyntaxKind.ConstKeyword),
        FieldDeclarationSyntax field => field.Modifiers.Any(SyntaxKind.ConstKeyword),
        _ => false
    };

    private static IEnumerable<SyntaxNode> GenericTypesNamed(SyntaxNode root, string name) =>
        root.DescendantNodes()
            .OfType<GenericNameSyntax>()
            .Where(g => g.Identifier.Text == name);

    /// <summary>
    /// טיפוסי המערך בקוד, בלי הכפילות של <c>int[] a = new int[5];</c> — שם אותו מערך אחד
    /// מופיע גם בהצהרה וגם ביצירה, והיה נספר פעמיים מול "לכל היותר מערך אחד".
    /// </summary>
    private static IEnumerable<ArrayTypeSyntax> ArrayTypes(SyntaxNode root) =>
        root.DescendantNodes()
            .OfType<ArrayTypeSyntax>()
            .Where(type => !IsRedundantCreationType(type));

    private static bool IsRedundantCreationType(ArrayTypeSyntax type) =>
        type.Parent is ArrayCreationExpressionSyntax creation
        && creation.Parent is EqualsValueClauseSyntax equals
        && equals.Parent is VariableDeclaratorSyntax declarator
        && declarator.Parent is VariableDeclarationSyntax declaration
        && declaration.Type is ArrayTypeSyntax;

    /// <summary>
    /// מערך דו-ממדי ומעלה. <c>int[,]</c> הוא מציין דרגה אחד בדרגה 2; <c>int[][]</c> משורג
    /// הוא שני מציינים בדרגה 1 כל אחד, ולכן אינו מטריצה.
    /// </summary>
    private static bool IsMatrix(ArrayTypeSyntax type) =>
        type.RankSpecifiers.Any(rank => rank.Rank > 1);

    private static IEnumerable<SyntaxNode> DeclaratorsOfType(
        SyntaxNode root,
        string keyword,
        string clrName) =>
        root.DescendantNodes()
            .OfType<VariableDeclarationSyntax>()
            .Where(declaration => IsNamedType(declaration.Type, keyword, clrName))
            .SelectMany(declaration => declaration.Variables);

    private static bool IsNamedType(TypeSyntax type, string keyword, string clrName)
    {
        var bare = type is NullableTypeSyntax nullable ? nullable.ElementType : type;

        return bare switch
        {
            PredefinedTypeSyntax predefined => predefined.Keyword.ValueText == keyword,
            IdentifierNameSyntax identifier => identifier.Identifier.Text == clrName,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text == clrName,
            _ => false
        };
    }

    // ── רקורסיה ───────────────────────────────────────────────────────────────

    /// <summary>
    /// מתודות שהגוף שלהן קורא לעצמן.
    /// <para>
    /// 🔴 <b>ההשוואה היא של מזהה שלם, לא של תת-מחרוזת.</b> המימוש המתבקש
    /// <c>call.ToString().Contains(method.Identifier.Text)</c> שגוי: <c>"SumDigits"</c> מכיל
    /// את <c>"Sum"</c>, ולכן מתודה שרק קוראת לעוזרת בשם אחר הייתה מדווחת כרקורסיבית —
    /// והתלמידה הייתה מקבלת ניקוד על דרישה שלא קיימה.
    /// </para>
    /// </summary>
    private static IEnumerable<SyntaxNode> FindRecursiveMethods(SyntaxNode root)
    {
        foreach (var node in root.DescendantNodes().Where(IsMethodLike))
        {
            var (name, body) = node switch
            {
                MethodDeclarationSyntax m => (m.Identifier.Text, (SyntaxNode?)m.Body ?? m.ExpressionBody),
                LocalFunctionStatementSyntax f => (f.Identifier.Text, (SyntaxNode?)f.Body ?? f.ExpressionBody),
                _ => (null!, null)
            };

            if (body is null || string.IsNullOrEmpty(name))
                continue;

            var callsItself = body.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(invocation => string.Equals(InvokedName(invocation), name, StringComparison.Ordinal));

            if (callsItself)
                yield return node;
        }
    }

    /// <summary>שם המתודה שנקראה, בלי המקבל ובלי ארגומנטי הטיפוס.</summary>
    private static string? InvokedName(InvocationExpressionSyntax invocation) =>
        invocation.Expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            GenericNameSyntax generic => generic.Identifier.Text,
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
            MemberBindingExpressionSyntax binding => binding.Name.Identifier.Text,
            _ => null
        };

    // ── עומק קינון ────────────────────────────────────────────────────────────

    /// <summary>
    /// עומק הקינון המרבי של לולאות, והלולאות שנמצאות בעומק הזה (כדי שהמשוב יצביע על
    /// הלולאה הפנימית ולא על החיצונית).
    /// </summary>
    private static (int Count, IReadOnlyList<SyntaxNode> Nodes) MeasureLoopDepth(SyntaxNode root)
    {
        var maxDepth = 0;
        var deepest = new List<SyntaxNode>();

        void Walk(SyntaxNode node, int depth)
        {
            foreach (var child in node.ChildNodes())
            {
                var childDepth = depth;

                if (IsLoop(child))
                {
                    childDepth = depth + 1;

                    if (childDepth > maxDepth)
                    {
                        maxDepth = childDepth;
                        deepest.Clear();
                    }

                    if (childDepth == maxDepth)
                        deepest.Add(child);
                }

                Walk(child, childDepth);
            }
        }

        Walk(root, 0);
        return (maxDepth, deepest);
    }
}
