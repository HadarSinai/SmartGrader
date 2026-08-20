---
name: backend-judge0-mono-wrapper-pattern
description: "Use when touching the generated C# wrapper code that Judge0CodeRunner sends to Judge0 in the SmartGrader backend — BuildWrappedSource, BuildWrappedMultiFileSource, BuildArgs/BuildJsonArgs, MergeFiles, or the InvariantCulture preamble. Judge0 CE compiles with Mono (language_id 51), whose BCL is .NET Framework-era: modern C#/.NET that compiles fine in the solution fails inside the sandbox with CS0234/CS1525, and the error names a class the student never wrote. Covers what is NOT available under Mono, why every such failure looks like a student bug, and how to prove a wrapper change against real Judge0 before trusting it. USE FOR: 'the wrapper does not compile', 'CS0234 System.Text.Json', 'MultiFileMethod submissions all fail', 'add an argument type to the runner', 'change the generated Main'. NOT for the CQRS handler that calls ICodeRunnerService (see backend-mediatr-query-handler-pattern), and NOT for deciding what a failed run means for the grade (that is the scoring path)."
---

# Judge0 / Mono Wrapper Pattern

`Judge0CodeRunner` does not send the student's code to Judge0. It **generates a C# file** — the
student's (or teacher's) code merged into a wrapper with a `Main` that reads the test input, calls the
entry method, and prints the result. That generated file is what gets compiled.

The whole pattern exists because of one fact:

> **Judge0 CE `language_id 51` compiles with Mono, not with modern .NET.**
> Its BCL is .NET Framework-era. The solution builds with .NET 8, so *nothing in `dotnet build` will
> ever catch a Mono incompatibility* — the wrapper is a string, and the compiler that rejects it lives
> in a container somewhere else.

## Why this keeps costing real money

A wrapper that does not compile does not fail as "wrapper is broken". It fails as:

```
Main.cs(5,19): error CS0234: The type or namespace name `Json' does not exist
               in the namespace `System.Text'
```

`Main.cs` is a file nobody wrote. The submission comes back `CompilationFailed`, and the student — whose
code was perfect — is told her code does not compile. Two real bugs of exactly this family have already
shipped:

| Bug | What the generated code used | Mono's answer |
|---|---|---|
| `Console.ReadLine()!` (null-forgiving) | C# 8 syntax | `CS1525` |
| `JsonDocument.Parse` + `using System.Text.Json;` | .NET Core 3.0+ BCL | `CS0234` — **every `MultiFileMethod` submission failed** |

The second one sat undetected until the teacher-facing test-case verification ran the same path and
surfaced it. Grading has no such witness: a student who is told her correct code does not compile
usually assumes she is wrong.

## When to Use

- Editing `BuildWrappedSource`, `BuildWrappedMultiFileSource`, or the generated `Main` in
  [Judge0CodeRunner.cs](../../../server/Infrastructure/Services/CodeRunner/Judge0CodeRunner.cs).
- Adding or changing a supported parameter type in `BuildArgs` / `BuildJsonArgs`.
- Diagnosing a `CompilationFailed` whose error points at `Main.cs` or at a class the author never wrote
  (`StudentSolution`, `Program`, `SmartGraderJsonArgs`).
- Adding a new `GradingMode` — it needs a wrapper, and the wrapper needs this checklist.

## Not available under Mono — use these instead

| Do not emit | Emit instead |
|---|---|
| `System.Text.Json` (`JsonDocument`, `JsonElement`, `JsonSerializer`) | hand-rolled parsing — see `SmartGraderJsonArgs` in the runner |
| `!` null-forgiving (`Console.ReadLine()!`) | `(Console.ReadLine() ?? "")` |
| `Span<T>` / `Range` / index-from-end (`s[^1]`, `s[1..]`) | `Substring`, explicit indexes |
| target-typed `new()`, records, top-level statements | explicit types, plain classes, an explicit `Program.Main` |
| `string.Split(char)` single-char overload | `Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)` |

These constraints apply **only to the emitted string**. The C# around it — the runner class itself —
is normal .NET 8 and should stay that way.

## Workflow

1. **Locate the emitting member.** Every wrapper is a `$@"..."` string or a `const string`. Remember
   `{{` / `}}` escaping in interpolated strings and `""` for quotes in verbatim ones.
2. **Check the table above** before writing any API into the generated code. If unsure whether Mono has
   it, assume it does not.
3. **Keep the three invariants** every wrapper already honors:
   - `MergeFiles` lifts every `using` to the top (students write `using System;` out of habit; a
     `using` inside a class body is `CS1529`).
   - `InvariantCultureSetup` is emitted inside `Main` — without it `Console.WriteLine(3.14)` prints
     `3,14` on a comma-decimal locale and every decimal test fails for reasons unrelated to the code.
   - Names you introduce must not collide with student class names. Prefix them (`SmartGraderJsonArgs`).
4. **Match the mode's input contract** — this is what the teacher types into the "קלט" field:

   | GradingMode | Input format | Reference/student code shape |
   |---|---|---|
   | `FullProgram` | full stdin, one `Console.ReadLine()` per line | complete program **with** `Main` |
   | `Method` | args separated by spaces (`3 5`) | **bare method only**, no wrapping class — the body is pasted into `static class StudentSolution` |
   | `MultiFileMethod` | JSON array (`[3, 5]`, `["Dana", 3.5]`) | the classes themselves, no `Main`; entry method `static` |

5. **Prove it against real Judge0.** `dotnet build` proves nothing here. Run the teacher-facing
   verification endpoint, which compiles the wrapper for real:

   ```
   POST /api/lessons/{lessonId}/assignments/verify-tests
   { "gradingMode": "MultiFileMethod",
     "referenceSolution": [{ "fileName": "Calculator.cs", "content": "public class Calculator { public static int Add(int a, int b) { return a + b; } }" }],
     "expectedFiles":     [{ "fileName": "Calculator.cs", "methodName": "Add" }],
     "tests":             [{ "input": "[3, 5]", "expected": "8" }] }
   ```

   `hasCompileError: false` is the only evidence that counts. Include at least one **string** and one
   **decimal** argument — those are what break on quoting and on culture.
6. **Record Mono workarounds** in [.github/תיקונים.md](../../../.github/תיקונים.md) under the
   "דברים עתידיים להחזיר כשעוברים ל-.NET מודרני" section, so the ugliness is deliberate and reversible
   rather than mysterious.

## Pitfalls

- **Trusting `dotnet build`.** It compiles the runner, never the string the runner produces.
- **Testing only `FullProgram`.** It is the only mode whose wrapper barely touches the BCL, so it keeps
  working while the other two are broken. Every wrapper change needs all three modes exercised.
- **Testing only `int`.** Quoting bugs hide behind numbers; culture bugs hide behind integers. Use a
  string and a decimal.
- **Reporting a wrapper failure as a code failure.** If `Main.cs` appears in a compile error, it is our
  bug, never the student's. `VerifyTestCasesHandler` already models the right split — the teacher's own
  compile error is shown as *hers*, and infrastructure failure is shown as *ours*.
- **Naming a helper something a student might name a class.** `Utils`, `Helper`, `Json` will collide.

## See Also

- [backend-ai-verified-generation-pattern](../backend-ai-verified-generation-pattern/SKILL.md) — the
  verification endpoint that exercises this path is how the `System.Text.Json` bug was found; the same
  "run it, don't trust it" discipline applies to wrapper changes.
- [backend-mediatr-query-handler-pattern](../backend-mediatr-query-handler-pattern/SKILL.md) — the
  handlers that call `ICodeRunnerService`; they never know the wrapper exists.
