using System.Net.Http.Json;
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
            var requestBody = new
            {
                source_code = wrappedSource,
                language_id = _options.LanguageId,
                stdin = test.Input,
                expected_output = test.Expected,
                cpu_time_limit = _options.TimeoutSeconds
            };

            var response = await _httpClient.PostAsJsonAsync(
                "submissions?wait=true",
                requestBody,
                ct);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<Judge0Response>(
                cancellationToken: ct);

            if (result is null)
            {
                details.Add(new TestCaseResult(
                    Input: test.Input,
                    Expected: test.Expected,
                    Actual: "",
                    Passed: false,
                    Error: "No response from Judge0"));
                continue;
            }

            // Compilation error — abort all remaining tests
            if (result.Status?.Id == 6)
            {
                return new RunnerResult(
                    Passed: 0,
                    Total: tests.Count,
                    HasCompileError: true,
                    CompileError: result.CompileOutput,
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
                    Error: "Time Limit Exceeded"));
                continue;
            }

            // Accepted
            if (result.Status?.Id == 3)
            {
                passed++;
                details.Add(new TestCaseResult(
                    Input: test.Input,
                    Expected: test.Expected,
                    Actual: result.Stdout?.TrimEnd() ?? "",
                    Passed: true,
                    Error: null));
            }
            else
            {
                details.Add(new TestCaseResult(
                    Input: test.Input,
                    Expected: test.Expected,
                    Actual: result.Stdout?.TrimEnd() ?? "",
                    Passed: false,
                    Error: result.Stderr ?? result.CompileOutput));
            }
        }

        return new RunnerResult(
            Passed: passed,
            Total: tests.Count,
            HasCompileError: false,
            CompileError: null,
            Details: details);
    }

    private static string BuildWrappedSource(
        string sourceCode,
        string methodName,
        IReadOnlyList<(string Type, string Name)> parameters)
    {
        return $@"
using System;
using System.Linq;
public static class StudentSolution
{{
    {sourceCode}
}}
public class Program
{{
    public static void Main(string[] args)
    {{
        // תואם Mono (language_id 51). כשעוברים ל-.NET מודרני ניתן להחזיר ל-Console.ReadLine()!.Split
        var parts = (Console.ReadLine() ?? """").Split(' ');
        var result = StudentSolution.{methodName}({BuildArgs(parameters)});
        Console.WriteLine(result);
    }}
}}";
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

        var filesBlock = string.Join("\n\n", sourceFiles.Select(f => f.Content));

        return $@"
using System;
using System.Linq;
using System.Text.Json;
{filesBlock}
public class Program
{{
    public static void Main(string[] args)
    {{
        var stdin = Console.ReadLine() ?? ""[]"";
        var argsJson = JsonDocument.Parse(stdin).RootElement;
        var result = {entryClassName}.{methodName}({BuildJsonArgs(parameters)});
        Console.WriteLine(result);
    }}
}}";
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
