using FluentAssertions;
using Xunit;

namespace SmartGrader.UnitTests.Docs
{
    /// <summary>
    /// לכל כלל עסקי ב-<c>docs/business-rules.md</c> יש מזהה ייחודי ועוגן שמצביע על קובץ קיים.
    /// <para>
    /// ⚠️ העוגן הוא <b>איפה הכלל חי</b>, לא הוכחה שהוא עובד. קובץ שעבר מקום מפיל כאן; כלל
    /// שנשבר מפיל בטסט שלו. שני דברים שונים, בכוונה — עוגן שמנסה להיות גם ראיה הופך
    /// לטסט שאף אחד לא מבין למה הוא אדום.
    /// </para>
    /// </summary>
    public class BusinessRuleAnchorTests
    {
        private const string Document = "business-rules.md";

        private static IReadOnlyList<(string Id, string Anchor)> Rules()
        {
            var blocks = GenBlock.FindAll(RepoRoot.ReadDoc(Document), "rules");
            blocks.Should().HaveCount(1, $"{Document} מחזיק בלוק כללים אחד");
            blocks[0].Argument.Should().Be("B");

            return blocks[0].Rows.Select(r => (Id: r[0], Anchor: r[2])).ToList();
        }

        // כל נתיב שמצוטט עדיין קיים
        [Fact]
        public void EveryAnchor_PointsAtAFileThatExists()
        {
            var rules = Rules();

            rules.Should().HaveCountGreaterThan(30,
                "אם נמצאו כמעט אפס כללים, הפרסור נשבר ולא המסמך");

            var broken = rules
                .Where(r => !File.Exists(Path.Combine(RepoRoot.Path, r.Anchor.Replace('/', Path.DirectorySeparatorChar))))
                .Select(r => $"{r.Id} → {r.Anchor}")
                .ToList();

            broken.Should().BeEmpty($"עוגנים ב-{Document} שמצביעים על קבצים שאינם קיימים");
        }

        // מזהים ייחודיים ורצופים — B-N מצוטט ממסמכי האזורים ואסור שימוחזר
        [Fact]
        public void RuleIds_AreUniqueAndSequential()
        {
            var ids = Rules().Select(r => r.Id).ToList();

            ids.Should().OnlyHaveUniqueItems();
            ids.Should().Equal(Enumerable.Range(1, ids.Count).Select(n => $"B-{n}"),
                $"{Document} ממספר B-1 ומעלה ברצף");
        }

        // הכלל נאמר פעם אחת — הרישום הוא המקור, לא server/CLAUDE.md
        [Fact]
        public void ServerClaudeMd_LinksToTheRegistry_RatherThanRestatingIt()
        {
            var claudeMd = File.ReadAllText(Path.Combine(RepoRoot.Path, "server", "CLAUDE.md"));

            claudeMd.Should().Contain("business-rules.md",
                "server/CLAUDE.md חייב להפנות לרישום; שני מקורות אמת לאותו כלל, והעותק הוא זה שמשתבש");
        }
    }
}
