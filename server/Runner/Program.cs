using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

record TestCaseDto(string Input, string Expected);
record RunnerRequest(string SourceCode, string MethodName, List<TestCaseDto> Tests);
record TestCaseResultDto(string Input, string Expected, string Actual, bool Passed, string? Error);
record RunnerResponse(int Passed, int Total, List<TestCaseResultDto> Details, string? CompileError);

static MetadataReference Ref<T>() => MetadataReference.CreateFromFile(typeof(T).Assembly.Location);

var inputJson = await Console.In.ReadToEndAsync();

RunnerRequest req;
try
{
    req = JsonSerializer.Deserialize<RunnerRequest>(inputJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
          ?? new RunnerRequest("", "Sum", new());
}
catch
{
    var bad = new RunnerResponse(0, 0, new(), "Invalid JSON input");
    Console.WriteLine(JsonSerializer.Serialize(bad));
    return;
}

// עוטפים למחלקה קבועה
var wrapped = $@"
using System;
using System.Linq;
public static class StudentSolution
{{
    {req.SourceCode}
}}";

var tree = CSharpSyntaxTree.ParseText(wrapped);

var refs = new List<MetadataReference>
{
    Ref<object>(),
    Ref<Console>(),
    Ref<Enumerable>(),
};

var compilation = CSharpCompilation.Create(
    "StudentSubmission",
    new[] { tree },
    refs,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

using var ms = new MemoryStream();
var emit = compilation.Emit(ms);

if (!emit.Success)
{
    var errors = string.Join("\n", emit.Diagnostics
        .Where(d => d.Severity == DiagnosticSeverity.Error)
        .Select(d => d.ToString()));

    var respFail = new RunnerResponse(
        0,
        req.Tests.Count,
        req.Tests.Select(t => new TestCaseResultDto(t.Input, t.Expected, "", false, "Compilation error")).ToList(),
        errors);

    Console.WriteLine(JsonSerializer.Serialize(respFail));
    return;
}

ms.Position = 0;
var asm = Assembly.Load(ms.ToArray());

var type = asm.GetType("StudentSolution");
var method = type?.GetMethod(req.MethodName, BindingFlags.Public | BindingFlags.Static);

if (method is null)
{
    var respFail = new RunnerResponse(
        0,
        req.Tests.Count,
        req.Tests.Select(t => new TestCaseResultDto(t.Input, t.Expected, "", false, $"Method '{req.MethodName}' not found")).ToList(),
        null);

    Console.WriteLine(JsonSerializer.Serialize(respFail));
    return;
}

int passed = 0;
var details = new List<TestCaseResultDto>();

foreach (var t in req.Tests)
{
    try
    {
        // input "1 2" -> שני int
        var parts = (t.Input ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var args = parts.Select(int.Parse).Cast<object>().ToArray();

        var result = method.Invoke(null, args);
        var actual = result?.ToString() ?? "";

        var ok = string.Equals(actual.Trim(), (t.Expected ?? "").Trim(), StringComparison.Ordinal);
        if (ok) passed++;

        details.Add(new TestCaseResultDto(t.Input, t.Expected, actual, ok, null));
    }
    catch (Exception ex)
    {
        details.Add(new TestCaseResultDto(t.Input, t.Expected, "", false, ex.InnerException?.Message ?? ex.Message));
    }
}

var resp = new RunnerResponse(passed, req.Tests.Count, details, null);
Console.WriteLine(JsonSerializer.Serialize(resp));
