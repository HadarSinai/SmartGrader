using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace SmartGrader.UnitTests.Docs
{
    /// <summary>
    /// <c>docs/README.md</c> הוא מפה, וטסט אחד שומר עליה: כל מסמך מופיע בה, וכל קישור יחסי
    /// נפתר. זה מה שהורג את "ה-README עדיין מפנה ל-lessonresults-jtbd.md".
    /// </summary>
    public class DocsIndexTests
    {
        private const string Index = "README.md";

        private static readonly Regex MarkdownLink =
            new(@"\[[^\]]*\]\((?<target>[^)\s]+)\)", RegexOptions.Compiled);

        // כל קובץ md תחת docs/ מוזכר במפה — כולל הסט הישן, שמופיע כ"מוחלף"
        [Fact]
        public void EveryDocument_IsMentionedInTheIndex()
        {
            var index = RepoRoot.ReadDoc(Index);

            var all = Directory
                .GetFiles(RepoRoot.DocsDir, "*.md", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(RepoRoot.DocsDir, f).Replace('\\', '/'))
                .Where(rel => !rel.Equals(Index, StringComparison.OrdinalIgnoreCase))
                .OrderBy(rel => rel, StringComparer.Ordinal)
                .ToList();

            all.Should().HaveCountGreaterThan(15,
                "אם הסריקה מצאה כמעט כלום, הנתיב שגוי ולא המפה");

            var unmentioned = all
                .Where(rel => !index.Contains(rel, StringComparison.OrdinalIgnoreCase))
                .ToList();

            unmentioned.Should().BeEmpty(
                $"מסמכים תחת docs/ שאינם מופיעים ב-{Index} — מסמך שאי אפשר להגיע אליו מהמפה");
        }

        // כל קישור יחסי במסמכי המפרט מצביע על קובץ שקיים
        [Fact]
        public void EveryRelativeLink_InTheSpecSet_Resolves()
        {
            var broken = new List<string>();
            var checkedLinks = 0;

            foreach (var file in RepoRoot.SpecDocs())
            {
                var dir = Path.GetDirectoryName(file)!;

                foreach (Match match in MarkdownLink.Matches(File.ReadAllText(file)))
                {
                    var target = match.Groups["target"].Value;

                    if (target.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                        target.StartsWith('#'))
                        continue;

                    // עוגן בתוך קובץ אחר — הקובץ הוא מה שנבדק
                    var path = target.Split('#')[0];
                    if (path.Length == 0)
                        continue;

                    checkedLinks++;

                    var resolved = Path.GetFullPath(Path.Combine(dir, path));
                    if (!File.Exists(resolved) && !Directory.Exists(resolved))
                        broken.Add($"{Path.GetFileName(file)} → {target}");
                }
            }

            checkedLinks.Should().BeGreaterThan(20,
                "אם כמעט לא נבדקו קישורים, הפרסור נשבר");

            broken.Should().BeEmpty("קישורים יחסיים שבורים במסמכי המפרט");
        }

        // המפה מונה את שלושה-עשר המסמכים — פחות מזה, ומשהו נשמט מהתכנון
        [Fact]
        public void TheIndex_NamesAllThirteenDocuments()
        {
            var index = RepoRoot.ReadDoc(Index);

            string[] planned =
            [
                "glossary.md", "domain-model.md", "permissions.md",
                "grading-rules.md", "business-rules.md", "design-system.md",
                "areas/teacher-content.md", "areas/teacher-classroom.md", "areas/student.md",
                "areas/admin.md", "areas/auth-account.md", "areas/shared-ui.md"
            ];

            foreach (var doc in planned)
                index.Should().Contain(doc, $"{Index} חייב לנקוב ב-{doc}, גם לפני שנכתב");
        }
    }
}
