using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace SmartGrader.UnitTests.Docs
{
    /// <summary>
    /// כל רכיב מחזיק את התבנית והסגנון שלו בקבצים נפרדים.
    /// <para>
    /// ⚠️ פרסור טקסט על TypeScript, מאותה סיבה כמו טבלת הנתיבים ומערכת העיצוב: אין ללקוח
    /// פרויקט טסטים, ולכן טסט בשרת שקורא את הקובץ הוא השומר היחיד שיש.
    /// </para>
    /// </summary>
    public class ComponentFileLayoutTests
    {
        private static string AppDir =>
            Path.Combine(RepoRoot.Path, "client", "src", "app");

        private static readonly Regex InlineTemplate =
            new(@"^\s*template:\s*`", RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex InlineStyles =
            new(@"^\s*styles:\s*\[", RegexOptions.Compiled | RegexOptions.Multiline);

        private static IReadOnlyList<string> ComponentFiles() => Directory
            .EnumerateFiles(AppDir, "*.ts", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains("@Component({", StringComparison.Ordinal))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();

        /// <summary>
        /// שלושים וחמישה רכיבים החזיקו תבנית של מאות שורות בתוך מחרוזת ב-‎.ts‎ — ולכן בלי
        /// שירות שפה ל-HTML, בלי עיצוב אוטומטי, ועם קובץ אחד שמערבב שלוש שפות.
        /// </summary>
        [Fact]
        public void EveryComponent_KeepsItsTemplateInAFileOfItsOwn()
        {
            var files = ComponentFiles();

            files.Should().HaveCountGreaterThan(20,
                "אם כמעט לא נמצאו רכיבים, הסריקה נשברה ולא הקוד");

            var offenders = files
                .Where(f => InlineTemplate.IsMatch(File.ReadAllText(f)))
                .Select(f => Path.GetRelativePath(RepoRoot.Path, f))
                .ToList();

            offenders.Should().BeEmpty(
                "רכיב מצהיר על templateUrl ומחזיק את ה-HTML בקובץ ‎.component.html‎ לצדו");
        }

        // אותו נימוק, ובנוסף: CSS בתוך מחרוזת אינו נבדק על ידי אף כלי
        [Fact]
        public void EveryComponent_KeepsItsStylesInAFileOfItsOwn()
        {
            var offenders = ComponentFiles()
                .Where(f => InlineStyles.IsMatch(File.ReadAllText(f)))
                .Select(f => Path.GetRelativePath(RepoRoot.Path, f))
                .ToList();

            offenders.Should().BeEmpty(
                "רכיב מצהיר על styleUrls ומחזיק את ה-CSS בקובץ ‎.component.css‎ לצדו. " +
                "רכיב בלי סגנון משלו אינו מצהיר על ‎styles: []‎ ריק — הוא פשוט לא מצהיר");
        }

        /// <summary>
        /// הצהרה שמפנה לקובץ שאינו קיים נכשלת רק בבנייה, ורק אם מישהו בונה. כאן היא נכשלת מיד.
        /// </summary>
        [Fact]
        public void EveryDeclaredTemplateAndStyleFile_Exists()
        {
            var reference = new Regex(
                @"(templateUrl|styleUrls)\s*:\s*\[?\s*""(?<path>\./[^""]+)""",
                RegexOptions.Compiled);

            var missing = new List<string>();

            foreach (var file in ComponentFiles())
            {
                var dir = Path.GetDirectoryName(file)!;

                foreach (Match m in reference.Matches(File.ReadAllText(file)))
                {
                    var target = Path.Combine(dir, m.Groups["path"].Value.Replace("./", ""));
                    if (!File.Exists(target))
                    {
                        missing.Add(
                            $"{Path.GetRelativePath(RepoRoot.Path, file)} -> {m.Groups["path"].Value}");
                    }
                }
            }

            missing.Should().BeEmpty("רכיב מפנה לקובץ תבנית או סגנון שאינו קיים");
        }
    }
}
