using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace SmartGrader.UnitTests.Docs
{
    /// <summary>
    /// כל נתיב ב-<c>app.routes.ts</c> נתבע על ידי <b>בדיוק מסמך אזור אחד</b>.
    /// <para>
    /// זו בדיקת הכיסוי שהתוכנית מנסחת כתנאי קבלה: "אם מסך אינו נקוב בדיוק במסמך אזור אחד,
    /// זה פגם". מסך שנשמט אינו מתגלה בקריאה — הוא פשוט אינו שם.
    /// </para>
    /// <para>
    /// ⚠️ השוואת <b>ריבוי</b> ולא קבוצה: הנתיב "lessons" מופיע גם אצל המורה וגם ב-<c>/my</c>,
    /// ו-"" מופיע שלוש פעמים. קבוצה הייתה בולעת את הכפילויות ואיתן כל מסך שנתבע פעמיים.
    /// </para>
    /// </summary>
    public class AreaRouteCoverageTests
    {
        private static readonly Regex PathLiteral =
            new(@"path:\s*""(?<path>[^""]*)""", RegexOptions.Compiled);

        private static List<string> ActualRoutes() =>
            PathLiteral
                .Matches(File.ReadAllText(RepoRoot.ClientRoutesFile))
                .Select(m => m.Groups["path"].Value)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();

        private static List<(string Area, string Path)> ClaimedRoutes()
        {
            var claims = new List<(string, string)>();
            var areasDir = Path.Combine(RepoRoot.DocsDir, "areas");

            foreach (var file in Directory.GetFiles(areasDir, "*.md"))
                foreach (var block in GenBlock.FindAll(File.ReadAllText(file), "arearoutes"))
                    foreach (var row in block.Rows)
                        claims.Add((block.Argument, row[0]));

            return claims;
        }

        // כל הנתיבים נתבעים, ואף אחד לא פעמיים
        [Fact]
        public void EveryClientRoute_IsClaimedByExactlyOneAreaDocument()
        {
            var claims = ClaimedRoutes();

            claims.Should().HaveCountGreaterThan(30,
                "אם נמצאו כמעט אפס תביעות, הפרסור נשבר ולא המסמכים");

            var claimed = claims.Select(c => c.Path).OrderBy(p => p, StringComparer.Ordinal).ToList();

            claimed.Should().Equal(ActualRoutes(),
                "כל נתיב ב-app.routes.ts חייב להיתבע בדיוק פעם אחת על פני מסמכי האזורים — " +
                "נתיב חסר הוא מסך שלא אופיין, ונתיב כפול הוא שני מסמכים שסותרים זה את זה");
        }

        /// <summary>
        /// סעיף Screen Composition נכתב ואינו נשאר placeholder.
        /// <para>
        /// ⚠️ לא נבדק <b>מה</b> כתוב שם — זו החלטה עיצובית, כלומר class B. נבדק רק שהסעיף
        /// קיים ושהטקסט הזמני של שלב A4 לא שרד, כי placeholder שנשאר נקרא כמו החלטה.
        /// </para>
        /// </summary>
        [Fact]
        public void EveryAreaDocument_HasAWrittenScreenComposition()
        {
            var areasDir = Path.Combine(RepoRoot.DocsDir, "areas");
            var files = Directory.GetFiles(areasDir, "*.md");

            files.Should().HaveCount(6);

            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                var name = Path.GetFileName(file);

                text.Should().Contain("## Screen Composition",
                    $"{name} חייב להחזיק סעיף Screen Composition");

                text.Should().NotContain("*Filled in phase A5.*",
                    $"{name} עדיין מחזיק את הטקסט הזמני של A4 — placeholder שנשאר נקרא כמו החלטה");
            }
        }

        // ששת מסמכי האזורים קיימים ותובעים משהו
        [Fact]
        public void AllSixAreaDocuments_Exist_AndClaimRoutes()
        {
            string[] expected =
            [
                "teacher-content", "teacher-classroom", "student",
                "admin", "auth-account", "shared-ui"
            ];

            var areas = ClaimedRoutes().Select(c => c.Area).Distinct().OrderBy(a => a).ToList();

            areas.Should().BeEquivalentTo(expected,
                "ששת מסמכי האזורים מכסים את שלושה-עשר אזורי המסכים ואת הרכיבים המשותפים");
        }
    }
}
