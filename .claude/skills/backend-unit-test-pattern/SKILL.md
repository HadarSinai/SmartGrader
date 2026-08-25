---
name: backend-unit-test-pattern
description: "Use when writing or reviewing unit tests for the SmartGrader .NET backend: the SmartGrader.UnitTests xUnit project, FluentAssertions assertions, [Theory]/[InlineData] cases, characterization tests that lock existing behaviour, and the two entity-construction helpers (TestAssignment for the protected Assignment constructor, SubmissionBuilder for the PendingAi→ProcessingAi→Done state sequence). Covers naming (English method + Hebrew comment), the no-logic-in-tests rule, project references (Domain+Application only, Infrastructure only for Roslyn tests), and what is deliberately NOT tested (OpenAI, Judge0, SMTP, trivial validators). USE FOR: 'add a unit test', 'test ScoreCalculator/LessonScoreCalculator', 'create the test project', 'write a characterization test', 'the test cannot construct the entity', 'dotnet test'. NOT for authoring exercise test cases that grade student code (that is the TestCase entity / plan-testCaseAuthoring), and NOT for the production CQRS/repository patterns themselves (see the sibling backend-* skills)."
---

# SmartGrader Backend Unit Test Pattern

How unit tests are written in this repo: project layout, packages, naming, entity
construction, and the boundaries of what gets tested. Grounded in the real entity
code (`Assignment.cs`, `Submission.cs`, `ScoreCalculator.cs`).

## Why this exists

A bug in a grading engine is **silent**: no crash, no red screen — a student gets 73
instead of 91 and it flows to the report card. Two such bugs already happened
(documented in `ScoreCalculator.cs:53-54` and `:85-86`). Tests exist to make that
class of bug fail loudly.

## Project layout (fixed, do not reinvent)

```
server/Tests/SmartGrader.UnitTests/
├── SmartGrader.UnitTests.csproj    ← net8.0, added to server/SmartGrader.sln
├── Helpers/
│   ├── TestAssignment.cs           ← subclass reaching Assignment's protected ctor
│   └── SubmissionBuilder.cs        ← walks the legal state sequence to a graded Submission
├── Domain/…Tests.cs                ← ScoreCalculator, LessonScoreCalculator, entities
├── Analysis/…Tests.cs              ← Roslyn (the one Infrastructure exception)
├── Common/…Tests.cs                ← HebrewDateConverter, describers, token generators
└── Authorization/…Tests.cs         ← LessonAccess, StudentScope, TestVisibility
```

Packages: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`,
`FluentAssertions` (+ `NSubstitute` only once mocks are actually needed).
There is **no** `Directory.Packages.props` — pin versions in the `.csproj`,
consistent with the other projects.

**Project references: `Domain` and `Application` only.** Never `Api`. The single
exception is `Infrastructure`, referenced solely for `RoslynCodeAnalysisService`
tests — testing through a fake `ICodeAnalysisService` would test the fake.

## Conventions (decided, not open)

1. **Naming:** English method names in the repo's convention —
   `Total_IsFullScore_WhenAssignmentHasNoTests` — with a one-line Hebrew comment
   above each test stating the case in plain language:
   ```csharp
   // תרגיל בלי מבחנים → ציון מלא, לא אפס
   [Fact]
   public void Total_IsFullScore_WhenAssignmentHasNoTests() { … }
   ```
2. **No logic inside a test.** No `if`, no loops, no calculations in test data —
   literal values only. Multiple cases go through `[Theory]`/`[InlineData]`.
3. **One assertion subject per test.** Several `Should()` calls on the *same*
   result object are fine; asserting two unrelated behaviours is two tests.
   Body follows Arrange-Act-Assert, separated by blank lines (no comments needed).
4. **FluentAssertions, not Assert.** `result.Total.Should().Be(100)` — the failure
   message is the documentation.
5. **Characterization tests** where no comment states the intent: run the code,
   lock in what it does today, and mark it `// characterization — מתעד התנהגות קיימת, לא מאשר אותה`.
   Such a test claims "this won't change unnoticed", not "this is right".
6. **A test never edits production code.** If a test fails against existing code,
   that is a finding to raise (is the test wrong, or is this a real bug?) — never a
   licence to change the code so the test passes.
7. **Test through public members only.** Private methods are covered via the public
   API that calls them; never raise a member's visibility for a test.
8. **Every test is order-independent** — it builds its own inputs and shares no
   mutable state with other tests. No skipped/disabled tests left in the suite:
   a test is green, red-and-being-fixed, or deleted.

## Picking cases (what earns a test)

- **One test per distinct observable outcome** — each branch that produces a
  different return value, exception, or state change.
- **Skip duplicate scenarios** whose observable result is identical to an existing
  test (e.g. lists of 1 vs 2 vs 3 items, unless the code explicitly branches on count).
- **No speculative edge cases** (Unicode, giant payloads) unless a comment or bug
  documents them. Null inputs get a test only where the code declares it handles
  them (e.g. `ScoreCalculator`'s null-coalesced lists).
- The filter question is always: *if this breaks, will anyone notice before a
  student gets a wrong grade?*

## Mocks (Phase 5+ only, NSubstitute)

- **No mocks before they're needed.** The scoring engine, entities, Roslyn analysis
  and pure utilities all test with real objects — mocks enter only at authorization
  gates and handlers.
- **Prefer state over interaction.** Assert what the result *is*, not which methods
  were called; verify a call only when the call itself is the contract (e.g.
  `ILogWriter` received an error row on SMTP failure).
- **Mock only interfaces this repo owns** (`I…Repository`, `IEmailSender`,
  `ICodeRunnerService`). Never mock EF Core, ClosedXML, or other third-party types.

## The two entity-construction obstacles (and their fixed solutions)

**1. `Assignment` has only a protected constructor** (`Assignment.cs:77`) — by
design, entities are created through EF or factories. Do NOT add a public
constructor. The pattern is the `Helpers/TestAssignment.cs` subclass, which
reaches the protected ctor and sets the public-settable properties directly.

**The one reflection exception — storage plumbing only.** `Assignment.Id` has a
*private* setter (a subclass cannot set it), and `Submission.SubmittedAt` is
written from `DateTime.UtcNow` in the ctor with no injectable clock. Both are
storage plumbing, not domain invariants — EF itself sets them via reflection.
The helpers therefore set **exactly these** via one reflection line each,
documented in place. Domain state (Score, Status, IsBonus…) is NEVER set by
reflection — only through public setters or the real state machine. Do not widen
this exception.

**2. A graded `Submission` is reachable only through its state machine.** `Score`
has a private setter; `MarkDone` throws unless status is `ProcessingAi`
(`Submission.cs:384-388`). The only legal path is:

```csharp
var s = new Submission(studentId, assignmentId, sourceCode);  // → PendingAi
s.MarkProcessingAi();                                          // → ProcessingAi
s.MarkDone(breakdown, feedbackJson: null);                     // → Done, Score set
```

Encapsulate this once in `Helpers/SubmissionBuilder.cs` and reuse it — never
inline the sequence in individual tests.

`ScoreCalculator` inputs need neither helper: `TestCaseResult` is a record with a
public constructor and `StructuralRuleResult` has public setters — construct freely.

## Verification

From `server/`: `dotnet test`. Additional gates:

- **Suite stays under ~10 seconds.** A slow suite doesn't get run.
- **`dotnet build` on the solution still succeeds** — the test project must not
  break the existing build.
- **Deliberate-break check** for a new guard: temporarily break the production
  behaviour, confirm the test goes red with a readable message, revert. Proves the
  test detects the regression rather than passing for the wrong reason.
- **No flaky tests.** A test that passes and fails across identical runs is fixed
  or deleted immediately — an intermittent red trains everyone to ignore red.
- Never report a phase/PR as done if its tests do not pass.

## Deliberately NOT tested

| Excluded | Reason |
|---|---|
| OpenAI calls | Non-deterministic; only the graceful fallback when the model is down |
| Judge0 | Needs Docker; a test red for external reasons trains you to ignore red |
| SMTP | Only that `ILogWriter` recorded the send failure |
| Trivial AutoMapper profiles | Restating a mapping can't find a fault in it; computed `ForMember` fields are the exception |
| Trivial validators (`NotEmpty` on a name) | Only validators with a real rule (rubric total vs MaxScore, Hebrew date ranges) earn a test |
| Coverage % as a target | The criterion is "if this breaks, will anyone notice before a student gets a wrong grade?" |

## Common pitfalls

- **Adding a public constructor / `internal set` to an entity "for testability"** —
  never; use `TestAssignment` / `SubmissionBuilder` instead.
- **Reaching for reflection beyond `Id` / `SubmittedAt`** — the exception above is
  closed; anything else goes through the entity's real API or does not get tested.
- **Mocking `ICodeAnalysisService` to test Roslyn behaviour** — tests the fake;
  reference Infrastructure in that one test class instead.
- **Asserting on Hebrew message text in domain exceptions** — assert the exception
  *type* and the state change; wording changes must not break the suite (the one
  exception: `StructuralRuleDescriber` tests, whose subject IS the Hebrew wording).
- **`DateTime.UtcNow` inside assertions** — race-prone; assert with
  `BeCloseTo(…, 1.Seconds())` or inject the time.
