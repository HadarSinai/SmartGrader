# Plan: Test-Case Authoring — Verify & AI-Generate

## TL;DR

A teacher writes test cases today with **no way to check them**. One mistyped expected value — `20` where the sum
of digits is `19` — fails the entire class on correct code, and she only finds out when students complain. Two
features close that, and they reinforce each other:

1. **Verify** — the teacher pastes a known-good solution and the system runs the current test list against it,
   so an authoring error surfaces before the assignment reaches the class.
2. **Generate** — AI proposes test cases from the task description, and **every proposal is executed against the
   reference solution before the teacher ever sees it**, so a wrong suggestion is caught by the machine rather
   than trusted.

**Ship this before [plan-gradingRequirementsEngine](plan-gradingRequirementsEngine.prompt.md)**, which makes
tests all-or-nothing — at that point a single authoring typo zeroes every student's test points.

## Why AI is safe here, unlike grading

Elsewhere in this system AI is deliberately kept away from any number, because a grade must be reproducible and
explainable. Test generation is the opposite situation:

| | Grading | Test generation |
|---|---|---|
| Who consumes the output | the student, as a grade | the teacher, as a draft |
| Is it verified | it *is* the verdict | **executed against a reference solution** |
| Cost of a mistake | an unfair grade nobody can explain | a suggestion the teacher edits or deletes |
| Reproducibility required | yes | no — she reviews it once |

**Nothing the model proposes is trusted.** It is run, checked, and reviewed by a human before it is saved.

---

## ⚠️ MANDATORY — load the relevant skill before writing code

| Work | Skill |
|---|---|
| New Command/Query + Handler + FluentValidation | `backend-mediatr-query-handler-pattern` |
| New controller action / route | `backend-controller-endpoint-pattern` |
| Entity ↔ DTO mapping | `backend-automapper-profile-pattern` |
| Form array, dialogs, inline validation | `client-flow-fix-implementation-pattern` |
| Result table styling | `client-list-table-pattern`, `client-design-token-rollout-pattern` |
| Writing the skill in Step 5 | `create-skill` |

Hebrew copy addresses the user in the **feminine** form — this is a girls' school. Follow
[server/CLAUDE.md](../../server/CLAUDE.md) and [client/CLAUDE.md](../../client/CLAUDE.md).

**Depends on:** [plan-authorizationAndDataSafety](plan-authorizationAndDataSafety.prompt.md) (ownership checks)
and [plan-gradingSecurityHardening](plan-gradingSecurityHardening.prompt.md) (output normalization, `IsSample`).

---

## Step 1 — The reference solution

Add `Assignment.ReferenceSolution` (string, nullable) — the teacher's own known-good implementation. It is the
foundation of both features and must **never** be exposed to students: exclude it from every DTO a student can
reach, following the redaction pattern established for `Tests` in `plan-gradingSecurityHardening` Step 1.

In the assignment form it lives in a collapsed section, *"פתרון לדוגמה (לא נראה לתלמידות)"*.

---

## Step 2 — Verify the test cases

**Skills:** `backend-controller-endpoint-pattern`, `backend-mediatr-query-handler-pattern`

A command that runs the reference solution against the current test list through the existing
`ICodeRunnerService` and returns per-test results. **Nothing is persisted** — no `Submission` row, no score, no
AI call.

```
┌─────────────────────────────────────────────┐
│  בדיקת מקרי הבדיקה                          │
│                                             │
│  ✅ בדיקה 1    קלט 1234  →  10              │
│  ✅ בדיקה 2    קלט  999  →  27              │
│  ✅ בדיקה 3    קלט   55  →  10              │
│  ❌ בדיקה 4    קלט  892                     │
│       ציפית 20  ·  הפתרון שלך החזיר 19      │
│  ✅ בדיקה 5    קלט    7  →   7              │
│                                             │
│  [ תיקון ל-19 ]   [ עריכה ידנית ]           │
└─────────────────────────────────────────────┘
```

Requirements:

- Reuse the same `GradingMode` dispatch `AiWorker` uses, so verification exercises the **real** path — including
  the `Method`-mode wrapper and the culture and normalization fixes from `plan-gradingSecurityHardening`
- Offer a one-click **"תיקון"** that writes the actual value into the expected field
- Surface compile errors in the reference solution itself clearly — that is the teacher's bug, not a test bug
- The teacher must be able to save without verifying; **warn, do not block**

---

## Step 3 — AI-generated test cases

**Skills:** `backend-mediatr-query-handler-pattern`, `backend-ai-feedback-prompt-pattern` (once written)

### Flow

```
1. Teacher writes the task description (+ reference solution)
2. Clicks "הצע מקרי בדיקה"
3. The model proposes N candidate INPUTS with expected outputs
4. 🔴 Every candidate is EXECUTED against the reference solution
5. Where they disagree, the reference solution wins and the row is flagged
6. The teacher reviews, edits, marks samples, and saves what she wants
```

**Step 4 is what makes this safe.** The model is a source of *ideas for interesting inputs*, not a source of
truth about outputs. Its arithmetic is never trusted.

### Prompt

Follow the design in `plan-gradingRequirementsEngine` Step 6 — short, one job, strict JSON, capped output:

```
Propose {n} test cases for this C# exercise.
Task: {description}
Grading mode: {mode}  (FullProgram: input is full stdin | Method: space-separated args | MultiFileMethod: JSON array)

Cover: the ordinary case, boundary values, and any edge case the description implies.
Inputs must match the grading mode's format exactly.
Mark a case "core" when it tests the main thing the exercise is about,
and "edge" when it tests a boundary or unusual input.

{"cases":[{"input":<string>,"expected":<string>,"why":<string>,"core":<boolean>}]}
```

`why` is shown to the teacher so she understands what each case is for; it is not persisted.

`core` pre-fills the `IsCore` flag that
[plan-gradingRequirementsEngine](plan-gradingRequirementsEngine.prompt.md) uses to gate scoring — a failing core
test zeroes the test points, while a failing edge test only costs its share. The teacher can override every
suggestion; the model is proposing a classification, not deciding one. This is the same discipline as the values
themselves: **suggest, verify, let a human confirm.**

Reuse the cost controls: `max_tokens`, `response_format: json_object`, low temperature. Degrade gracefully — if
the model is unavailable, the button reports it and manual authoring is unaffected.

### Review UI

```
┌──────────────────────────────────────────────────────┐
│  הצעות (5)                    ✓ אומתו מול הפתרון שלך │
│                                                      │
│  ☑  1234  →  10      מקרה רגיל                       │
│  ☑     0  →   0      ערך גבול                        │
│  ☑     7  →   7      ספרה בודדת                      │
│  ⚠     -5 →   5      שלילי — הפתרון שלך החזיר 5,     │
│                       ה-AI הציע 0. נבדק, וזה 5.      │
│  ☐  99999 →  45      מספר גדול                       │
│                                                      │
│         [ הוספת המסומנות ]                           │
└──────────────────────────────────────────────────────┘
```

- Nothing is saved until she confirms — this is a proposal list, not an edit
- Rows where the model and the reference disagreed are **flagged**, showing both values; the executed result is
  what gets saved
- Without a reference solution, generation still works but every row is marked **"לא אומת"** and the warning is
  prominent
- Marking samples (`IsSample`) happens here, so she chooses what students will see at the same moment

---

## Step 4 — Guardrails

| What | Why |
|---|---|
| Both actions are `[Authorize(Roles = "Teacher,Admin")]` **plus** a lesson-ownership check | Same omission as `CompleteLesson` in `plan-authorizationAndDataSafety` Step 4 |
| Rate-limit generation | Each click is a paid API call; a held button should not spend |
| Cap `n` (≈10) | Bound the cost and keep the review list readable |
| Never persist `ReferenceSolution` into any student-facing DTO | It is the full answer |
| Verification results are transient | They describe the teacher's draft, not a graded artifact |

---

## Step 5 — Extract a skill

**Skill:** `create-skill`

Create **`backend-ai-verified-generation-pattern`** — the general shape this establishes and the reason it is
trustworthy:

- The model proposes candidates; **deterministic execution decides the truth**; a human confirms before anything
  is stored
- On disagreement the executed result wins, and the disagreement is shown rather than hidden
- Contrast with the grading path, where the model is kept away from numbers entirely — the distinction is *is
  this output independently verifiable before use?*
- Graceful degradation: the manual path must never depend on the model being reachable

Mirror into `.github/skills/` and `.claude/skills/` per the root [CLAUDE.md](../../CLAUDE.md).

---

## Verification

```bash
cd server && dotnet build SmartGrader.sln     # stop the running API first — it locks Infrastructure.dll
cd client && npx ng build
```

| # | Action | Expected |
|---|---|---|
| 1 | Paste a correct reference solution, verify | All tests pass; **no submission row created** |
| 2 | Mistype one expected value, verify | That row fails, showing expected vs actual, with a "תיקון" button |
| 3 | Click "תיקון" | The expected field updates to the executed value |
| 4 | Paste a reference solution that does not compile | The compile error is shown as the teacher's problem, not a failed test |
| 5 | Generate with a reference solution present | Proposals arrive **already executed**; disagreements flagged |
| 6 | Generate **without** one | Still works; every row marked "לא אומת" |
| 7 | Generate, then cancel | Nothing is saved |
| 8 | Generate for a `Method`-mode assignment | Inputs are space-separated, not JSON |
| 9 | Generate for `MultiFileMethod` | Inputs are JSON arrays |
| 10 | Remove the OpenAI key, then generate | Clear message; manual authoring and verification still work |
| 11 | Teacher B calls either endpoint on Teacher A's assignment | Rejected |
| 12 | Fetch the assignment as a student | `ReferenceSolution` absent from the payload |
