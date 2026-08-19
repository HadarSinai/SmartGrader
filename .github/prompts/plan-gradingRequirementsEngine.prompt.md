# Plan: Assignment Requirements & Deterministic Grading Engine (Prompt 2 of 2)

## TL;DR

The grade is one line today — `passed / total * 100` — so the system only asks *"is the output correct?"* and
never *"did she solve it the way I asked?"* A teacher who teaches `while` this week and recursion next has no way
to express that. This prompt adds **structural requirements checked by Roslyn** (the C# syntax parser, not an
LLM, so the same code always earns the same grade), a **100-point rubric** the teacher allocates like an exam,
a **retry rule** driven by the score alone, a **teacher override**, and a **rewrite of the OpenAI prompt** that
cuts roughly half the input tokens while making it impossible for the model to leak hidden test answers.

**Requires [plan-gradingSecurityHardening](plan-gradingSecurityHardening.prompt.md)** — it establishes
`TestCase.IsSample` and the one-submission-per-assignment rule that this prompt builds on.

## User decisions (already confirmed)

The teacher's framing, which drives the whole design:

> If the assignment required recursion and she used loops — **it counts as not having done it at all.** No grade.
> Submit again.

**That is a rejection, not a lower grade.**

| Decision | Choice |
|---|---|
| Grade source | Fully automatic. **No** teacher approval step in the normal flow |
| How requirements are checked | **Roslyn**, never an LLM — the grade must be reproducible |
| Requirement severities | 🔴 **Blocking** (no grade) · 🟡 **Scored** (costs points) · ⚪ **Advisory** (comment only) |
| Grade composition | **Points summing to 100**, like an exam rubric. Abstract weights were rejected as unclear |
| Retry rule | Score **< 85** → resubmit freely until she clears 85. **No attempt cap** |
| Teacher override | **A button in the teacher UI, not a shared code.** Overrides everything. Audited |
| AI role | Hebrew explanatory text only. **Returns no numbers** |
| Hebrew grammatical gender | **Feminine throughout** (`את`, `כתבת`, `נסי`) — a girls' school. Applies to AI output and all UI copy |

### Why the 85 threshold does not enable guessing

The worry was *"she reads the feedback, patches it, gets 100."* Two mechanisms prevent it:

1. **Tests are hidden** (Prompt 1) — feedback describes the *kind* of failure, never the expected value
2. **The AI never receives hidden expected values** (Step 6 below) — it physically cannot leak them

To improve, the student has to actually understand. That is the point.

---

## ⚠️ MANDATORY — load the relevant skill before writing code

| Work | Skill |
|---|---|
| New Command/Query + Handler + FluentValidation | `backend-mediatr-query-handler-pattern` |
| New repository lookup / EF query | `backend-repository-query-pattern` |
| New controller action / route | `backend-controller-endpoint-pattern` |
| Entity ↔ DTO mapping | `backend-automapper-profile-pattern` |
| Anything under `client/src/app/pages/my/` | `client-student-area-pattern` |
| Copy, inline validation, confirm dialogs | `client-flow-fix-implementation-pattern` |
| List/table pages | `client-list-table-pattern` |
| Styling / tokens on a touched page | `client-design-token-rollout-pattern` |
| A custom `ControlValueAccessor` form control | `client-cva-form-control-pattern` |
| Writing the new skill in Step 8 | `create-skill` |

Also obey [server/CLAUDE.md](../../server/CLAUDE.md) and [client/CLAUDE.md](../../client/CLAUDE.md).

---

## What an assignment becomes

Five questions the teacher answers. The last two are new.

```
1. What is the task?          →  title + description
2. How does she submit?       →  grading mode
3. How do I know it is right? →  test cases  (+ IsSample from Prompt 1)
4. What must the solution use? →  requirements     🆕
5. How are points divided?     →  100-point rubric 🆕
```

---

## Step 1 — Requirement model

**Skill:** `backend-mediatr-query-handler-pattern`

New files under `server/Domain/Entities/`: `StructuralRule.cs`, `RuleKind.cs`, `RuleSeverity.cs`,
`CodeConstruct.cs`, `StructuralRuleResult.cs`.

**`StructuralRule`:**

| Field | Type | Example |
|---|---|---|
| `Kind` | enum | `MustUse` · `MustNotUse` · `AtLeast` · `AtMost` |
| `Construct` | enum | `Switch`, `While`, `Recursion`, `NestedLoopDepth`… |
| `Threshold` | int | for `AtLeast`/`AtMost` |
| `Severity` | enum | `Blocking` · `Scored` · `Advisory` |
| `Points` | int | `Scored` only |

| Severity | When unmet |
|---|---|
| 🔴 `Blocking` | **No grade at all** · resubmit. Carries no points — it is a gate |
| 🟡 `Scored` | Loses its points |
| ⚪ `Advisory` | Comment in the feedback · zero grade impact |

**`CodeConstruct` catalog — open, ordered by teaching sequence:**

| Group | Values | Roslyn node |
|---|---|---|
| Conditionals | `If`, `Switch`, `Ternary` | `IfStatementSyntax`, `SwitchStatementSyntax` + `SwitchExpressionSyntax`, `ConditionalExpressionSyntax` |
| Loops | `For`, `While`, `DoWhile`, `Foreach`, `AnyLoop` | `ForStatementSyntax`, `WhileStatementSyntax`, `DoStatementSyntax`, `ForEachStatementSyntax` |
| Methods | `Method`, `Recursion` | `MethodDeclarationSyntax`; recursion = a method whose body invokes its own identifier |
| Collections | `Array`, **`Matrix`**, `List`, `Dictionary` | `ArrayTypeSyntax` — **`Matrix` is a rank > 1 specifier (`int[,]`), which is a different node shape from `int[]` and must not collapse into `Array`**; `GenericNameSyntax` by name |
| **Variable types** | `BoolVariable`, `StringVariable`, `CharVariable`, `LocalVariable`, `Constant` | `VariableDeclarationSyntax` inspected by declared type; `LocalDeclarationStatementSyntax`; `const` modifier |
| **OOP** | `Class`, `Property`, `Constructor`, `Field`, `Inheritance`, `Interface` | `ClassDeclarationSyntax`, `PropertyDeclarationSyntax`, `ConstructorDeclarationSyntax`, `FieldDeclarationSyntax`, `BaseListSyntax`, `InterfaceDeclarationSyntax` |
| Advanced | `TryCatch`, `Linq` | `TryStatementSyntax`, `QueryExpressionSyntax` + `using System.Linq` |
| Flow | `Break`, `Continue`, `Goto` | matching nodes |
| Efficiency | `NestedLoopDepth` | max nesting depth of loop nodes |

The OOP group exists because **a classes exercise has no input and no output at all** — see "Assignments without
I/O" below. Without `Property`/`Constructor`/`Inheritance` such an assignment cannot be expressed.

**Adding a construct = one enum value + one `case` in the analyzer.** That openness is the requirement — a
different construct is taught every week.

`StructuralRuleResult` carries the rule, `Passed`, the actual count, and **line numbers**
(`GetLocation().GetLineSpan().StartLinePosition.Line + 1`) so feedback can say "line 12".

### Entity changes

- **`Assignment`** — `StructuralRulesJson` following the existing `TestsJson` pattern exactly (`[NotMapped]`
  property with a private setter that serializes, plus `SetStructuralRules`), `TestsAllocation` (int),
  `RetryThreshold` (int, default 85)
- **`Submission`** — `StructuralResultsJson` in the same pattern, new `SubmissionStatus.RequirementsNotMet`,
  `MarkRequirementsNotMet(...)`, and that status added to the allowed sources in `MarkPendingAi`
  (`Submission.cs:123-127`)

Migration: `dotnet ef migrations add AddStructuralRules`.

**No attempt counter.** Retry eligibility derives from the score alone.

---

## Step 2 — Grade composition: a 100-point rubric

Teachers already think in points, so the form mirrors an exam rubric rather than abstract percentages:

```
Out of 100 points:
   Tests ............  80
   At most 3 `if` ...  20
   ═══════════════════════
   Total ............ 100  ✓
```

- The form validates the sum is **exactly 100**
- **No scored rules? Tests receive all 100 automatically**
- `Blocking` and `Advisory` rules carry no points
- **Efficiency is not a separate component** — it is a scored rule using `NestedLoopDepth`

### 🔴 Core tests gate; edge tests are proportional

Two designs were considered and **both rejected**:

**Pure proportional** (today's behaviour) rewards luck. A real submission that never reads input at all —

```csharp
long number = 987654321;
```

— passes **2 of 5** against `[987654321→45, 0→0, 1234→10, 55→10, 7→7]` purely by coincidence, earning
`2/5 × 80 = 32` points for doing nothing. The same happens whenever a student handles only positive numbers, or
always returns `0`.

**All-or-nothing** overcorrects: a student who solved the exercise correctly but forgot `n = 0` loses every test
point. Unlimited retries do not make that fair — she solved the problem.

**The model: `TestCase.IsCore` (bool, default `true`).** The teacher unticks the minority of cases that are edge
cases; most tests are core by nature.

```csharp
bool allCorePassed = tests.Where(t => t.IsCore).All(t => t.Passed);

testPoints = allCorePassed
    ? assignment.TestsAllocation * (passedCount / (double)totalCount)   // proportional over ALL tests
    : 0;                                                               // core failed → solved nothing

rulePoints = scoredRules.Where(r => r.Passed).Sum(r => r.Points);
finalScore = Math.Round(testPoints + rulePoints, 1);
```

Both cases now land correctly, with tests allocated 80:

| Submission | Core | Edge | Test points |
|---|---|---|---|
| Correct, forgot `n = 0` | ✅ 2/2 | ❌ 1/2 | `80 × 3/4` = **60** |
| Hardcoded `987654321` | ❌ 0/2 | — | **0** |

An assignment with **no** core tests marked (all unticked) has no gate — the score is purely proportional, which
is a legitimate choice; the form should point it out rather than prevent it.

### Test count determines grade granularity — warn about it

The proportion is taken over **all** tests, core and edge together, so each test is worth
`TestsAllocation / testCount`. With few tests the grade becomes very coarse:

| Tests | Each worth (of 80) | Possible test scores |
|---|---|---|
| **2** | 40 | 0 · 40 · 80 |
| 4 | 20 | 0 · 20 · 40 · 60 · 80 |
| 5 | 16 | 0 · 16 · 32 · 48 · 64 · 80 |
| 8 | 10 | steps of 10 |

At two tests, one forgotten edge case costs **half the test points** — which reintroduces exactly the harshness
the core/edge split was designed to remove. **Show a soft warning in the assignment form below four tests**:
*"עם 2 מקרי בדיקה כל אחד שווה 40 נקודות — מומלץ 4-6."* Warn, do not block.

### When only one test is possible, shift weight to requirements

Many beginner exercises admit exactly **one** correct output and take no input at all:

> *"מצאי שורשים שלמים לכל המספרים עד 30, כאשר 30 שמור כמשתנה מקומי"*

There is nothing to vary — one test, and test scoring is unavoidably binary. The rubric is deliberately flexible
so the teacher can rebalance, which is exactly what she does when marking such an exercise by hand: she reads the
code more than the output.

```
Tests ....................  50    ← binary, no way around it
30 as a local variable ...  30
At most 2 `if` ...........  20
═══════════════════════════════
                           100
```

A student whose output is slightly wrong but whose code uses a local variable and a clean loop scores **50**
rather than 0 — below the threshold, so she resubmits with something to build on.

The guidance to surface in the form:

| Test cases available | Suggested split |
|---|---|
| Many | tests 80 · requirements 20 |
| **Exactly one** | tests 50 · requirements 50 |
| None (classes exercise) | requirements 100 |

Note what this example demonstrates: *"30 as a local variable"* is the actual teaching point, and it is
**checkable and scorable** rather than an unenforced sentence in the description. When tests cannot carry the
nuance, requirements can.

### 🔴 The single-test shape is the common case here, not the exception

The teacher confirmed that most exercises in this course hardcode their data:

```csharp
int[,] matrix = { {1,2,3}, {4,5,6}, {7,8,9} };   // data lives in the student's code
// print the largest value in each row
```

No input, one possible output — therefore **one test**, always binary. Design accordingly:

- **Requirements are the primary grading mechanism in this course, not a supplement.** They carry most of the
  points in most assignments. The requirements section must be at least as prominent and as easy to fill as the
  test section — treating it as an "advanced, collapsed" area would hide the main tool
- The rubric's default split should follow the assignment's actual shape rather than a fixed 80/20 — see the
  table above
- Typical requirement sets for this shape: `Matrix` 🔴 + `AtLeast NestedLoopDepth 2` 🟡 for a matrix exercise;
  `BoolVariable` 🔴 for the flag-variable pattern (*"בדקי אם המערך ממוין — השתמשי במשתנה בוליאני"*);
  `StringVariable` + `MustNotUse Linq` for string exercises

**An option worth surfacing to the teacher, not forcing:** an exercise restructured as a method —
*"write `FindMax(int[,] matrix)`"* under `MultiFileMethod` — accepts many different matrices as JSON inputs and
regains full test granularity. That is a pedagogical choice about teaching methods versus straight-line code, so
mention it in the form as a hint when an assignment ends up with a single test; never impose it.

### Scored requirements are binary, tests are proportional

The two halves of the rubric behave differently **on purpose**:

```
Tests ...........  80   → proportional: how many cases worked
At most 3 `if` ..  20   → binary: the condition holds or it does not
```

A test is a **measurement**; a requirement is a **condition**. There is no partial credit for "at most 3 `if`"
when the student wrote four.

Worked example — 5 tests (2 core, 3 edge), tests 80, one scored rule worth 20:

| | Result | Points |
|---|---|---|
| Both core tests | ✅ | gate opens |
| Edge tests | 2 of 3 | |
| Overall tests | 4 of 5 | `80 × 0.8` = **64** |
| `AtMost If 3` — she wrote 5 | ❌ | **0** |
| **Final** | | **64** → below 85, so she can resubmit |

### The natural pairing of the two per-test flags

The defaults produce the right combination without the teacher thinking about it — `IsSample` defaults to
`false` (hidden), `IsCore` defaults to `true`:

```
1234 → 10    ☑ sample   ☑ core    shows her the format
 999 → 27    ☐ hidden   ☑ core
   0 →  0    ☐ hidden   ☐ edge    the traps stay hidden
  -5 →  5    ☐ hidden   ☐ edge
```

She sees the expected shape; she does not see the edge cases she is expected to think of herself.

🔴 **Guard the all-gates case.** An assignment with no tests and no `Scored` rules — only `Blocking` ones, the
natural shape for a pure classes exercise — yields `0 + 0 = 0`, so a student who satisfies **every** requirement
scores zero. When there is nothing to allocate points to and all blocking gates passed, the score is **100**.
Cover it explicitly in `ScoreCalculator` and in the form's points validation.

`ScoreCalculator` is a Domain service and a pure function; the formula moves here out of the inline expression at
`AiWorker.cs:159`.

`IsCore` is orthogonal to `IsSample` from `plan-gradingSecurityHardening` — *sample/hidden* controls **what the
student sees**, *core/edge* controls **how it scores**. A core test can be either.

### Assignments without I/O

Not every exercise runs. *"כתבי מחלקה `Student` עם בנאי ושתי תכונות"* has no input, no output, and nothing to
execute — it is graded **entirely on structure**:

```
Tests:        (none)
Requirements: Class 🔴 · Constructor 🔴 · AtLeast Property 2 🟡 50 · AtLeast Method 1 🟡 50
Rubric:       tests 0 + rules 100 = 100 ✓
```

Consequences for the implementation:

- **Relax the Prompt 1 validator** to "at least one test **or** at least one structural requirement". Only an
  assignment with neither is invalid
- `TestsAllocation = 0` is legal, and `total == 0` must yield `testPoints = 0` **without** the current
  `AiWorker.cs:159` behaviour of scoring every student 0 overall
- The form hides the test section's "required" marker when requirements alone are present

A related case already works and only needs documenting: *"צרי מערך 1-10 והדפיסי את הסכום"* has no user input —
leave `Input` empty and set `Expected` to `55`.

### 🔴 Free-text output is the wrong thing to test

Structure checks prove a method **exists**, not that it **works**. The obvious fix — have the student write a
`Main` that exercises the class — reintroduces exact-string matching on a sentence, and sentences have many
reasonable spellings:

```
Expected:  דנה: 95
Student:   דנה החרוצה: 95      ← fails, and she was not wrong
           דנה - 95            ← fails
           השם: דנה, הציון: 95  ← fails
```

Normalization (Prompt 1) handles whitespace and line endings. It cannot handle extra words, and nothing
deterministic can.

**Guidance to surface in the assignment form** — the teacher picks one:

| Situation | Approach |
|---|---|
| Checking structure | **No tests at all.** Requirements only. Use this for pure classes exercises |
| Checking behaviour | Ask for a **single unambiguous value**: *"הדפיסי רק את הציון"* → expected `95` |
| Output must be prose | **Dictate the exact format in the task and mark that test as a sample**, so the student sees the required shape before submitting. This is precisely what `IsSample` is for |

**Do not** ask the model to judge whether output is "close enough". That reintroduces exactly the
non-reproducibility this whole design removes — same code, different grade per run.

Note the useful pairing when a `Main` is involved: the **test** proves it works, the **requirement** proves she
actually used the class rather than hardcoding `Console.WriteLine("דנה: 95")`. Neither layer catches that alone.

---

## Step 3 — Roslyn analyzer

- `server/Application/Services/CodeAnalysis/ICodeAnalysisService.cs` —
  `Analyze(sourceCode, rules) → IReadOnlyList<StructuralRuleResult>`
- `server/Infrastructure/Services/CodeAnalysis/RoslynCodeAnalysisService.cs` — NuGet
  `Microsoft.CodeAnalysis.CSharp`

```csharp
var root = CSharpSyntaxTree.ParseText(sourceCode).GetRoot();
// switch on Construct → count matching nodes + collect line numbers
```

Wrap in try/catch — unparseable code must never break grading. Register Scoped in
`Infrastructure/DependencyInjection.cs`.

### 🔴 Recursion detection must compare identifiers, not substrings

The obvious implementation is wrong:

```csharp
call.Expression.ToString().Contains(method.Identifier.Text)   // ❌
```

`"SumDigits"` contains `"Sum"`, so a method that merely *calls a differently-named helper* is reported as
recursive. Compare the invoked identifier **exactly** — resolve the `IdentifierNameSyntax` /
`MemberAccessExpressionSyntax` name and test equality with the enclosing method's identifier.

### 🔴 Dead code defeats a lone `MustUse` rule

```csharp
while (false) { }              // satisfies "must use while"
for (int i = 0; ...) { ... }   // the actual solution
```

Roslyn counts the `WhileStatementSyntax` and the rule passes. **A `MustUse` rule is usually worthless without its
`MustNotUse` partner.** Two mitigations, both required:

- The assignment form **suggests the paired rule** — picking `MustUse While` offers `MustNotUse For` alongside it
- The requirements table shown to the teacher flags a `MustUse` loop/conditional rule that has no counterpart, so
  she sees the gap while authoring

Detecting "is this code actually reachable" is out of scope — pairing is the practical control.

### Roslyn parses newer C# than Judge0 runs

The analyzer runs on .NET 8 Roslyn; Judge0 executes **Mono** (`language_id = 51`), which is considerably older.
A student can write a modern construct that Roslyn happily recognizes and Mono then refuses to compile:

```csharp
var label = n switch { 1 => "אחת", _ => "אחר" };   // satisfies MustUse Switch, Mono rejects it
```

The requirement passes, compilation fails, and the student reasonably protests *"אבל השתמשתי ב-switch!"*. The
compile-error prompt (Step 6) must therefore be told the runtime is Mono and instructed to name unsupported
modern syntax explicitly when it appears in the compiler output.

---

## Step 4 — Pipeline

```
1. Roslyn checks the requirements       ← instant · local · free
   ├─ a Blocking rule failed?
   │    → RequirementsNotMet · Score = null · AI explains · resubmit
   │    → STOP. Judge0 is never called.
   └─ clean?
2.      Judge0 runs the tests
3.      ScoreCalculator computes the grade
4.      AI writes the feedback text
```

In `AiWorker.cs`:

1. Call `ICodeAnalysisService` **before** the code runner
2. A failed `Blocking` rule → `MarkRequirementsNotMet`, call the AI for the explanation, `return` before Judge0.
   **Persist the structural results before calling the AI, and degrade gracefully if it fails** — Roslyn already
   established the fact, so an OpenAI outage must never leave the student with a bare status. Fall back to the
   deterministic finding on its own:
   ```
   ❌ הדרישה "חובה רקורסיה" לא התקיימה — נמצאה לולאת while בשורה 12
      (ההסבר המפורט אינו זמין כרגע)
   ```
   The same applies to the normal grading path: the score is already computed, so an AI failure must not discard
   it. Today `AiWorker`'s outer catch marks `AiFailed` and the grade is lost.
3. Otherwise proceed, then compute the score through `ScoreCalculator`
4. **Compile-error path** (`AiWorker.cs:135`) — remove the early `return`; call the AI with the compiler message
   so the student gets a Hebrew explanation instead of raw `error CS0103`

Checking requirements before execution also means a non-conforming submission costs no Judge0 quota.

---

## Step 5 — Retry, history, and teacher override

**Skills:** `backend-controller-endpoint-pattern`, `backend-mediatr-query-handler-pattern`,
`client-list-table-pattern`

### Retry rule — no attempt cap

```
score < 85    →  open. She resubmits until she clears 85.
score >= 85   →  locked.  🔓 teacher only
```

- `UpdateSubmissionHandler:51-55` — additionally allow `Done` when `Score < assignment.RetryThreshold`
- `Submission.MarkPendingAi()` — allow the transition from `Done` under the same condition.
  **Enforce in the domain, not only in the handler**

🔴 **The rule must not apply retroactively.** On deploy, every historical submission scoring under 85 would
suddenly become editable — students could reopen work from months ago.

Note how locking actually works: `LessonResult` is keyed by **`(StudentId, LessonId)`**, so a lesson is finalized
**per student**, not for the class, when the teacher uses "סיום שיעור" (`CompleteWith`, one-shot). Separately,
`SchoolClass.IsArchived` marks a rolled-over year.

Gate the retry rule on **three** conditions — a submission is locked if any holds:

1. Its `LessonResult.IsComplete` is true for that student and lesson
2. Its lesson's class `IsArchived`
3. **It was submitted before this feature went live**

⚠️ **The third is not optional.** Teachers who never used "סיום שיעור" have no completed `LessonResult` rows at
all, so conditions 1 and 2 would protect nothing and the entire submission history would unlock on deploy. Verify
against the real database before shipping.

**Rate-limit resubmissions.** With no attempt cap, `while (true) { }` costs `cpu_time_limit × testCount` seconds
per attempt and can be resubmitted immediately. Enforce a minimum interval (≈1 minute) between submissions for
the same assignment — cheap to add, and it also absorbs double-clicks.

### History

Resubmission must **not** destroy the previous attempt. Today `MarkPendingAi()` wipes `Score`, `FeedbackJson`
and `TestResults` in place, so with free retries the record vanishes.

Archive each attempt (score, feedback, test results, structural results, source, timestamp) before resetting.
**Only the latest attempt counts as the grade**, so averages never double-count. Reset `GradedAt` as well — it
currently stays stuck on a stale date.

Teacher screen: an attempt timeline — *"ניסיון 1: 40 · ניסיון 2: 78"*.

🔴 **Attempts must live in their own table.** Prompt 1 adds a unique index on `(StudentId, AssignmentId)`, so
storing an attempt as another `Submission` row would violate it immediately. Model history as a separate
`SubmissionAttempt` table keyed by `SubmissionId` — the `Submission` row stays the single current state.

Unlimited retries plus a full per-attempt archive grows without bound in SQLite. Keep the **most recent N
attempts in full** (10 is ample) and collapse older ones to score + timestamp only — the source and feedback of
attempt 3 out of 30 has no audience.

### Editing an assignment that already has submissions

Changing tests, requirements or the rubric after students have submitted silently produces a class graded under
two different rulebooks — the first fifteen students by the old rules, everyone after by the new ones.

- **Warn on save** when the assignment has any submission: state how many, and what changed
- Offer **"בדיקה מחדש של כל ההגשות"**, which re-enqueues every existing submission through the current rules
- If she declines, mark affected submissions as graded under an earlier revision so the discrepancy is visible
  rather than invisible

### Teacher override

**A button, not a code.** A shared code was proposed and rejected: codes get passed between students, which
silently disables the rule, whereas a button is already protected by the existing authentication.
`[Authorize(Roles = "Teacher,Admin")]` plus the lesson-ownership check is sufficient — no new password layer.

```
דנה כהן    92    נעול 🔒    [ אישור הגשה נוספת ]
```

**The grant overrides everything**, the 85 threshold included. Implement as
`Submission.GrantExtraAttempt(teacherId, reason)` — a one-shot flag consumed by the next submission, recording
who / when / for whom / why. That audit trail is what replaces "seeing who used the code".

Also in scope, as a safety net rather than part of the normal flow:

- Override `Submission.Score` with a reason — today `Score` is `private set` with no mutator at all
- Reopen a finalized `LessonResult` — `CompleteWith` throws `"Already completed"` and a wrong final grade can
  currently **never** be corrected

### 🔴 Surface the computed scores when finalizing a lesson

**Every grade this engine computes currently stops at the submission and never reaches the lesson grade.**
`CompleteLessonHandler` takes `command.FinalScore` verbatim from what the teacher typed; it injects
`ISubmissionRepository` but uses it **only** to check blocking statuses (lines 28-52) and never reads a score.
On the client, `openFinalize()` sets `finalScore = null` — nothing is suggested at all.

So the teacher hand-computes an average per student, for every student, while the system already knows every
number. Fix the dialog to show the inputs and pre-fill the suggestion:

```
סיום שיעור — דנה כהן

   תרגיל 1 ......  85
   תרגיל 2 ......  90
   תרגיל 3 ......  78
   ─────────────────
   ממוצע .......  84.3

   ציון סופי:  [ 84.3 ]     ← pre-filled, fully editable
```

The teacher still decides — the value stays an editable suggestion, and manual entry must remain possible for the
case the code comment at `CompleteLessonHandler:27` already anticipates (*"AiFailed מותר בכוונה — מאפשר למורה
לתת ציון ידני כשה-AI נכשל"*). Skip submissions with a null score in the average and say so in the dialog.

---

## Step 6 — Rewrite the OpenAI prompt

### What the AI is actually for

Everything factual is decided deterministically before the model is called. Roslyn decides whether a requirement
was met; the test runner decides whether the output matched; `ScoreCalculator` decides the number. **The model
contributes only the Hebrew explanation** — which is exactly why the grade stays reproducible while the wording
may vary.

Without it, a 9th-grader sees:

```
❌ Test 1 failed. Expected: 10, Got: 45
❌ Requirement not met: Recursion
❌ error CS0103: The name 'sum' does not exist in the current context
```

Correct, precise, and useless to her. With it:

> המטלה דרשה פתרון ברקורסיה, ובקוד שלך יש לולאת `while` בשורה 12. הרעיון שלך נכון — את מחשבת את הסכום
> כמו שצריך. כדי להפוך את זה לרקורסיה: במקום הלולאה, תני למתודה לקרוא לעצמה עם `n-1`.

The compile-error path is where it earns the most: `CS0103` becomes *"בשורה 7 השתמשת במשתנה `sum` שלא הגדרת;
נראה שהתכוונת ל-`total` משורה 4"* — the difference between a stuck student and one who can continue.


Today `OpenAiFeedbackService` sends ~350 tokens in **every** scenario with **no output cap**.

| Issue | Action |
|---|---|
| Scoring block (lines 62-70) | **Delete.** ~40% of the prompt and now entirely redundant — Roslyn and the rubric set the grade |
| `optional_full_solution` | **Delete.** The most expensive output field, and it hands the student the solution |
| One prompt for every case | **Split into three** (below) |
| No `max_tokens` | **Add** — 600 |
| No JSON mode | `response_format: { type: "json_object" }` — less preamble, fewer parse failures |
| Temperature | 0.2 — also steadies the wording between runs |
| Unbounded source length | Truncate at ~4000 chars with a marker |

Remove `scores` from `AiFeedbackResult` and `AiFeedbackResultDto` too — **the AI returns no numbers.**

### Three focused prompts

| Scenario | Contents | Size |
|---|---|---|
| **Compile error** | compiler message + code. No tests, no rules | ~120 tok |
| **Requirement unmet** | the rule + the finding + code. **No test data at all** — Judge0 never ran | ~140 tok |
| **Normal grading** | pass count + **sample tests only** + code | ~180 tok |

Shared system preamble stays short:

```
You are a C# teacher writing feedback for a 9th-grade student in Israel.
Write all text in Hebrew, addressing the student in the FEMININE form
(את, שלך, כתבת, נסי) — this is a girls' school. Warm but direct.
State only the facts given below. Never invent errors or results.
Return strict JSON only.
```

🔴 **Feminine Hebrew is a hard requirement, not a preference.** The default LLM habit is masculine forms; the
instruction must be explicit and the verification step must actually read the generated text to confirm it. Every
piece of UI copy written in these prompts follows the same rule.

### 🔴 Critical — the prompt must not defeat the hiding

If the model receives `expected: 10` and writes *"החזרת 45 במקום 10"*, it has just leaked the answer and all of
Prompt 1's work is wasted.

**Hidden test details are never sent to the model** — only the fact that a test failed. It receives the code and
the sample tests, which is enough to locate the bug in nearly every case, and costs fewer tokens.

Expected saving: **~50% on input**, more on output.

---

## Step 7 — Client

**Skills:** `client-flow-fix-implementation-pattern`, `client-student-area-pattern`,
`client-design-token-rollout-pattern`

### Assignment form

A `FormArray` following the existing `tests` pattern in `assignment-form.component.ts`:

```
דרישות התרגיל (אופציונלי)

   [ חובה להשתמש ב ▾ ] [ while ▾ ]   ● חובה  ○ מנוקדת      ○ המלצה
   [ לכל היותר 3   ▾ ] [ if    ▾ ]   ○ חובה  ● מנוקדת [20] ○ המלצה
   + הוספת דרישה

   ניקוד:  בדיקות [80]  +  דרישות [20]  =  100 ✓
```

The `Construct` dropdown is grouped per the catalog. Live validation that the points total exactly 100.

**Keep the form approachable** — an assignment with no requirements must stay as quick to create as it is today.
The requirements section is collapsed by default and the points row is hidden entirely when no scored rule
exists.

### Feedback panel

`submission-feedback-panel.component.ts`:

- **Remove the four AI score tiles** — the server no longer returns them
- Remove the disclaimer added earlier (`הציון... מחושב מתוצאות הבדיקות בלבד`) — no longer true
- Show the real breakdown: `בדיקות 64 · דרישות 0 · סה"כ 64`
- Add a requirements table (rule, required, found, result) and a `RequirementsNotMet` state with the explanation
  and the resubmit button from Prompt 1

---

## Step 8 — Extract a new skill from the working code

**Skill:** `create-skill`

Create **two** skills after the code works:

**`backend-roslyn-code-analysis`** — parsing with `CSharpSyntaxTree`, mapping a construct enum to
`DescendantNodes().OfType<T>()`, detecting recursion, computing nesting depth, extracting line numbers, and
tolerating unparseable input. Include the "add a construct = one enum value + one case" extension recipe, since
the catalog grows every term.

**`backend-ai-feedback-prompt-pattern`** — the scenario-split prompt design from Step 6, which is non-obvious and
will be re-touched every time feedback changes:

- One short shared preamble plus a scenario-specific block, rather than a single prompt carrying rules that do
  not apply (a compile-error case needs no test or rubric instructions)
- **The model never returns numbers** — deterministic code owns every score; the model owns only Hebrew wording
- **Never send data the student must not see.** Hidden test values are withheld from the model itself, because a
  model told "expected 10" will quote it back and defeat the redaction
- Cost controls: `max_tokens`, `response_format: json_object`, low temperature for steadier wording, source
  truncation
- The failure mode that motivated this: a JSON template containing literal `0` values, which the model copied
  verbatim into every student's scores

Mirror both into `.github/skills/` and `.claude/skills/` per the root [CLAUDE.md](../../CLAUDE.md).

---

## Verification

```bash
cd server && dotnet build SmartGrader.sln     # stop the running API first — it locks Infrastructure.dll
cd client && npx ng build
```

Assignment "factorial": 2 tests (one sample), `MustUse Recursion` (**Blocking**), `AtMost If 3` (**Scored**,
20 pts), tests allocated 80.

| # | Submit | Expected |
|---|---|---|
| 1 | Correct solution using `while` | ❌ No grade · Hebrew explanation · resubmit button · **Judge0 never called** |
| 2 | Click the button | Editor opens **with the previous code** |
| 3 | Correct recursion, 5 `if` | ✅ 80 + 0 = **80** |
| 4 | **The exact same code again** | ✅ **Exactly 80 again** ← the core stability check |
| 5 | Reduce to 3 `if` | ✅ **100**, now locked (≥85) |
| 6 | Try to resubmit at 100 | Rejected |
| 7 | Teacher clicks "אישור הגשה נוספת" | Opens despite the threshold · the grant is audited |
| 8 | Student calls the grant endpoint directly | 403 — `Teacher,Admin` only |
| 9 | Feedback text on a hidden failed test | **Never mentions an expected value** |
| 10 | An assignment with no requirements at all | Behaves exactly as before: tests worth 100 |
| 11 | All core tests pass, one **edge** test fails (3 of 4 overall) | Test points = `allocation × 3/4` — the forgotten edge case costs a little, not everything |
| 11b | A **core** test fails | Test points = **0**, requirement points still awarded, resubmit stays open |
| 11c | Hardcoded solution passing 2 of 5 by luck, both core failing | **0** test points |
| 11d | Create an assignment with only 2 test cases | Soft warning that each is worth half the test points |
| 11e | A scored rule partially satisfied (4 `if` where 3 allowed) | Loses the **whole** rule's points — requirements are binary |
| 11f | Single-test assignment (*"שורשים שלמים עד 30"*), tests 50 / requirements 50; student's output is wrong but her code uses a local variable and a loop | Scores **50**, not 0 — and stays open for resubmission |
| 11g | `MustUse Matrix` against code declaring only `int[]` | **Not** satisfied — a 1-D array must not count as a matrix |
| 11h | `MustUse BoolVariable` against the flag-variable pattern `bool isSorted = true;` | Satisfied |
| 12 | A classes assignment: **zero tests**, OOP requirements only | Saves, grades, and scores out of 100 on structure alone |
| 13 | An assignment with neither tests nor requirements | Rejected by the validator |
| 14 | Read the generated Hebrew feedback | Addresses the student in the **feminine** form throughout |
| 15 | A classes assignment whose `Main` prints `דנה החרוצה: 95` against expected `דנה: 95` | Fails — **correctly**. Confirms the form steers teachers toward structure-only or single-value tests instead |

**Adversarial and failure-path checks — these are the ones that catch design holes:**

| # | Scenario | Expected |
|---|---|---|
| 16 | `MustUse While` only; student writes `while(false){}` plus a real `for` loop | Rule passes — **known hole**. Confirm the form offered the paired `MustNotUse For`, which closes it |
| 17 | Method `SumDigits` that calls a separate method `Sum`; `MustUse Recursion` | Correctly reported as **not** recursive (no substring match) |
| 18 | `MustUse Switch`; student writes a modern `switch` expression | Compiles-error path explains the Mono limitation by name |
| 19 | An old submission scoring 40 in a **finalized** lesson, after deploy | Stays locked — the retry rule is not retroactive |
| 20 | Two submissions within seconds | Second is rate-limited |
| 21 | OpenAI key removed, then submit failing a blocking rule | Requirement result still shown, status correct, no `AiFailed` |
| 22 | OpenAI key removed, then submit a normally-gradeable solution | **Score is still saved** — an AI outage does not discard a computed grade |
| 23 | Edit an assignment that already has 3 submissions | Warned, offered a re-grade of all three |
| 24 | `while(true){}` submitted | TLE per test, graded as failed, no worker hang |
| 25 | A submission scoring 40 whose `LessonResult.IsComplete` is true | Locked |
| 26 | A submission scoring 40 in an **archived class** | Locked |
| 27 | Open "סיום שיעור" for a student with graded submissions | Per-assignment scores listed, average pre-filled, still editable |
| 28 | Same, for a student with one ungraded submission | Ungraded one excluded from the average, and the dialog says so |
