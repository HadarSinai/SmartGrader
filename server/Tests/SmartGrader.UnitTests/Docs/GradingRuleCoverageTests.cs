using System.Reflection;
using FluentAssertions;
using Xunit;

namespace SmartGrader.UnitTests.Docs
{
    /// <summary>
    /// כל כלל ניקוד ב-<c>docs/grading-rules.md</c> קשור לטסט שמוכיח אותו, דרך
    /// <c>[Trait("Rule", "G-N")]</c>, ולהפך.
    /// <para>
    /// זה מה שמונע את הכיוון המסוכן: מוחקים התנהגות, הטסט הולך איתה, והמסמך ממשיך להבטיח
    /// כלל שכבר לא קיים. כאן הוא הופך אדום.
    /// </para>
    /// </summary>
    public class GradingRuleCoverageTests
    {
        private const string Document = "grading-rules.md";
        private const string Covered = "✅";

        private static IReadOnlyList<(string Id, string Coverage)> DocumentedRules()
        {
            var blocks = GenBlock.FindAll(RepoRoot.ReadDoc(Document), "rules");
            blocks.Should().HaveCount(1, $"{Document} מחזיק בלוק כללים אחד");
            blocks[0].Argument.Should().Be("G");

            return blocks[0].Rows.Select(r => (Id: r[0], Coverage: r[2])).ToList();
        }

        /// <summary>כל ערכי <c>[Trait("Rule", …)]</c> בפרויקט הטסטים.</summary>
        private static ILookup<string, string> RuleTraits()
        {
            var pairs =
                from type in AllTestTypes()
                from method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance |
                                               BindingFlags.Static | BindingFlags.DeclaredOnly)
                from attribute in method.GetCustomAttributesData()
                where attribute.AttributeType.Name == "TraitAttribute"
                      && attribute.ConstructorArguments.Count == 2
                      && (attribute.ConstructorArguments[0].Value as string) == "Rule"
                select new
                {
                    Rule = (string)attribute.ConstructorArguments[1].Value!,
                    Method = $"{type.Name}.{method.Name}"
                };

            return pairs.ToLookup(p => p.Rule, p => p.Method);
        }

        /// <summary>
        /// ⚠️ טיפוס שלא נטען הוא טיפוס שהטראיטים שלו אינם נראים, ואז "כלל בלי טסט" ו"הרכבה
        /// שלא נטענה" נראים זהה. מפרידים ביניהם כאן ולא משאירים אדום מטעה — במכונה הזו
        /// Smart App Control חוסם הרכבות טריות, וזה בדיוק המצב שנוצר.
        /// </summary>
        private static Type[] AllTestTypes()
        {
            try
            {
                return typeof(GradingRuleCoverageTests).Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                var reason = ex.LoaderExceptions.FirstOrDefault()?.Message ?? "unknown";
                throw new InvalidOperationException(
                    "טיפוסי הטסטים לא נטענו, ולכן אי אפשר לדעת אילו כללים מכוסים. " +
                    $"זו תקלת סביבה ולא כיסוי חסר: {reason}", ex);
            }
        }

        // כל כלל שמסומן מכוסה — יש לו לפחות טסט אחד
        [Fact]
        public void EveryCoveredRule_HasAtLeastOneTest()
        {
            var traits = RuleTraits();

            var uncovered = DocumentedRules()
                .Where(r => r.Coverage == Covered && !traits.Contains(r.Id))
                .Select(r => r.Id)
                .ToList();

            uncovered.Should().BeEmpty(
                $"{Document} מסמן את הכללים האלה כמכוסים, ואין להם אף [Trait(\"Rule\", …)]");
        }

        // כלל שהמסמך מודה שאינו מכוסה — אם נכתב לו טסט, המסמך צריך להתעדכן
        [Fact]
        public void EveryUncoveredRule_ReallyHasNoTest()
        {
            var traits = RuleTraits();

            var nowCovered = DocumentedRules()
                .Where(r => r.Coverage != Covered && traits.Contains(r.Id))
                .Select(r => r.Id)
                .ToList();

            nowCovered.Should().BeEmpty(
                $"נכתבו טסטים לכללים ש-{Document} עדיין מסמן כלא-מכוסים — לעדכן את הטבלה");
        }

        // טסט שמצביע על כלל שאינו קיים — כנראה מזהה שהוסב או הוסר
        [Fact]
        public void EveryTrait_PointsAtARuleThatExists()
        {
            var documented = DocumentedRules().Select(r => r.Id).ToHashSet(StringComparer.Ordinal);

            var orphans = RuleTraits()
                .Where(g => !documented.Contains(g.Key))
                .Select(g => $"{g.Key} ({string.Join(", ", g)})")
                .ToList();

            orphans.Should().BeEmpty($"[Trait] שמפנה לכלל שאינו קיים ב-{Document}");
        }

        // מזהים ייחודיים ורצופים — מספור מחדש מנתק בשקט כל [Trait] קיים
        [Fact]
        public void RuleIds_AreUniqueAndSequential()
        {
            var ids = DocumentedRules().Select(r => r.Id).ToList();

            ids.Should().HaveCountGreaterThan(15, "אם נמצאו כמעט אפס כללים, הפרסור נשבר");
            ids.Should().OnlyHaveUniqueItems();
            ids.Should().Equal(Enumerable.Range(1, ids.Count).Select(n => $"G-{n}"),
                $"{Document} ממספר G-1 ומעלה ברצף; מזהה אינו ממוחזר ואינו ממוספר מחדש");
        }
    }
}
