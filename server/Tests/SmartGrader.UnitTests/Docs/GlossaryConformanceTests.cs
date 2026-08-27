using System.Reflection;
using FluentAssertions;
using SmartGrader.Application.Common.Authorization;
using SmartGrader.Domain.Entities;
using Xunit;

namespace SmartGrader.UnitTests.Docs
{
    /// <summary>
    /// כל מזהה ש-<c>docs/glossary.md</c> מצמיד למונח עברי קיים באמת בקוד.
    /// <para>
    /// המילון הוא המסמך המאפשר של כל הסט: הוא מה שמתיר לשאר המסמכים לתאר קוד אנגלי בלי
    /// להתמלא בשמות טיפוסים — הכישלון המדויק של הסט הישן, שבו מטרת המורה נוסחה כ-
    /// "a correct AssignmentResponseDto". שינוי שם של <c>MethodName</c> מפיל כאן.
    /// </para>
    /// </summary>
    public class GlossaryConformanceTests
    {
        private const string Document = "glossary.md";

        /// <summary>
        /// עוגן לכל הרכבה שהמילון מפנה אליה. שם ההרכבה בפועל הוא <c>Domain</c> ו-
        /// <c>Application</c> — לא מרחב השמות <c>SmartGrader.*</c>.
        /// </summary>
        private static readonly IReadOnlyDictionary<string, Assembly> Assemblies =
            new Dictionary<string, Assembly>(StringComparer.Ordinal)
            {
                ["Domain"] = typeof(Submission).Assembly,
                ["Application"] = typeof(TestVisibility).Assembly
            };

        // כל שורה במילון מצביעה על טיפוס או חבר קיים
        [Fact]
        public void EveryIdentifier_ExistsInTheCode()
        {
            var blocks = GenBlock.FindAll(RepoRoot.ReadDoc(Document), "identifiers");

            blocks.Should().NotBeEmpty($"{Document} חייב להחזיק לפחות בלוק gen:identifiers אחד");

            var checkedRows = 0;
            var missing = new List<string>();

            foreach (var block in blocks)
            {
                Assemblies.Should().ContainKey(block.Argument,
                    $"{Document} מפנה להרכבה {block.Argument} שאין לה עוגן בטסט");

                var assembly = Assemblies[block.Argument];

                foreach (var row in block.Rows)
                {
                    var identifier = row[1];
                    checkedRows++;

                    if (!Exists(assembly, identifier))
                        missing.Add($"{block.Argument}: {identifier}");
                }
            }

            // רצפה: פרסור שלא מצא כמעט כלום הוא רג'קס שבור, לא מילון תקין
            checkedRows.Should().BeGreaterThan(40,
                "המילון מונה כארבעים שורות — ספירה נמוכה פירושה שהפרסור נשבר");

            missing.Should().BeEmpty(
                $"{Document} מצמיד מונחים למזהים שאינם קיימים עוד בקוד");
        }

        private static bool Exists(Assembly assembly, string identifier)
        {
            var parts = identifier.Split('.');

            var type = assembly.GetTypes()
                .FirstOrDefault(t => t.IsPublic && t.Name == parts[0]);

            if (type is null)
                return false;

            if (parts.Length == 1)
                return true;

            // מאפיין, שדה (כולל חבר enum, שהוא שדה סטטי) או מתודה
            return type.GetMember(
                parts[1],
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Length > 0;
        }

        // בלי הבדיקה הזו טעות כתיב במזהה הייתה נראית כמו טיפוס פרטי, כלומר עוברת בשקט
        [Fact]
        public void TheCheckItself_RejectsAnIdentifierThatDoesNotExist()
        {
            Exists(typeof(Submission).Assembly, "Assignment.ThisFieldDoesNotExist").Should().BeFalse();
            Exists(typeof(Submission).Assembly, "NoSuchType").Should().BeFalse();
            Exists(typeof(Submission).Assembly, "Assignment.MethodName").Should().BeTrue();
            Exists(typeof(Submission).Assembly, "RuleSeverity.Blocking").Should().BeTrue();
        }
    }
}
