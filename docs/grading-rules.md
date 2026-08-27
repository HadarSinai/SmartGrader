# Grading Rules

> SmartGrader · Version 1.0 · Last updated 2026-08-27 · Status: as-built

| Version | Date | Change |
|---|---|---|
| 1.0 | 2026-08-27 | First edition. `G-1 … G-25`. |

**What this document answers:** how a grade is produced — written so the owner can explain any number
to a parent, from this document alone.

**The acceptance test for this document** is exactly that: take a real graded submission, hand a reader
this file, and ask them to reproduce the number by hand. If they cannot, the document is wrong, not the
reader.

---

## The principle everything else rests on

> **No language model determines any number.**

Roslyn decides whether a structural requirement was met. The sandbox decides whether the output
matched. `ScoreCalculator` decides the score. The model writes **prose only** — the explanation a
student reads, never a figure she is graded on.

Two consequences that are easy to lose and expensive to rediscover:

- **The same code always receives the same grade.** `ScoreCalculator` and `LessonScoreCalculator` are
  pure functions: same inputs, same number, every run.
- **A model failure can never erase a grade.** The score is computed *before* the model is called, and
  the call is wrapped. An earlier version had one outer `catch` that marked the submission `AiFailed`,
  and a perfectly correct score was lost to an OpenAI hiccup.

---

## The pipeline

```
1. Roslyn checks the structural requirements     ← immediate · local · free
   ├─ a Blocking requirement failed, and the code parses?
   │     → RequirementsNotMet · NO SCORE AT ALL · the model explains · stays open
   │     → stop. The sandbox is never called.
   └─ clean?
2.      the sandbox runs the test cases
3.      ScoreCalculator produces the score and its breakdown
4.      the model writes the prose
```

Requirements are checked **before** execution not merely because it is logical: a submission that
does not meet a blocking requirement consumes no execution quota at all.

---

## The rules

Every rule has a stable id. **A rule is stated here once and referenced everywhere else by id** — an
area doc lists `G-7`, it does not restate it.

`GradingRuleCoverageTests` binds each id to the tests that prove it, through
`[Trait("Rule", "G-N")]`. Delete the behaviour and the test goes with it, and this table goes red.

<!-- gen:rules G -->

| Id | Rule | Covered |
|---|---|---|
| G-1 | When a Blocking structural requirement is not met and the code parses, the system shall set the submission to `RequirementsNotMet`, shall produce no score, and shall not call the sandbox. | — |
| G-2 | When the code fails to parse, the system shall not apply the Blocking gate, and shall continue to execution. | — |
| G-3 | When the submitted code fails to compile, the system shall set the submission to `CompilationFailed` and shall produce no score. | — |
| G-4 | When the sandbox is unreachable, the system shall set the submission to `JudgeUnavailable`, shall produce no score, and shall not retry automatically. | — |
| G-5 | When the language model fails, the system shall retain the score already computed and shall degrade only the prose. | — |
| G-6 | The system shall award test points as `testsAllocation × passed ÷ total`. | ✅ |
| G-7 | When any Core test case fails, the system shall award zero test points, regardless of how many other cases passed. | ✅ |
| G-8 | When no test case ran, the system shall award zero test points — an empty run is not a failed run. | ✅ |
| G-9 | When there is nothing to allocate points to, the system shall award the assignment's full ceiling. | ✅ |
| G-10 | The system shall award a Scored requirement's points in full or not at all. | ✅ |
| G-11 | The system shall award no points for a Blocking or an Advisory requirement. | ✅ |
| G-12 | The system shall cap a submission's score at the assignment's ceiling. | ✅ |
| G-13 | The system shall round every score to one decimal place. | ✅ |
| G-14 | The system shall reject an assignment whose rubric does not sum to exactly its ceiling. | ✅ |
| G-15 | The system shall reject an assignment that has neither a test case nor a structural requirement. | ✅ |
| G-16 | The system shall reject a Scored requirement carrying fewer than one point. | ✅ |
| G-17 | The assignment ceiling shall be 100, or 100 + the bonus value for a bonus assignment. | ✅ |
| G-18 | The lesson's computed score shall be the unweighted average of the assignments that have a score. | ✅ |
| G-19 | The system shall exclude an ungraded assignment from the lesson average, and shall not count it as zero. | ✅ |
| G-20 | When no assignment in a lesson has a score, the computed score shall be absent, and a final score shall be possible only as an explicitly reasoned override. | ✅ |
| G-21 | The lesson ceiling shall be 150 when the lesson contains a bonus assignment and 100 otherwise, derived from the assignments and never from the request. | ✅ |
| G-22 | The system shall treat an entered final score within 0.05 of the computed score as agreement, not as an override. | ✅ |
| G-23 | An override of a submission's score shall carry a written reason and shall lie between 0 and the assignment ceiling. | ✅ |
| G-24 | An override of a lesson's final score shall carry a written reason, and the computed score shall be retained beside it. | ✅ |
| G-25 | Only the latest attempt on an assignment shall count toward any score. | ✅ |

<!-- /gen -->

### The five uncovered rules, and why

`G-1` … `G-5` describe the **order and failure handling of the pipeline**, which lives in
`AiWorker` — in the `Api` project. The test project is fixed to `Domain` + `Application`
(`backend-unit-test-pattern`), and adding an `Api` reference to reach one background service would
drag the whole web host into the unit-test project.

**This is a real gap, not a technicality.** `G-1` is arguably the most important rule in the system,
and nothing fails if someone reorders those two steps. Closing it needs either a handler-level seam in
front of `AiWorker` or an integration test that owns a host — a decision, not an oversight, and it is
recorded here so it stays visible.

---

## Worked examples

### The core gate — `G-6`, `G-7`

An assignment with `TestsAllocation = 100`, five test cases, four of them Core.

| Case | Passed | Core |
|---|---|---|
| 1 | ✅ | ✅ |
| 2 | ✅ | ✅ |
| 3 | ✅ | ✅ |
| 4 | ✅ | ✅ |
| 5 | ❌ | ❌ |

All Core cases passed, so the gate opens. Test points = `100 × 4/5` = **80**.

Now fail case 2 instead — a Core case. The gate closes and test points are **0**, even though four
cases out of five passed.

**Why both halves.** Purely proportional scoring rewards luck: a submission that never reads the input
and prints a constant passes 2 of 5 by accident and collects 32 points for nothing. Pure all-or-nothing
punishes a student who solved the problem and forgot `n = 0`. The gate is the compromise — the central
cases are a threshold, and past it an edge case costs only its own share.

### Nothing to allocate — `G-9`

A classes exercise: "write a class `Student` with a constructor and two properties." No input, no
output, nothing to run. `TestsAllocation = 0`, no Scored requirements, only Blocking ones.

`allocatable = 0`. Adding zero to zero gives **0**, which would fail every student who did exactly
what was asked. The rule instead awards the **full ceiling — 100** — and it is sound precisely because
this line is reached only after every Blocking gate has opened.

### The rubric — `G-14`, `G-16`

An assignment worth 100 with three test cases and two Scored requirements:

| Component | Allocation |
|---|---|
| test cases | 60 |
| "must use recursion" (Scored) | 25 |
| "at most 3 `if`" (Scored) | 15 |
| **total** | **100** ✅ |

Change the second requirement to 20 and the total is 105 — rejected. Set either requirement to 0
points and it is rejected by `G-16`: a scored requirement worth nothing does nothing, and is almost
always an unfilled field.

**An assignment with no Scored requirements needs no arithmetic** — the test cases take all 100
automatically, so an ordinary assignment stays as quick to create as it ever was.

### All-or-nothing on a requirement — `G-10`

"At most 3 `if` statements", worth 25. The student wrote 4.

She loses **all 25**, not a quarter of them. A requirement is a **condition, not a measurement**:
there is no meaningful sense in which four `if`s is 75% of at most three.

### A bonus assignment — `G-12`, `G-17`

`IsBonus = true`, `BonusValue = 20` → ceiling **120**, and the rubric must sum to 120. A student who
does everything scores 120. `G-12` caps at 120 even if a mis-saved rubric would produce more.

⚠️ **Plan B's B2 replaces this model.** Under B2 every assignment is graded out of 100 and the bonus
becomes a lesson-level addition. This document describes what is built **today**; when B2 ships,
`G-17`, `G-21` and their examples change together, in one version bump.

### The lesson score — `G-18`, `G-19`, `G-20`

A lesson with four assignments. The student has:

| Assignment | Score |
|---|---|
| 1 | 90 |
| 2 | 80 |
| 3 | still being graded |
| 4 | never submitted |

Computed score = `(90 + 80) ÷ 2` = **85** — two assignments, not four.

**Assignment 3 and 4 are skipped, not zeroed.** Averaging in a zero for work that is still in the
queue shows a student a low number that has nothing to do with anything she did.

If *nothing* is graded, the computed score is **absent** — not zero. The teacher may still record a
final grade, but only as an override with a written reason (`G-20`, `G-24`).

### Agreement versus override — `G-22`

The dialog suggests 85. The teacher accepts and submits 85.

`Calculate` rounds to one decimal, so an exact `==` on a `double` would flag the system's own
suggestion as a departure demanding a written justification. Anything within **0.05** is agreement.
Enter 87 and it is an override, and the reason becomes mandatory.

---

## The three severities as product concepts

| Severity | What it means to a teacher | Effect |
|---|---|---|
| 🔴 **Blocking** | "the exercise asked for recursion and she wrote a loop — that is not the exercise" | **Rejection.** No score at all, the submission stays open. Carries no points, because it is a gate. |
| 🟡 **Scored** | "this is worth 25 of the 100" | All of it, or none of it. |
| ⚪ **Advisory** | "worth mentioning in the feedback" | Nothing. |

**Blocking is the one people get wrong.** It is not a heavy penalty; it produces **no grade**, and
that is deliberate — a submission that did not do what was asked has not been assessed, it has been
returned.

---

## What the model is allowed to touch

| Produced by | Never produced by |
|---|---|
| the wording of the feedback | any score, any point value, any count |
| which mistake to explain first | whether a requirement was met |
| suggested test cases *for a teacher to verify* | whether a test passed |

A suggested test case is only a proposal: it is executed against the teacher's reference solution, and
the execution wins on disagreement.

Hidden test values are withheld from the prompt itself — not filtered afterwards — because the same
sentences reach the student, and a model that was given the answer will paraphrase it.

---

## Applicable business rules

Resubmission, locks, attempt limits and deletion are **not** grading rules. See
[business-rules.md](business-rules.md): `B-1` … `B-8` (submissions and attempts) are the ones that
decide whether a grade can still change.
