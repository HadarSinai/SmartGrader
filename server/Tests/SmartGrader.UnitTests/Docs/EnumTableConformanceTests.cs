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
        /// <summary>
        /// כל מסמכי המפרט, ולא <c>domain-model.md</c> בלבד: טבלת הסטטוסים ב-
        /// <c>design-system.md</c> נגזרת מאותו enum, ושתי הטבלאות חייבות להתיישן יחד.
        /// </summary>
        [Fact]
        public void EveryEnumBlock_InEveryDocument_MatchesTheDomainType()
        {
            var found = 0;

            foreach (var file in RepoRoot.SpecDocs())
            {
                var name = Path.GetFileName(file);

                foreach (var block in GenBlock.FindAll(File.ReadAllText(file), "enum"))
                {
                    found++;

                    var type = typeof(Submission).Assembly.GetType(block.Argument);

                    type.Should().NotBeNull(
                        $"{name} מפנה אל {block.Argument}, שאינו קיים בהרכבת הדומיין");
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
                        $"הטבלה של {type.Name} ב-{name} התיישנה מול הקוד");
                }
            }

            // ששת ה-enums ב-domain-model.md ועוד טבלת הסטטוסים ב-design-system.md
            found.Should().BeGreaterThanOrEqualTo(7,
                "בלוק gen:enum שנעלם הוא טבלה שנשארה בלי שומר");
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
