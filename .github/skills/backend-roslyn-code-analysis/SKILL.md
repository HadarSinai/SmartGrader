---
name: backend-roslyn-code-analysis
description: "Use when checking a structural requirement against student C# source with Roslyn in the SmartGrader backend: adding a CodeConstruct to the catalog, mapping a construct to DescendantNodes().OfType<T>(), detecting recursion, measuring loop-nesting depth, reporting line numbers, or tolerating unparseable code. Covers the extension recipe (one enum value + one case), the four traps that silently mis-grade (substring recursion, matrix collapsing into array, the blocking gate firing on a syntax error, and the absence of a semantic model), and the pinned package version. USE FOR: 'add a construct to the requirements catalog', 'detect recursion/nesting/matrix in code', 'the requirement passed but it should not have', 'the analyzer says 0 for valid code', 'check the code structure'. NOT for deciding what a failed requirement does to the grade (that is ScoreCalculator), and NOT for the Hebrew wording shown to the student (that is StructuralRuleDescriber, see backend-ai-feedback-prompt-pattern)."
---

# Backend Roslyn Code Analysis

The requirements engine answers *"did she solve it the way I asked?"* — and it must answer identically
every single time, because the answer carries points. That is the whole reason it is a syntax parser
and never a language model.

Reference implementation:
[`RoslynCodeAnalysisService`](../../../server/Infrastructure/Services/CodeAnalysis/RoslynCodeAnalysisService.cs),
behind [`ICodeAnalysisService`](../../../server/Application/Services/CodeAnalysis/ICodeAnalysisService.cs).

## When to Use

- Adding a construct to the catalog because a new topic is being taught (`Queue`, `Stack`, `Lambda`…).
- A requirement passes when it should fail, or fails on code that clearly satisfies it.
- Reporting *where* in the code something was found, for the feedback panel.
- Reviewing a PR that touches `Measure`, `CodeConstruct`, or `StructuralRule.IsSatisfiedBy`.

## The extension recipe

**Adding a construct = one enum value + one `case`.** Nothing else. A different construct is taught
every week, and that openness is the actual requirement.

1. Add the value to [`CodeConstruct`](../../../server/Domain/Entities/CodeConstruct.cs), inside its
   teaching-order group. **Give it an explicit number** and never renumber existing values — the enum
   is serialized into `Assignment.StructuralRulesJson`, so a shifted number silently changes the
   meaning of every assignment already in the database. (`[JsonStringEnumConverter]` stores names, but
   the explicit numbers document that the ordering is not the contract.)
2. Add one `case` to `Measure`, returning `Found(...)`.
3. Add the Hebrew name to `StructuralRuleDescriber.ConstructName`, and its grammatical gender to
   `IsFeminine` if it is feminine — "לא נמצא רקורסיה" is a mistake the student sees.
4. Add the label to the client catalog too: `CODE_CONSTRUCT_LABELS_HE` **and**
   `CODE_CONSTRUCT_GROUPS` in [`assignment.model.ts`](../../../client/src/app/models/assignment.model.ts).
   A construct missing from `CODE_CONSTRUCT_GROUPS` is unreachable in the teacher's dropdown.
5. Write the syntax that *nearly* matches and confirm it does not count.

```csharp
case CodeConstruct.TryCatch:
    return Found(root.DescendantNodes().OfType<TryStatementSyntax>());
```

## The four traps

These four cost points if you get them wrong, and none of them announce themselves.

### 🔴 1. Recursion compares identifiers, never substrings

The obvious implementation is wrong:

```csharp
call.Expression.ToString().Contains(method.Identifier.Text)   // ❌
```

`"SumDigits"` contains `"Sum"`, so a method that merely calls a differently-named helper is reported
as recursive — and the student is awarded a requirement she did not meet. Resolve the invoked name
exactly (`InvokedName`) and compare with `StringComparison.Ordinal`.

Note it also walks `LocalFunctionStatementSyntax`, not only `MethodDeclarationSyntax`: the `Method`
grading mode submits a bare method, which Roslyn parses as a local function inside top-level
statements. Without it, "must use recursion" fails on every submission in that mode.

### 🔴 2. A syntax error must NOT fire the blocking gate

Code with a missing semicolon parses into a tree that contains no recursion — so a blocking
`MustUse Recursion` rule would reject it with *"the assignment required recursion"* while the real
problem is a typo. `CodeAnalysisResult.HasSyntaxErrors` exists exactly to prevent this: when it is
true the submission continues to Judge0 and the student gets the actual compiler error.

```csharp
var hasSyntaxErrors = tree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);
```

The same flag is set by the `catch` and by empty source, so a parse failure can never be mistaken for
an honest count of zero.

### 🔴 3. `Matrix` must not collapse into `Array`

`int[,]` is one rank specifier of rank 2; `int[][]` is two specifiers of rank 1 each and is *not* a
matrix. `Array` explicitly excludes matrices and vice versa — otherwise "must use a 2-D array" passes
on `int[]`, which is the entire point of the requirement.

Also note `ArrayTypes` filters out the redundant type in `int[] a = new int[5];`, where the same
array appears twice in the tree and would count as two against "at most one array".

### 🔴 4. There is no semantic model

`CSharpSyntaxTree.ParseText` gives syntax only — no compilation, no type resolution. The consequences
are real and must be documented next to each `case`:

| Code | Behavior | Why |
|---|---|---|
| `var isSorted = true;` | **not** a bool variable | the declared type is `var`; nothing resolves it |
| `class A : IComparable` | satisfies `MustUse Inheritance` | a base list and an interface list are the same node |
| `.Where().Select()` chains | **not** counted as `Linq` | it looks like any other method call; only query syntax and `using System.Linq` count |
| `if / else if / else` | counts as **2** ifs | `else if` is a nested `IfStatementSyntax` — and this is the correct count for "at most 3 if" |

Do not paper over these with heuristics. Document them, and let the teacher phrase the requirement
explicitly (the form already tells her to).

## Rules that are decided in one place only

`Measure` counts. It never decides. The verdict lives in
[`StructuralRule.IsSatisfiedBy`](../../../server/Domain/Entities/StructuralRule.cs) and both the
analyzer and the tests go through it — a second interpretation of the same rule is exactly what makes
a grade change between code paths.

`NestedLoopDepth` is the one construct whose value is a **depth, not a count**: "at most nesting 2"
passes on three sibling loops and fails on two nested ones. `MeasureLoopDepth` also returns the
*deepest* loops as its nodes, so the feedback points at the inner loop rather than the outer one.

## Line numbers

```csharp
node.GetLocation().GetLineSpan().StartLinePosition.Line + 1   // Roslyn is 0-based
```

Capped at `MaxReportedLines` (10) — feedback says "in line 12", not a list of forty. In multi-file
submissions the source is the **concatenated** files, so line numbers refer to the joined text and not
to any single file; say so wherever they are surfaced.

## Never throws

`Analyze` returns a result in every path, including the `catch`. An analyzer failure must never take
down the grading of a submission: the submission proceeds and Judge0 reports what actually happened.
Registered `Scoped` in `Infrastructure/DependencyInjection.cs`.

## Package version — pinned deliberately

```xml
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.5.0" />
```

**Do not upgrade this casually.** `Microsoft.EntityFrameworkCore.Design` pins
`Microsoft.CodeAnalysis.Common` to exactly 4.5.0; any other version fails the restore with `NU1107`.

## Pitfalls

- Substring matching for recursion — awards points for a requirement that was not met.
- Letting the blocking gate fire when `HasSyntaxErrors` is true — the student is told the wrong thing.
- Renumbering `CodeConstruct` values, silently rewriting the meaning of stored assignments.
- Counting `int[]` as a matrix, or counting the same array twice from its declaration and its `new`.
- Re-implementing `IsSatisfiedBy` inside the analyzer or a test.
- Adding a construct on the server and forgetting `CODE_CONSTRUCT_GROUPS` on the client, so no teacher
  can ever select it.
- Reaching for a semantic model to fix `var` — it means compiling student code in-process, which is
  the thing the Judge0 sandbox exists to avoid.

## See Also

- [backend-ai-feedback-prompt-pattern](../backend-ai-feedback-prompt-pattern/SKILL.md) — how these
  findings become Hebrew, and the shared `StructuralRuleDescriber` wording.
- [backend-judge0-mono-wrapper-pattern](../backend-judge0-mono-wrapper-pattern/SKILL.md) — Roslyn
  parses newer C# than Mono compiles, so a requirement can pass and compilation still fail.
- [backend-role-based-field-redaction](../backend-role-based-field-redaction/SKILL.md) — why rule
  results, unlike test cases, are shown to the student in full.
