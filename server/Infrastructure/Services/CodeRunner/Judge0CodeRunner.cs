using System.Net.Http.Json;
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

    public async Task<RunnerResult> RunAsync(
        string sourceCode,
        string methodName,
        IReadOnlyList<TestCase> tests,
        CancellationToken ct = default)
    {
        var details = new List<TestCaseResult>();
        int passed = 0;

        var parameters = ExtractParameters(sourceCode, methodName);
        string wrappedSource = BuildWrappedSource(sourceCode, methodName, parameters);

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
        var parts = Console.ReadLine()!.Split(' ');
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
