using FluentAssertions;
using SmartGrader.Domain.Entities;
using Xunit;

namespace SmartGrader.UnitTests.Docs
{
    /// <summary>
    /// כל טבלת enum ב-<c>docs/domain-model.md</c> מושווית ל-enum האמיתי — שמות וערכים.
    /// <para>
    /// זה מה שמסיים לתמיד את "הטבלה מונה 4 והדומיין מחזיק 7", ואת "33 מבנים בקטלוג" כשיש
    /// 31. לא חריצות — טסט אדום.
    /// </para>
    /// <para>
    /// ⚠️ עמודת <c>Meaning</c> אינה נבדקת. היא פרוזה, היא לא נגזרת מהקוד ולכן אינה יכולה
    /// להתיישן, ובדיקה שלה הייתה הופכת כל תיקון ניסוח למאבק — ואז מוחקים את הסימונים.
    /// </para>
    /// </summary>
    public class EnumTableConformanceTests
    {
        private const string Document = "domain-model.md";

        // כל בלוק gen:enum מתאר enum קיים, על שמותיו וערכיו
        [Fact]
        public void EveryEnumBlock_MatchesTheDomainType()
        {
            var blocks = GenBlock.FindAll(RepoRoot.ReadDoc(Document), "enum");

            blocks.Should().HaveCount(6,
                $"{Document} מתעד את כל ששת ה-enums של הדומיין — בלוק חסר הוא טבלה בלי שומר");

            foreach (var block in blocks)
            {
                var type = typeof(Submission).Assembly.GetType(block.Argument);

                type.Should().NotBeNull(
                    $"{Document} מפנה אל {block.Argument}, שאינו קיים בהרכבת הדומיין");
                type!.IsEnum.Should().BeTrue($"{block.Argument} אינו enum");

                var documented = block.Rows
                    .Select(r => (Member: r[0], Value: int.Parse(r[1])))
                    .OrderBy(x => x.Value)
                    .ToList();

                var actual = Enum.GetValues(type)
                    .Cast<object>()
                    .Select(v => (Member: v.ToString()!, Value: (int)v))
                    .OrderBy(x => x.Value)
                    .ToList();

                documented.Should().Equal(actual,
                    $"הטבלה של {type.Name} ב-{Document} התיישנה מול הקוד");
            }
        }

        // שני ה-enums שהמסמך הישן טעה בהם, מוצמדים במפורש
        [Theory]
        [InlineData(typeof(SubmissionStatus), 7)]
        [InlineData(typeof(CodeConstruct), 31)]
        public void CountsTheOldSpecGotWrong_AreStillWhatTheDocumentSays(Type enumType, int expected)
        {
            Enum.GetValues(enumType).Length.Should().Be(expected);
        }
    }
}
