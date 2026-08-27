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
        /// הספירה של היום, לא יעד. A6 מוריד אותה לאפס; עד אז היא נעולה כדי שלא תגדל.
        /// <b>להוריד את המספר הזה כשקובץ מנוקה — לעולם לא להעלות אותו.</b>
        /// </summary>
        private const int HardcodedColourFileRatchet = 14;

        private static readonly Regex CssVariable =
            new(@"^\s*(?<token>--[a-z0-9-]+)\s*:", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex HexColour =
            new(@"#[0-9a-fA-F]{3,8}\b", RegexOptions.Compiled);

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
                .Where(f => HexColour.IsMatch(File.ReadAllText(f)))
                .Select(f => Path.GetRelativePath(RepoRoot.Path, f))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToList();

            offenders.Should().HaveCountLessThanOrEqualTo(HardcodedColourFileRatchet,
                "קובץ שממציא צבע משלו יצא ממערכת העיצוב. אם ניקית קובץ — להוריד את הרַצֶ'ט; " +
                $"כרגע {string.Join(", ", offenders)}");
        }

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
