---
name: backend-ai-verified-generation-pattern
description: "Use when an AI model generates content that a deterministic check can verify before anyone relies on it — proposing test cases run against a reference solution, or any 'model suggests, execution decides, human confirms' flow in SmartGrader. Covers why this is safe here while AI is kept away from grades, making the executed result win on disagreement and showing the disagreement instead of hiding it, and degrading gracefully so the manual path never depends on the model. USE FOR: 'let AI suggest X', 'generate test cases', 'is it safe to use AI for this', 'verify the model output before saving', 'the AI got the number wrong'. NOT for the grading path itself, where the model must not produce numbers at all (see the contrast section), and NOT for role-based redaction of a response (that is backend-role-based-field-redaction)."
---

# Backend AI-Verified Generation Pattern

SmartGrader deliberately keeps AI away from every number that becomes a grade. Test-case generation
is the one place the rule inverts — and the reason is not "this feature matters less". It is that
**the model's output can be independently checked before anyone acts on it.**

That single question decides whether a model may produce a value in this codebase:

> Can this output be verified by a deterministic process *before it is used*?

| | Grading (`OpenAiFeedbackService`) | Generation (`OpenAiTestCaseSuggestionService`) |
|---|---|---|
| Who consumes it | the student, as a grade | the teacher, as a draft |
| Is it verified | it *is* the verdict | executed against a reference solution |
| Cost of a mistake | an unfair grade nobody can explain | a suggestion she edits or deletes |
| Reproducibility | required | not needed — she reviews it once |

## When to Use

- Adding an "AI suggests / propose / draft this for me" action anywhere a teacher reviews the output.
- Reviewing whether a proposed AI feature is safe, or whether it quietly hands the model authority.
- Deciding what to store when a model and a deterministic check disagree.
- Adding a second consumer of `ICodeRunnerService` outside the grading pipeline.

## The Shape

```
1. Model proposes candidates          ← ideas, never facts
2. 🔴 Every candidate is EXECUTED     ← this step is the whole safety argument
3. Executed result wins on conflict   ← and the conflict is SHOWN
4. Human confirms before anything is stored
```

Reference implementation: [`SuggestTestCasesHandler`](../../../server/Application/UseCases/Assignments/SuggestTestCases/SuggestTestCasesHandler.cs).

## Workflow

1. **Split the model behind its own interface**, next to but separate from the existing one
   ([`ITestCaseSuggestionService`](../../../server/Application/Services/Feedback/ITestCaseSuggestionService.cs)
   alongside `IFeedbackService`). Separate interfaces mean separate timeouts, prompts, and failure
   policies — the background grading call retries with backoff, the teacher-facing call fails fast
   because she is staring at the screen.

2. **The service returns candidates and nothing more.** No `Verified` flag, no scores, no decisions.
   `SuggestedTestCase(Input, Expected, Why, IsCore)` — the handler decides what is true.

3. **Execute every candidate through the same path the real thing uses.** Not a similar path — the
   same one. This is why [`GradingModeRunner`](../../../server/Application/Services/CodeRunner/GradingModeRunner.cs)
   was extracted out of `AiWorker`: verification that runs a different dispatch than grading gives
   the teacher confidence about a code path her students will never hit. **Verification on the wrong
   path is worse than no verification**, because it converts uncertainty into false certainty.

4. **On disagreement the executed result wins, and the disagreement is surfaced.** Both values reach
   the DTO:

   ```csharp
   Expected = ran ? detail!.Actual : proposal.Expected,   // what gets saved
   AiExpected = proposal.Expected,                        // what the model claimed
   Disagreed = ran && !detail!.Passed,
   ```

   Silently overwriting would be *safe* but not *legible* — the visible "ה-AI הציע 0, הפתרון שלך
   החזיר 5" row is the evidence that verification actually ran.

5. **Fail closed when the execution did not really happen.** A runtime error or timeout returns empty
   stdout. Treating that as "the verified answer is empty string" turns a good test case into one
   that expects nothing — approved in a single click. Both the suggestion path and the verify path
   guard it with the same condition:

   ```csharp
   var ran = detail is not null
             && detail.Error is null
             && !string.IsNullOrWhiteSpace(detail.Actual);
   ```

6. **Degrade gracefully in three separate directions.** These are distinct failures and the user must
   be able to tell them apart:

   | What broke | Behavior |
   |---|---|
   | No API key / model unreachable | `TestCaseSuggestionUnavailableException` → `BusinessRuleException` → 400 with a Hebrew message. Manual authoring and verification are untouched. |
   | Runner down *after* suggestions arrived | Return the suggestions marked **unverified**. Throwing here discards work that was already paid for. |
   | No reference solution at all | Generation still works; every row is marked "לא אומת" and the warning is prominent. |

   The rule underneath: **the manual path must never depend on the model being reachable.**

7. **Nothing is persisted until the human confirms.** The command returns proposals; the rows enter
   the entity only through the normal save. Verification results are transient by design — they
   describe a draft, not a graded artifact, so there is no table for them.

8. **Guard the endpoint like the paid resource it is**: `[Authorize(Roles = "Teacher,Admin")]` **plus**
   an ownership check (`LessonAccess`), a rate-limit policy, and a cap on the requested count enforced
   server-side — the model ignores `n` sometimes, and that number becomes Judge0 runs.

## Contrast — where this pattern must NOT be applied

`AiWorker` computes the score from runner results, never from `scores.final_score` in the model's
JSON, even though the model returns one. There is no deterministic check that can validate a grade
before a student receives it: the grade *is* the judgment. Any argument of the form "the model is
usually right about this" is the argument this pattern exists to reject — the question is never
accuracy, it is **verifiability before use**.

## Pitfalls

- Trusting the model's arithmetic because it "looked right in testing" — it is never executed at
  authoring time by the same input the class will hit.
- Verifying through a path that differs from the real one (a different `GradingMode` dispatch, a
  different normalization) — false confidence.
- Hiding the disagreement and silently storing the executed value: safe, but the teacher can no
  longer distinguish "the model was wrong" from "my own reference solution is wrong".
- Writing empty stdout into an expected-output field after a crash or timeout.
- Letting an AI outage break manual authoring.
- Capping the count only in the prompt and not in code.

## See Also

- [backend-mediatr-query-handler-pattern](../backend-mediatr-query-handler-pattern/SKILL.md) — the handler shell.
- [backend-role-based-field-redaction](../backend-role-based-field-redaction/SKILL.md) — why the reference solution never reaches a student, and why the AI prompt is itself a leak path.
- [backend-controller-endpoint-pattern](../backend-controller-endpoint-pattern/SKILL.md) — the endpoint and its `[Authorize]`/rate-limit attributes.
