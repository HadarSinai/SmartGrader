using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace SmartGrader.UnitTests.Docs
{
    /// <summary>
    /// מטריצת ההרשאות מכסה כל endpoint בפועל, עם אותם תפקידים; וטבלת הנתיבים מכסה כל
    /// נתיב ב-<c>app.routes.ts</c>.
    /// <para>
    /// ⚠️ <b>פרסור טקסט ולא רפלקציה, בכוונה.</b> פרויקט הטסטים מוגבל ל-Domain+Application
    /// (ר' <c>backend-unit-test-pattern</c>), והוספת הפניה ל-<c>Api</c> רק כדי לקרוא
    /// <c>[Authorize]</c> הייתה גוררת את כל שרת ה-web לתוך טסטי היחידה בשביל טבלה.
    /// <c>app.routes.ts</c> הוא TypeScript ואין ללקוח פרויקט טסטים בכלל.
    /// </para>
    /// <para>
    /// 🔴 זה גם הטסט שהיה תופס את לוח הבקרה המת: <c>GET /api/students/submissions/recent</c>
    /// לא התאים לשום route בשרת במשך שבועות, מפני ששום דבר לא השווה בין הכתובות של הלקוח
    /// לנתיבים של השרת.
    /// </para>
    /// </summary>
    public class PermissionsMatrixConformanceTests
    {
        private const string Document = "permissions.md";

        // ── endpoints ──────────────────────────────────────────────────────────

        // כל action ב-controller מופיע במטריצה, ועם אותם תפקידים
        [Fact]
        public void EveryEndpoint_HasAMatrixRowWithMatchingRoles()
        {
            var actual = ScanControllers();
            var documented = DocumentedEndpoints();

            // רצפה: פרסור טקסט שמצא כמעט כלום הוא רג'קס שבור, ואז הטסט "עובר" על ריק
            actual.Should().HaveCountGreaterThan(40,
                "אם הסריקה מצאה פחות מארבעים endpoints, הרג'קס נשבר ולא הקוד");

            var missing = actual.Keys.Except(documented.Keys).OrderBy(k => k).ToList();
            missing.Should().BeEmpty($"endpoints שקיימים בקוד ואין להם שורה ב-{Document}");

            var extra = documented.Keys.Except(actual.Keys).OrderBy(k => k).ToList();
            extra.Should().BeEmpty($"שורות ב-{Document} שאין להן endpoint בקוד");

            var wrongRoles = actual.Keys
                .Where(k => documented[k] != actual[k])
                .Select(k => $"{k}: המסמך אומר '{documented[k]}', הקוד אומר '{actual[k]}'")
                .OrderBy(s => s)
                .ToList();

            wrongRoles.Should().BeEmpty($"תפקידים ב-{Document} שאינם תואמים ל-[Authorize] בקוד");
        }

        private static Dictionary<string, string> DocumentedEndpoints()
        {
            var blocks = GenBlock.FindAll(RepoRoot.ReadDoc(Document), "endpoints");
            blocks.Should().HaveCount(1, $"{Document} מחזיק בלוק endpoints אחד");

            return blocks[0].Rows.ToDictionary(
                r => Key(r[0], r[1]),
                r => NormalizeRoles(r[2]),
                StringComparer.Ordinal);
        }

        private static string Key(string method, string route) =>
            $"{method.Trim().ToUpperInvariant()} {route.Trim().ToLowerInvariant()}";

        /// <summary>מיון התפקידים, כדי שסדר הכתיבה ב-<c>[Authorize]</c> לא יהיה חלק מהחוזה.</summary>
        private static string NormalizeRoles(string roles)
        {
            var trimmed = roles.Trim();
            if (trimmed.StartsWith('('))
                return trimmed;

            return string.Join(",", trimmed
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .OrderBy(r => r, StringComparer.Ordinal));
        }

        private static readonly Regex ClassDeclaration =
            new(@"^\s*public\s+(?:sealed\s+)?class\s+(?<name>\w+)Controller\b", RegexOptions.Compiled);

        private static readonly Regex RouteAttribute =
            new(@"^\s*\[Route\(""(?<template>[^""]+)""\)\]", RegexOptions.Compiled);

        private static readonly Regex AuthorizeAttribute =
            new(@"^\s*\[Authorize(?:\(Roles\s*=\s*""(?<roles>[^""]*)""\))?\]", RegexOptions.Compiled);

        private static readonly Regex HttpAttribute =
            new(@"^\s*\[Http(?<verb>Get|Post|Put|Patch|Delete)(?:\(""(?<template>[^""]*)""\))?\]",
                RegexOptions.Compiled);

        private static readonly Regex MemberDeclaration =
            new(@"^\s*(public|private|protected|internal)\s", RegexOptions.Compiled);

        private static Dictionary<string, string> ScanControllers()
        {
            var found = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var file in Directory.GetFiles(RepoRoot.ControllersDir, "*.cs"))
            {
                var lines = File.ReadAllLines(file);

                var classIndex = Array.FindIndex(lines, l => ClassDeclaration.IsMatch(l));
                if (classIndex < 0)
                    continue; // ApiControllerBase, ו-SubmissionController שכולו בהערה

                var controllerName = ClassDeclaration.Match(lines[classIndex]).Groups["name"].Value;

                string? classRoute = null;
                string? classRoles = null;

                for (var i = 0; i < classIndex; i++)
                {
                    var route = RouteAttribute.Match(lines[i]);
                    if (route.Success)
                        classRoute = route.Groups["template"].Value
                            .Replace("[controller]", controllerName, StringComparison.Ordinal);

                    var auth = AuthorizeAttribute.Match(lines[i]);
                    if (auth.Success)
                        classRoles = auth.Groups["roles"].Success && auth.Groups["roles"].Value.Length > 0
                            ? auth.Groups["roles"].Value
                            : "(any)";
                }

                if (classRoute is null)
                    continue;

                for (var i = classIndex; i < lines.Length; i++)
                {
                    var http = HttpAttribute.Match(lines[i]);
                    if (!http.Success)
                        continue;

                    var (roles, anonymous) = ActionAuthorization(lines, i + 1);

                    var template = http.Groups["template"].Value;
                    var route = template.Length == 0 ? classRoute : $"{classRoute}/{template}";

                    var effective = anonymous
                        ? "(anonymous)"
                        : roles ?? classRoles ?? "(anonymous)";

                    found[Key(http.Groups["verb"].Value, route)] = NormalizeRoles(effective);
                }
            }

            return found;
        }

        /// <summary>
        /// התכונות שבין תכונת ה-Http לחתימת המתודה. כל מה שביניהן שייך ל-action הזה.
        /// </summary>
        private static (string? Roles, bool Anonymous) ActionAuthorization(string[] lines, int from)
        {
            string? roles = null;
            var anonymous = false;

            for (var j = from; j < lines.Length; j++)
            {
                if (MemberDeclaration.IsMatch(lines[j]))
                    break;

                if (lines[j].Contains("[AllowAnonymous]", StringComparison.Ordinal))
                    anonymous = true;

                var auth = AuthorizeAttribute.Match(lines[j]);
                if (auth.Success)
                    roles = auth.Groups["roles"].Success && auth.Groups["roles"].Value.Length > 0
                        ? auth.Groups["roles"].Value
                        : "(any)";
            }

            return (roles, anonymous);
        }

        // ── client routes ──────────────────────────────────────────────────────

        private static readonly Regex PathLiteral =
            new(@"path:\s*""(?<path>[^""]*)""", RegexOptions.Compiled);

        /// <summary>
        /// השוואה <b>מסודרת</b> ולא כקבוצה: הנתיב "lessons" מופיע גם באזור המורה וגם ב-
        /// <c>/my</c>, וקבוצה הייתה מאבדת את הכפילות ואיתה כל שינוי בתוך אחד משני האזורים.
        /// </summary>
        [Fact]
        public void EveryClientRoute_HasARowInTheSameOrder()
        {
            var actual = PathLiteral
                .Matches(File.ReadAllText(RepoRoot.ClientRoutesFile))
                .Select(m => m.Groups["path"].Value)
                .ToList();

            actual.Should().HaveCountGreaterThan(30,
                "אם הסריקה מצאה פחות משלושים נתיבים, הרג'קס נשבר ולא app.routes.ts");

            var blocks = GenBlock.FindAll(RepoRoot.ReadDoc(Document), "routes");
            blocks.Should().HaveCount(1, $"{Document} מחזיק בלוק routes אחד");

            var documented = blocks[0].Rows.Select(r => r[0]).ToList();

            documented.Should().Equal(actual,
                $"טבלת הנתיבים ב-{Document} התיישנה מול app.routes.ts");
        }
    }
}
