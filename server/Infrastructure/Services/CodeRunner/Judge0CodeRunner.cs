using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SmartGrader.Application.Services.CodeRunner;
using SmartGrader.Domain.Entities;

namespace SmartGrader.Infrastructure.Services.CodeRunner;

public sealed class Judge0CodeRunner : ICodeRunnerService
{
    private readonly HttpClient _httpClient;
    private readonly Judge0Options _options;

    public Judge0CodeRunner(HttpClient httpClient, IOptions<Judge0Options> options)
    {
        _options = options.Value;
        _httpClient = httpClient;

        var baseUri = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        _httpClient.BaseAddress = baseUri;

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            if (baseUri.Host.EndsWith(".rapidapi.com", StringComparison.OrdinalIgnoreCase))
            {
                // Hosted Judge0 CE via RapidAPI
                _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Key", _options.ApiKey);
                _httpClient.DefaultRequestHeaders.Add("X-RapidAPI-Host", baseUri.Host);
            }
            else
            {
                // Self-hosted Judge0 with authentication enabled
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_options.ApiKey}");
            }
        }
    }

    // ── נתיב קובץ יחיד (legacy) — ללא שינוי התנהגות ─────────────────────────

    public async Task<RunnerResult> RunAsync(
        string sourceCode,
        string methodName,
        IReadOnlyList<TestCase> tests,
        CancellationToken ct = default)
    {
        var parameters = ExtractParameters(sourceCode, methodName);
        string wrappedSource = BuildWrappedSource(sourceCode, methodName, parameters);

        return await RunTestsAsync(wrappedSource, tests, ct);
    }

    // ── נתיב רב-קובצי ────────────────────────────────────────────────────

    public async Task<RunnerResult> RunAsync(
        IReadOnlyList<SubmissionFile> sourceFiles,
        IReadOnlyList<ExpectedFile> expectedFiles,
        IReadOnlyList<TestCase> tests,
        CancellationToken ct = default)
    {
        // איזו מתודה קוראים בפועל: הראשון מבין ה-ExpectedFiles עם MethodName מוגדר,
        // וכברירת מחדל הקובץ הראשון (Judge0/Mono אין לו תמיכה אמיתית בפרויקט רב-קובצי —
        // זו הרכבה טקסטואלית לתוך יחידת קומפילציה אחת, לא קומפילציה רב-קובצית אמיתית).
        var entryFile = expectedFiles.FirstOrDefault(f => !string.IsNullOrWhiteSpace(f.MethodName))
                        ?? expectedFiles.FirstOrDefault();

        if (entryFile is null || string.IsNullOrWhiteSpace(entryFile.MethodName))
            throw new InvalidOperationException(
                "Multi-file submission requires at least one ExpectedFile with a MethodName.");

        string wrappedSource = BuildWrappedMultiFileSource(sourceFiles, entryFile.MethodName);

        return await RunTestsAsync(wrappedSource, tests, ct);
    }

    // ── נתיב GradingMode.FullProgram — תוכנית שלמה עם Main של התלמיד ───────

    public async Task<RunnerResult> RunProgramAsync(
        IReadOnlyList<SubmissionFile> sourceFiles,
        IReadOnlyList<TestCase> tests,
        CancellationToken ct = default)
    {
        // בלי עטיפה בכלל — הקבצים ממוזגים (usings מורמים לראש, ר' MergeFiles) ורצים כמו שהם.
        // ⚠️ מגבלה ידועה: זהו המסלול היחיד שבו ה-Main הוא של התלמידה, ולכן אי אפשר לכפות בו
        // CultureInfo.InvariantCulture כמו בשני המסלולים העטופים. בפועל קונטיינר בלי LANG רץ
        // ממילא בתרבות אינווריאנטית; אם הקונטיינר יוגדר עם locale בעל פסיק עשרוני, מקרי בדיקה
        // עם שברים עשרוניים במצב זה עלולים להיכשל על קוד תקין.
        string mergedSource = MergeFiles(sourceFiles.Select(f => f.Content));
        return await RunTestsAsync(mergedSource, tests, ct);
    }

    // ── לוגיקת הרצה משותפת מול Judge0 (זהה לשני הנתיבים) ────────────────────

    private async Task<RunnerResult> RunTestsAsync(
        string wrappedSource,
        IReadOnlyList<TestCase> tests,
        CancellationToken ct)
    {
        var details = new List<TestCaseResult>();
        int passed = 0;

        foreach (var test in tests)
        {
            // Judge0 (RapidAPI) דוחה עם 400 כל שדה שמכיל תווים שאינם ASCII-ניתן-לייצוג
            // (כולל עברית — נפוץ מאוד כאן) אלא אם הוא מקודד ב-Base64 ומצוין base64_encoded=true.
            // ⚠️ expected_output לא נשלח בכוונה: ההשוואה נעשית כאן, אחרי נרמול של *שני* הצדדים
            // (ר' NormalizeOutput). כשההשוואה נעשתה ב-Judge0 מול הערך הגולמי מהטופס, רווח אחד
            // בסוף שורה או CRLF שהודבק מ-Word הפילו את כל הכיתה על קוד תקין לחלוטין.
            var requestBody = new
            {
                source_code = EncodeBase64(wrappedSource),
                language_id = _options.LanguageId,
                stdin = EncodeBase64(test.Input),
                cpu_time_limit = _options.TimeoutSeconds
            };

            var response = await _httpClient.PostAsJsonAsync(
                "submissions?wait=true&base64_encoded=true",
                requestBody,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                // גוף התשובה מכיל את הסיבה האמיתית (למשל שגיאת ולידציה של Judge0) —
                // EnsureSuccessStatusCode לבדו מאבד אותה ומשאיר רק את קוד הסטטוס בלוג.
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException(
                    $"Judge0 request failed: {(int)response.StatusCode} {response.StatusCode}. {errorBody}");
            }

            var result = await response.Content.ReadFromJsonAsync<Judge0Response>(
                cancellationToken: ct);

            if (result is null)
            {
                details.Add(new TestCaseResult(
                    Input: test.Input,
                    Expected: test.Expected,
                    Actual: "",
                    Passed: false,
                    Error: "No response from Judge0",
                    IsSample: test.IsSample,
                    StatusDescription: "No response from Judge0"));
                continue;
            }

            // Judge0 מחזיר stdout/stderr/compile_output גם הם ב-Base64 כש-base64_encoded=true.
            string? stdout = DecodeBase64(result.Stdout);
            string? stderr = DecodeBase64(result.Stderr);
            string? compileOutput = DecodeBase64(result.CompileOutput);

            // Judge0 Internal Error (13) — תקלת תשתית של Judge0 עצמו, לא בעיה בקוד התלמיד.
            // נזרק כחריגה ייעודית כדי שינותב ב-AiWorker ל-MarkJudgeUnavailable ולא ל"טסט שנכשל".
            if (result.Status?.Id == 13)
                throw new CodeRunnerUnavailableException(
                    $"Judge0 internal error: {result.Status?.Description ?? "Internal Error"}");

            // Compilation error — abort all remaining tests
            if (result.Status?.Id == 6)
            {
                return new RunnerResult(
                    Passed: 0,
                    Total: tests.Count,
                    HasCompileError: true,
                    CompileError: compileOutput,
                    Details: details);
            }

            // Time Limit Exceeded
            if (result.Status?.Id == 5)
            {
                details.Add(new TestCaseResult(
                    Input: test.Input,
                    Expected: test.Expected,
                    Actual: "",
                    Passed: false,
                    Error: "Time Limit Exceeded",
                    IsSample: test.IsSample,
                    StatusDescription: result.Status?.Description ?? "Time Limit Exceeded"));
                continue;
            }

            // Status 3 = ההרצה הסתיימה בהצלחה. בלי expected_output זה כל מה שהסטטוס אומר —
            // הנכונות נקבעת כאן, בהשוואה מנורמלת.
            var actual = stdout ?? "";

            if (result.Status?.Id == 3)
            {
                bool isCorrect = NormalizeOutput(actual) == NormalizeOutput(test.Expected);
                if (isCorrect) passed++;

                details.Add(new TestCaseResult(
                    Input: test.Input,
                    Expected: test.Expected,
                    Actual: actual.TrimEnd(),
                    Passed: isCorrect,
                    Error: null,
                    IsSample: test.IsSample,
                    StatusDescription: isCorrect ? "Accepted" : "Wrong Answer"));
            }
            else
            {
                // שגיאת ריצה (חלוקה באפס, חריגה ממערך, NZEC וכו') — הסטטוס מבדיל אותה
                // מתשובה שגויה, וזה בדיוק המידע שהיה נזרק עד עכשיו
                details.Add(new TestCaseResult(
                    Input: test.Input,
                    Expected: test.Expected,
                    Actual: actual.TrimEnd(),
                    Passed: false,
                    Error: stderr ?? compileOutput,
                    IsSample: test.IsSample,
                    StatusDescription: result.Status?.Description));
            }
        }

        return new RunnerResult(
            Passed: passed,
            Total: tests.Count,
            HasCompileError: false,
            CompileError: null,
            Details: details);
    }

    /// <summary>
    /// מנרמל פלט לפני השוואה — על <b>שני</b> הצדדים, גם על הפלט של התלמידה וגם על הפלט הצפוי
    /// שהמורה הקלידה בטופס.
    /// <list type="bullet">
    /// <item>CRLF → LF: פלט צפוי שהודבק מ-Word נכשל אחרת מול פלט של קונטיינר לינוקס.</item>
    /// <item>חיתוך רווחים בסוף <b>כל שורה</b>, לא רק בסוף הפלט: זה הסעיף הקריטי לתרגילי מטריצה —
    /// <c>Console.Write(m[i,j] + " ")</c> הוא הדרך המקובלת להדפיס מטריצה, ומשאיר רווח בלתי נראה
    /// בסוף כל שורה שהיה מפיל קוד נכון לחלוטין.</item>
    /// <item>השמטת שורות ריקות בסוף.</item>
    /// </list>
    /// רווחים בתוך השורה נשמרים — הם משמעותיים (למשל ההפרדה בין תאי המטריצה עצמם).
    /// </summary>
    private static string NormalizeOutput(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        var lines = value
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n')
            .Select(line => line.TrimEnd())
            .ToList();

        while (lines.Count > 0 && lines[^1].Length == 0)
            lines.RemoveAt(lines.Count - 1);

        return string.Join("\n", lines);
    }

    // ── Base64 עבור Judge0 (RapidAPI דוחה תווים לא-ASCII, כמו עברית, ללא base64_encoded=true) ──

    private static string EncodeBase64(string? value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? ""));

    private static string? DecodeBase64(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch (FormatException)
        {
            // ליתר ביטחון: אם ה-Judge0 בסביבה מסוימת מחזיר טקסט רגיל בכל זאת.
            return value;
        }
    }

    private static string BuildWrappedSource(
        string sourceCode,
        string methodName,
        IReadOnlyList<(string Type, string Name)> parameters)
    {
        // ⚠️ הקוד של התלמידה עובר דרך MergeFiles ולא מודבק גולמי לתוך ה-class: כל שיעור C#
        // נפתח ב-using System;, התלמידות כותבות אותו מתוך הרגל, ו-using בתוך גוף מחלקה הוא
        // CS1529 — שגיאת קומפילציה שאין לה שום קשר לתרגיל. MergeFiles מרים את שורות ה-using
        // לראש (עם dedupe) ומשאיר רק את הגוף, בדיוק כמו בשני המסלולים האחרים.
        var merged = MergeFiles(new[] { "using System;\nusing System.Globalization;\nusing System.Linq;", sourceCode });
        var (usings, body) = SplitUsingsAndBody(merged);

        return $@"
{usings}
public static class StudentSolution
{{
    {body}
}}
public class Program
{{
    public static void Main(string[] args)
    {{
{InvariantCultureSetup}
        // תואם Mono (language_id 51). כשעוברים ל-.NET מודרני ניתן להחזיר ל-Console.ReadLine()!.Split
        // RemoveEmptyEntries: רווח כפול בשדה ""קלט"" של המורה נתן [""3"", """", ""5""],
        // ו-int.Parse("""") זרק שגיאת ריצה שנראתה כמו באג בקוד של התלמידה.
        var parts = (Console.ReadLine() ?? """").Split(new[] {{ ' ' }}, StringSplitOptions.RemoveEmptyEntries);
        var result = StudentSolution.{methodName}({BuildArgs(parameters)});
        Console.WriteLine(result);
    }}
}}";
    }

    /// <summary>
    /// כופה תרבות אינווריאנטית על ההרצה כולה. בלי זה <c>Console.WriteLine(3.14)</c> ו-
    /// <c>double.Parse</c> מסתמכים על ה-locale של הקונטיינר: על locale עם פסיק עשרוני התוכנית
    /// מדפיסה <c>3,14</c> מול פלט צפוי <c>3.14</c>, וכל מקרה בדיקה עם שבר עשרוני נכשל בלי קשר לקוד.
    /// </summary>
    private const string InvariantCultureSetup =
        "        System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;\n" +
        "        System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;";

    /// <summary>
    /// מפצל פלט של <see cref="MergeFiles"/> לשורות ה-using שבראשו ולשאר הגוף — נחוץ רק למסלול
    /// שבו הגוף חייב להיכנס לתוך class בעוד ה-usings חייבים להישאר מחוצה לו.
    /// </summary>
    private static (string Usings, string Body) SplitUsingsAndBody(string merged)
    {
        var usings = UsingDirectiveRegex.Matches(merged)
            .Select(m => m.Value.Trim())
            .ToList();

        return (string.Join("\n", usings), UsingDirectiveRegex.Replace(merged, "").Trim());
    }

    private static IReadOnlyList<(string Type, string Name)> ExtractParameters(
        string sourceCode, string methodName)
    {
        var match = Regex.Match(
            sourceCode,
            $@"\b{Regex.Escape(methodName)}\s*\(([^)]*)\)",
            RegexOptions.Singleline);

        if (!match.Success || string.IsNullOrWhiteSpace(match.Groups[1].Value))
            return Array.Empty<(string, string)>();

        return match.Groups[1].Value
            .Split(',')
            .Select(p => p.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length >= 2)
            .Select(parts => (Type: parts[parts.Length - 2], Name: parts[parts.Length - 1]))
            .ToArray();
    }

    private static string BuildArgs(IReadOnlyList<(string Type, string Name)> parameters)
    {
        var args = new List<string>();
        for (int i = 0; i < parameters.Count; i++)
        {
            string arg = parameters[i].Type.ToLowerInvariant() switch
            {
                "int" or "int32"    => $"int.Parse(parts[{i}])",
                "long" or "int64"   => $"long.Parse(parts[{i}])",
                "double"            => $"double.Parse(parts[{i}])",
                "float"             => $"float.Parse(parts[{i}])",
                "bool" or "boolean" => $"bool.Parse(parts[{i}])",
                _                   => $"parts[{i}]"
            };
            args.Add(arg);
        }
        return string.Join(", ", args);
    }

    // ── בניית מקור רב-קובצי ──────────────────────────────────────────────

    private static string BuildWrappedMultiFileSource(
        IReadOnlyList<SubmissionFile> sourceFiles,
        string methodName)
    {
        // הרכבה טקסטואלית של כל הקבצים כ-class-ים נפרדים בתוך אותה יחידת קומפילציה
        // (Judge0/Mono ללא תמיכה רב-קובצית אמיתית). התנגשות שמות class אפשרית ותטופל בעתיד.
        var parameters = ExtractParameters(
            string.Join("\n", sourceFiles.Select(f => f.Content)),
            methodName);

        var entryClassName = FindClassContaining(sourceFiles, methodName) ?? "StudentSolution";

        // MergeFiles מרים usings של כל הקבצים לראש (כולל אלה שנוסיף כאן) — מונע CS1529
        // כשקובץ שני מתחיל ב-using אחרי שה-class-ים של הקובץ הראשון כבר נפתחו.
        var mergedFiles = MergeFiles(
            new[] { "using System;\nusing System.Globalization;\nusing System.Linq;\nusing System.Text.Json;" }
                .Concat(sourceFiles.Select(f => f.Content)));

        return $@"
{mergedFiles}
public class Program
{{
    public static void Main(string[] args)
    {{
{InvariantCultureSetup}
        var stdin = Console.ReadLine() ?? ""[]"";
        var argsJson = JsonDocument.Parse(stdin).RootElement;
        var result = {entryClassName}.{methodName}({BuildJsonArgs(parameters)});
        Console.WriteLine(result);
    }}
}}";
    }

    // ── מיזוג קבצים עם הרמת using לראש (משותף ל-FullProgram וגם למסלול הרב-קובצי הישן) ──

    private static readonly Regex UsingDirectiveRegex = new(
        @"^\s*using\s+[\w.]+(\s*=\s*[\w.<>,\s]+)?\s*;\s*$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// ממזג כמה קבצי מקור ליחידת קומפילציה אחת: כל שורת using ברמה עליונה מכל הקבצים
    /// מורמת לראש (עם dedupe), ואחריה גוף כל קובץ בלי שורות ה-using שלו. פותר CS1529
    /// שנוצר כשקובץ ב' מתחיל ב-using אחרי שה-class-ים של קובץ א' כבר נפתחו בהדבקה נאיבית.
    /// </summary>
    private static string MergeFiles(IEnumerable<string> fileContents)
    {
        var usings = new List<string>();
        var bodies = new List<string>();

        foreach (var content in fileContents)
        {
            foreach (Match m in UsingDirectiveRegex.Matches(content))
            {
                var directive = m.Value.Trim();
                if (!usings.Contains(directive, StringComparer.Ordinal))
                    usings.Add(directive);
            }

            bodies.Add(UsingDirectiveRegex.Replace(content, "").Trim());
        }

        return string.Join("\n", usings) + "\n" + string.Join("\n\n", bodies);
    }

    private static string? FindClassContaining(IReadOnlyList<SubmissionFile> sourceFiles, string methodName)
    {
        foreach (var file in sourceFiles)
        {
            var classMatch = Regex.Match(file.Content, @"class\s+(\w+)");
            var hasMethod = Regex.IsMatch(file.Content, $@"\b{Regex.Escape(methodName)}\s*\(");
            if (classMatch.Success && hasMethod)
                return classMatch.Groups[1].Value;
        }
        return null;
    }

    private static string BuildJsonArgs(IReadOnlyList<(string Type, string Name)> parameters)
    {
        var args = new List<string>();
        for (int i = 0; i < parameters.Count; i++)
        {
            string arg = parameters[i].Type.ToLowerInvariant() switch
            {
                "int" or "int32"    => $"argsJson[{i}].GetInt32()",
                "long" or "int64"   => $"argsJson[{i}].GetInt64()",
                "double"            => $"argsJson[{i}].GetDouble()",
                "float"             => $"(float)argsJson[{i}].GetDouble()",
                "bool" or "boolean" => $"argsJson[{i}].GetBoolean()",
                _                   => $"argsJson[{i}].GetString()"
            };
            args.Add(arg);
        }
        return string.Join(", ", args);
    }

    // ── Private deserialization models ──────────────────────────────────────

    private sealed class Judge0Response
    {
        [JsonPropertyName("status")]
        public Judge0Status? Status { get; init; }

        [JsonPropertyName("stdout")]
        public string? Stdout { get; init; }

        [JsonPropertyName("stderr")]
        public string? Stderr { get; init; }

        [JsonPropertyName("compile_output")]
        public string? CompileOutput { get; init; }

        [JsonPropertyName("time")]
        public string? Time { get; init; }

        [JsonPropertyName("memory")]
        public int? Memory { get; init; }
    }

    private sealed class Judge0Status
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }
    }
}
