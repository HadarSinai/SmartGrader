using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace SmartGrader.UnitTests.Docs
{
    /// <summary>
    /// כל טוקן ש-<c>docs/design-system.md</c> מבטיח קיים באמת ב-<c>client/src/styles.css</c>,
    /// ומספר הקבצים שממציאים צבע משלהם אינו גדל.
    /// <para>
    /// ⚠️ פרסור טקסט על CSS, מאותה סיבה כמו טבלת הנתיבים: אין ללקוח פרויקט טסטים, ולכן
    /// טסט בשרת שקורא את הקובץ הוא השומר היחיד שיש.
    /// </para>
    /// </summary>
    public class DesignTokenTests
    {
        private const string Document = "design-system.md";

        /// <summary>
        /// אפס. A6 המיר את כל 14 הקבצים לטוקנים; מכאן והלאה כל צבע קשיח חדש מפיל.
        /// <b>לעולם לא להעלות את המספר הזה.</b>
        /// </summary>
        private const int HardcodedColourFileRatchet = 0;

        private static readonly Regex CssVariable =
            new(@"^\s*(?<token>--[a-z0-9-]+)\s*:", RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// ⚠️ לא רק hex. הגרסה הראשונה של הטסט חיפשה <c>#rrggbb</c> בלבד, ולכן שמונה קבצים
        /// שקבעו צבע כ-<c>rgba(...)</c> עברו מתחתיה בזמן ש-A6 הכריז על אפס — ביניהם שכבת
        /// ה-hero, שציירה קרם בהיר על גבי הרקע הכהה. צורת הכתיבה אינה העניין; קביעת צבע היא.
        /// </summary>
        private static readonly Regex LiteralColour =
            new(@"#[0-9a-fA-F]{3,8}\b|\b(?:rgba?|hsla?)\s*\(", RegexOptions.Compiled);

        private static string StylesPath =>
            Path.Combine(RepoRoot.Path, "client", "src", "styles.css");

        // כל טוקן שהמסמך נוקב בו מוגדר בגיליון הסגנונות
        [Fact]
        public void EveryDocumentedToken_ExistsInTheStylesheet()
        {
            var blocks = GenBlock.FindAll(RepoRoot.ReadDoc(Document), "tokens");
            blocks.Should().HaveCount(1, $"{Document} מחזיק בלוק tokens אחד");

            var documented = blocks[0].Rows.Select(r => r[0]).ToList();

            documented.Should().HaveCountGreaterThan(20,
                "אם נמצאו כמעט אפס טוקנים, הפרסור נשבר ולא המסמך");
            documented.Should().OnlyHaveUniqueItems();

            var defined = CssVariable
                .Matches(File.ReadAllText(StylesPath))
                .Select(m => m.Groups["token"].Value)
                .ToHashSet(StringComparer.Ordinal);

            defined.Should().HaveCountGreaterThan(20,
                "אם כמעט לא נמצאו הגדרות ב-styles.css, הרג'קס נשבר");

            var missing = documented.Where(t => !defined.Contains(t)).ToList();

            missing.Should().BeEmpty(
                $"{Document} מבטיח טוקנים שאינם מוגדרים ב-client/src/styles.css");
        }

        /// <summary>
        /// ⚠️ סופר <b>קבצים</b> ולא מופעים: קובץ אחד עם עשרים צבעים קשיחים הוא בעיה אחת
        /// לניקוי, וספירת מופעים הייתה הופכת כל refactor פנימי לשינוי במספר.
        /// </summary>
        [Fact]
        public void HardcodedColours_DoNotGrow()
        {
            var appDir = Path.Combine(RepoRoot.Path, "client", "src", "app");

            var offenders = Directory
                .EnumerateFiles(appDir, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
                .Where(f => LiteralColour.IsMatch(File.ReadAllText(f)))
                .Select(f => Path.GetRelativePath(RepoRoot.Path, f))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToList();

            offenders.Should().HaveCountLessThanOrEqualTo(HardcodedColourFileRatchet,
                "קובץ שממציא צבע משלו יצא ממערכת העיצוב. אם ניקית קובץ — להוריד את הרַצֶ'ט; " +
                $"כרגע {string.Join(", ", offenders)}");
        }

        /// <summary>
        /// כלל שהוגדר גלובלית ואז הוגדר שוב בתוך רכיב הוא שני מקורות אמת לאותה הצגה,
        /// ומי שיתקן את אחד מהם לא יידע על השני. זה בדיוק מה שקרה ל-<c>.sg-auth-*</c>
        /// (שלושה עותקים), ל-<c>.sg-account-*</c> (שלושה) ול-<c>.sg-header</c> (שניים).
        /// </summary>
        [Fact]
        public void NoComponent_RedefinesASelectorTheGlobalSheetAlreadyDefines()
        {
            var global = BaseClassSelectors(File.ReadAllText(StylesPath));

            global.Should().HaveCountGreaterThan(20,
                "אם כמעט לא נמצאו סלקטורים ב-styles.css, הרג'קס נשבר ולא הקוד");

            var appDir = Path.Combine(RepoRoot.Path, "client", "src", "app");

            var clashes = Directory
                .EnumerateFiles(appDir, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
                .SelectMany(f => BaseClassSelectors(File.ReadAllText(f))
                    .Where(global.Contains)
                    .Select(s => $"{Path.GetRelativePath(RepoRoot.Path, f)}: {s}"))
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();

            clashes.Should().BeEmpty(
                "כלל משותף מוגדר פעם אחת ב-client/src/styles.css. אם הרכיב צריך לשנות " +
                "התנהגות — שם מחלקה נוסף (מודיפייר), לא הגדרה מחדש של אותו שם");
        }

        /// <summary>
        /// רק סלקטור מחלקה יחיד שפותח כלל — <c>.foo {</c> או <c>.foo,</c>. צירופים
        /// (<c>.foo .bar</c>, <c>.foo.p-highlight</c>) אינם הגדרה מחדש של הבסיס ולכן אינם נספרים.
        /// </summary>
        private static HashSet<string> BaseClassSelectors(string css) =>
            Regex.Matches(css, @"^[ \t]*(?<sel>\.[a-zA-Z0-9_-]+)[ \t]*(\{|,[ \t]*$)",
                    RegexOptions.Multiline)
                .Select(m => m.Groups["sel"].Value)
                .ToHashSet(StringComparer.Ordinal);

        // המסמך מצהיר על הרצ'ט, והטסט אוכף אותו — שני מספרים שונים הם באג בהמתנה
        [Fact]
        public void TheDocument_StatesTheSameRatchetTheTestEnforces()
        {
            RepoRoot.ReadDoc(Document).Should().Contain(
                $"currently {HardcodedColourFileRatchet} files",
                $"{Document} חייב לנקוב באותו מספר שהטסט אוכף");
        }
    }
}
