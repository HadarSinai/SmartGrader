using SmartGrader.Domain.Entities;

namespace SmartGrader.Application.Services.CodeAnalysis;

/// <summary>
/// ניסוח דרישה מבנית ותוצאתה כטקסט קריא.
/// <para>
/// שני צרכנים, ובכוונה אותו מקור: הפרומפט שנשלח למודל (כדי שיֵדע על מה הוא מסביר) והמשוב
/// הדטרמיניסטי שמוצג לתלמידה כשהמודל אינו זמין. אילו כל אחד מהם היה מנסח לעצמו, הכשל
/// ב-OpenAI היה משנה לא רק את איכות ההסבר אלא גם את מה שנטען שנבדק.
/// </para>
/// <para>
/// ⚠️ העברית כאן פונה לתלמידה <b>בלשון נקבה</b> — בית ספר לבנות. זו דרישה קשיחה ולא העדפה.
/// </para>
/// </summary>
public static class StructuralRuleDescriber
{
    /// <summary>
    /// שם המבנה בעברית. מבנים ששמם בקוד הוא מילת מפתח (<c>if</c>, <c>while</c>) נשארים
    /// באנגלית — כך התלמידה כותבת אותם בפועל, ותרגום היה מרחיק את ההסבר מהקוד.
    /// </summary>
    public static string ConstructName(CodeConstruct construct) => construct switch
    {
        CodeConstruct.If => "if",
        CodeConstruct.Switch => "switch",
        CodeConstruct.Ternary => "אופרטור תנאי (?:)",

        CodeConstruct.For => "לולאת for",
        CodeConstruct.While => "לולאת while",
        CodeConstruct.DoWhile => "לולאת do-while",
        CodeConstruct.Foreach => "לולאת foreach",
        CodeConstruct.AnyLoop => "לולאה",

        CodeConstruct.Method => "מתודה",
        CodeConstruct.Recursion => "רקורסיה",

        CodeConstruct.Array => "מערך חד-ממדי",
        CodeConstruct.Matrix => "מערך דו-ממדי",
        CodeConstruct.List => "List",
        CodeConstruct.Dictionary => "Dictionary",

        CodeConstruct.BoolVariable => "משתנה בוליאני",
        CodeConstruct.StringVariable => "משתנה מחרוזת",
        CodeConstruct.CharVariable => "משתנה תו",
        CodeConstruct.LocalVariable => "משתנה מקומי",
        CodeConstruct.Constant => "קבוע (const)",

        CodeConstruct.Class => "מחלקה",
        CodeConstruct.Property => "תכונה (property)",
        CodeConstruct.Constructor => "בנאי",
        CodeConstruct.Field => "שדה",
        CodeConstruct.Inheritance => "ירושה",
        CodeConstruct.Interface => "ממשק",

        CodeConstruct.TryCatch => "try-catch",
        CodeConstruct.Linq => "LINQ",

        CodeConstruct.Break => "break",
        CodeConstruct.Continue => "continue",
        CodeConstruct.Goto => "goto",

        CodeConstruct.NestedLoopDepth => "עומק קינון לולאות",

        _ => construct.ToString()
    };

    /// <summary>
    /// המין הדקדוקי של שם המבנה, כדי ש"לא נמצא/ה" יתאים לו.
    /// <para>
    /// ⚠️ לא קישוט: הטקסט הזה מוצג לתלמידה כמו שהוא, ו"לא נמצא רקורסיה" הוא שגיאה שקופצת
    /// לעין בעברית. מילים לועזיות (<c>if</c>, <c>switch</c>, <c>LINQ</c>) נחשבות זכר.
    /// </para>
    /// </summary>
    private static bool IsFeminine(CodeConstruct construct) => construct switch
    {
        CodeConstruct.For or CodeConstruct.While or CodeConstruct.DoWhile
            or CodeConstruct.Foreach or CodeConstruct.AnyLoop => true,   // לולאה
        CodeConstruct.Method => true,                                     // מתודה
        CodeConstruct.Recursion => true,                                  // רקורסיה
        CodeConstruct.Class => true,                                      // מחלקה
        CodeConstruct.Property => true,                                   // תכונה
        CodeConstruct.Inheritance => true,                                // ירושה
        _ => false
    };

    /// <summary>
    /// מוסיף מקף בין אות יחס למילה לועזית: "ב-if" ולא "בif".
    /// </summary>
    private static string WithPrefix(string prefix, string name) =>
        name.Length > 0 && char.IsAscii(name[0]) && char.IsLetter(name[0])
            ? $"{prefix}-{name}"
            : $"{prefix}{name}";

    /// <summary>הדרישה עצמה — "חובה להשתמש ברקורסיה", "לכל היותר 3 if".</summary>
    public static string Describe(StructuralRule rule)
    {
        var name = ConstructName(rule.Construct);

        // עומק קינון אינו ספירה של מופעים, ולכן "לכל היותר 2 עומק קינון לולאות" לא היה נקרא.
        if (rule.Construct == CodeConstruct.NestedLoopDepth)
            return rule.Kind switch
            {
                RuleKind.AtMost => $"{name} לא יעלה על {rule.Threshold}",
                RuleKind.AtLeast => $"{name} יהיה לפחות {rule.Threshold}",
                RuleKind.MustNotUse => "אסור לקנן לולאות",
                _ => $"{name} לפחות 1"
            };

        return rule.Kind switch
        {
            RuleKind.MustUse => $"חובה להשתמש {WithPrefix("ב", name)}",
            RuleKind.MustNotUse => $"אסור להשתמש {WithPrefix("ב", name)}",
            RuleKind.AtLeast => $"לפחות {rule.Threshold} {name}",
            RuleKind.AtMost => $"לכל היותר {rule.Threshold} {name}",
            _ => name
        };
    }

    /// <summary>מה נמצא בפועל — "נמצאו 4 מופעים של if (בשורות 3, 7)", "לא נמצאה רקורסיה בקוד".</summary>
    public static string DescribeFinding(StructuralRuleResult result)
    {
        var name = ConstructName(result.Rule.Construct);

        if (result.Rule.Construct == CodeConstruct.NestedLoopDepth)
            return result.ActualCount == 0
                ? "לא נמצאו לולאות בקוד"
                : $"עומק הקינון בפועל: {result.ActualCount}{Lines(result)}";

        if (result.ActualCount == 0)
            return $"לא {(IsFeminine(result.Rule.Construct) ? "נמצאה" : "נמצא")} {name} בקוד";

        // "נמצאו 1 מופעים" אינו עברית.
        return result.ActualCount == 1
            ? $"נמצא מופע אחד של {name}{Lines(result)}"
            : $"נמצאו {result.ActualCount} מופעים של {name}{Lines(result)}";
    }

    /// <summary>שורה אחת שמסכמת דרישה שלא התקיימה, כפי שהיא מוצגת לתלמידה.</summary>
    public static string DescribeFailure(StructuralRuleResult result) =>
        $"❌ הדרישה \"{Describe(result.Rule)}\" לא התקיימה — {DescribeFinding(result)}";

    private static string Lines(StructuralRuleResult result) =>
        result.Lines.Count switch
        {
            0 => "",
            1 => $" (בשורה {result.Lines[0]})",
            _ => $" (בשורות {string.Join(", ", result.Lines)})"
        };
}
