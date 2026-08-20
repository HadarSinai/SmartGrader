---
name: backend-ai-feedback-prompt-pattern
description: "Use when changing what SmartGrader sends to OpenAI for student feedback, or what it expects back: the three scenario prompts (compile error / requirement unmet / normal grading), the shared system preamble, max_tokens, response_format json_object, temperature, source truncation, or the deterministic fallback when the model is unavailable. Covers why the model returns no numbers, why hidden test values are withheld from the prompt itself, why the feminine Hebrew instruction is explicit, and the failure mode where a JSON template's literal zeros were copied into every student's scores. USE FOR: 'change the AI feedback prompt', 'the feedback leaked the expected output', 'the AI invented a grade', 'the feedback is in masculine Hebrew', 'reduce the OpenAI cost', 'what happens when OpenAI is down'. NOT for AI that proposes content a deterministic check then verifies (that is backend-ai-verified-generation-pattern), and NOT for the syntax analysis that produces the findings (that is backend-roslyn-code-analysis)."
---

# Backend AI Feedback Prompt Pattern

Every fact is decided before the model is called. Roslyn decides whether a requirement was met, the
runner decides whether the output matched, `ScoreCalculator` decides the number. **The model
contributes only the Hebrew explanation** — which is precisely why the grade stays reproducible while
the wording may vary between runs.

Without it a 9th-grader sees `❌ error CS0103: The name 'sum' does not exist`. With it she sees
*"בשורה 7 השתמשת במשתנה `sum` שלא הגדרת; נראה שהתכוונת ל-`total` משורה 4"* — the difference between a
stuck student and one who can continue.

Reference implementation:
[`OpenAiFeedbackService`](../../../server/Infrastructure/Services/Feedback/OpenAiFeedbackService.cs).

## When to Use

- Changing the wording, contents, or cost controls of any feedback prompt.
- Adding a fourth scenario, or adding a field to the returned JSON.
- The feedback quoted a value the student was not supposed to see.
- The feedback addressed the student in masculine Hebrew.
- Deciding what the student sees when OpenAI is unreachable.

## One preamble, three scenarios

A single prompt carrying rules that do not apply is waste and noise: a compile-error case needs no
test instructions and no rubric. The system preamble stays short and shared; each scenario adds only
its own block.

| Scenario | Contents | Size |
|---|---|---|
| `GetCompileErrorFeedbackAsync` | compiler message + code. No tests, no rules | ~120 tok |
| `GetRequirementFeedbackAsync` | the rule + the finding + code. **No test data at all** — Judge0 never ran | ~140 tok |
| `GetGradingFeedbackAsync` | pass count + **sample tests only** + rule results + code | ~180 tok |

All three go through one `SendAsync`, so retry policy, JSON mode, and fallback behavior cannot drift
apart between them.

## 🔴 The model returns no numbers

The preamble says it twice, in two different ways:

```
Never state or guess a grade, a score or a number of points.
```

`AiFeedbackResult` has no `scores` field to deserialize into, so even a model that ignores the
instruction cannot get a number into the response. The deterministic breakdown reaches the client
separately as `SubmissionResponseDto.ScoreBreakdown`.

**The failure mode that motivated this:** the previous prompt embedded a JSON *example* containing
literal `0` values. The model copied them verbatim, and every student received a feedback panel of
zeros next to a real grade. Hence the current template labels its values as types, explicitly:

```
Return strict JSON only, in this shape (the values are TYPE placeholders, not defaults):
{ "good":[<string>], "issues":{...}, "minimal_changes":[<string>] }
```

Never put a plausible-looking value in a template. Put a type.

## 🔴 Never send data the student must not see

If the model receives `expected: 10` it will write *"החזרת 45 במקום 10"* — and every bit of the
hidden-test work is undone. The prompt is a leak path, exactly like a DTO.

- **Sample tests** are sent in full and the model may quote them: the student can already see them.
- **Hidden tests** are sent as a *count of failures only*, with an explicit instruction to describe
  the **kind** of case that may have been missed (zero, negatives, empty input) and never to state,
  guess, or hint at a value.
- The **reference solution is never sent at all** in the feedback path.

This costs fewer tokens than sending everything, and locates the bug in nearly every case anyway.

## 🔴 Feminine Hebrew is a hard requirement, not a preference

A girls' school. The default habit of an LLM writing Hebrew is masculine, so the instruction is
explicit (`את, שלך, כתבת, נסי`) — and **verification means actually reading the generated text**, not
confirming the instruction is present in the prompt. The same rule governs every UI string written
alongside these prompts.

## Tell it which compiler actually ran

Roslyn parses newer C# than Judge0 executes (Mono, `language_id 51`). A student can write a
`switch` expression, satisfy `MustUse Switch`, and still fail to compile — then protest, correctly,
*"אבל השתמשתי ב-switch!"*. The compile-error prompt therefore names the runtime and instructs the
model to call out unsupported modern syntax **by name** and offer the older equivalent.

## Cost controls

| Control | Value | Why |
|---|---|---|
| `max_tokens` | 600 | the previous prompt ran with no cap at all |
| `response_format` | `{ type: "json_object" }` | less preamble before the object, fewer parse failures |
| `temperature` | 0.2 | steadies wording between runs of the same code, and reduces invented findings |
| source truncation | 4000 chars | without it a whole pasted file is re-sent on every attempt |
| sample tests in prompt | 4 | |

The deleted `optional_full_solution` field was both the most expensive output and a handout of the
answer. Do not reintroduce a field that returns a complete solution — `minimal_changes` is
deliberately *the smallest concrete edits*, never the whole thing.

## Degrade to the deterministic finding

Every call passes a `fallback` and **never throws**. The facts were already established by Roslyn and
the runner; an OpenAI outage must not leave the student with a bare status and no explanation, and it
must never discard a score that is already computed.

```csharp
fallback: () => AiFeedbackResult.Deterministic(findings)
```

The fallback text comes from the **same**
[`StructuralRuleDescriber`](../../../server/Application/Services/CodeAnalysis/StructuralRuleDescriber.cs)
that phrases the findings inside the prompt. One source, two consumers — if each wrote its own
phrasing, an OpenAI failure would change not just the quality of the explanation but the claim about
what was checked.

Failure handling, in order:

1. No API key or model configured → fallback immediately, no HTTP call.
2. `429` / `503` → up to 3 attempts, honoring `Retry-After`, then backoff 2s/4s/8s capped at 10s.
3. Any other non-success → fallback.
4. Success but unparseable JSON → return the raw text with `ParseSucceeded = false`; the client
   renders it as-is. Hebrew prose beats a dry finding when it did arrive.

## Pitfalls

- A JSON template containing example *values* — the model copies them.
- Adding a numeric field to `AiFeedbackResult` "just for display". Every number must be reproducible.
- Passing hidden tests' `Input`/`Expected` into the prompt "so the feedback is more specific".
- Adding a scenario with its own ad-hoc `HttpClient` call instead of going through `SendAsync`.
- Throwing when OpenAI fails — it discards a grade that deterministic code already computed.
- Assuming the feminine instruction worked because it is written in the preamble; read the output.
- Describing the compiler as "C#" in the compile-error prompt rather than as Mono.

## See Also

- [backend-roslyn-code-analysis](../backend-roslyn-code-analysis/SKILL.md) — where the findings and
  their Hebrew phrasing come from.
- [backend-ai-verified-generation-pattern](../backend-ai-verified-generation-pattern/SKILL.md) — the
  one place a model output *may* become a value, because execution verifies it first.
- [backend-judge0-mono-wrapper-pattern](../backend-judge0-mono-wrapper-pattern/SKILL.md) — what Mono
  actually rejects.
- [backend-role-based-field-redaction](../backend-role-based-field-redaction/SKILL.md) — the prompt as
  a leak path alongside DTOs and notification feeds.
