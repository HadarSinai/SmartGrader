---
name: spec-feature-area-doc
description: "Use when writing or reviewing one of the six SmartGrader area specifications under docs/areas/ — teacher-content, teacher-classroom, student, admin, auth-account, or shared-ui. Covers the grouping rule (by the job the person is doing, not by the code folder), the mandatory grounding pass before a word is written (app.routes.ts → controller → handler → component → existing rules), the fixed section outline every area doc shares, the machine-asserted Screens & Routes table, the Screen Composition section, and the question bank an area doc is not finished until it can answer. USE FOR: 'write the area spec for X', 'spec the student area', 'which document does this screen belong in', 'is this area doc finished', 'what questions must an area spec answer'. NOT for the wording of an individual requirement sentence (that is spec-requirement-writing), and NOT for the conformance test that keeps the route table true (that is spec-domain-doc-conformance)."
---

# Writing a SmartGrader Area Specification

Six documents under `docs/areas/`. Each one is the single place a person doing one job goes to find
out what the system does for them.

## The grouping rule: by the job, not by the code folder

The old set sliced by **screen** — `assignments-jtbd.md`, `lessons-flow.md`, `students-journey.md`,
twenty files. That is why it never asked *"can an assignment be edited once submissions exist?"*:
the question exists only **between** lessons, assignments and submissions, and no per-screen document
owned it. Slicing by folder reproduces the folder's blind spots in the specification.

| Doc | Covers | The job |
|---|---|---|
| `teacher-content.md` | courses → lessons → assignments (+ the assignment form) | authoring what students will do |
| `teacher-classroom.md` | classes → students → submissions → lesson results → dashboard | running a class and grading it |
| `student.md` | the five `/my` screens | seeing my own work and my own grade |
| `admin.md` | teachers, system log | administering the installation |
| `auth-account.md` | login, forgot, reset, profile, lockout | getting in and staying in |
| `shared-ui.md` | topbar, the two shells, notifications bell, feedback panel, three form controls, a11y widget | — the components every job uses |

**`shared-ui.md` is the sixth because eight components belong to no page area.** They had no owner and
would have been missed — most sharply the notifications bell, which is a real feature (two entirely
different feeds by role, a 30-second poll, read state in `localStorage`, and `ClassSignalDetector`
thresholds behind it) that is specified nowhere at all today.

**Coverage check — the plan's own acceptance condition:** 13 page areas across the five job docs
(courses, lessons, assignments · classes, students, submissions, lesson-results, dashboard · my ·
teachers, logs · auth, profile) plus 8 shared components in the sixth. **If a screen or component is
not named in exactly one area doc, that is a defect** — not in the doc, in the plan. Say so rather
than quietly filing it twice.

**Write `student.md` first** — it is the smallest area with the sharpest owner. **Write `shared-ui.md`
last**, once the five callers exist and you know what each of them actually needs from a shared
component.

## The grounding pass — before a word is written

`docs/ux/assignments-jtbd.md` devotes 28 of its 55 lines to "Current Solution & Pain Points". That is
what happens when someone opens a document and starts writing from a screenshot: what comes out is
whatever they noticed, in the order they noticed it. **The grounding pass is what stops that.**

Walk the chain, in this order, for every screen in the area, and take notes as you go:

1. **`client/src/app/app.routes.ts`** — the exact path, its guard (`authGuard` / `teacherGuard` /
   `adminGuard` / `studentGuard`), its component, its shell (`AppLayoutComponent` vs
   `StudentLayoutComponent` vs standalone). This produces the `Screens & Routes` table directly.
2. **The controller** — `server/Api/Controllers/*.cs`. Every endpoint the screen calls: verb, route
   template, `[Authorize]` roles, what it returns.
3. **The handler** — `server/Application/UseCases/…`. The actual rules: what it validates, what it
   throws, what it redacts, what it silently allows.
4. **The component** — the Angular file. What is *really* on the screen, which columns exist, which
   are always empty, what the copy actually says (quote Hebrew verbatim).
5. **The existing rules** — `docs/grading-rules.md` and `docs/business-rules.md`. Anything already
   registered as `G-N` or `B-N` is **cited, never restated**.

Two things this pass reliably surfaces, because it did so in this repo:

- **A client URL that matches no server route.** `GET /api/students/submissions/recent` — `{studentId:int}`
  cannot match the literal `"submissions"`. It 404'd inside a `forkJoin` and took all four dashboard
  KPI cards with it, for weeks.
- **A column that can never be populated**, because the DTO has no such field.

Both are defects, not requirements. **Record them and route them to a dated defect list** — they do not
belong in `docs/`. That confusion is the whole reason the old set is being replaced.

## The fixed outline — identical in all six

The plan enumerates eight sections. Do not reorder them and do not add one; an area doc that needs a
ninth section is describing something that belongs in another document.

### 1. `Purpose`
Two or three sentences. What job this area does, for whom, and what would be lost if it did not exist.
No feature list.

### 2. `Who Uses This`
The persona, in **three lines**, inline. Not a link to a `personas.md` — a persona used by every
document belongs inside each of them, which is why the standalone file is being deleted.

### 3. `Screens & Routes`  **[machine-asserted]**
Inside a `<!-- gen:routes -->` block, per
[spec-domain-doc-conformance](../spec-domain-doc-conformance/SKILL.md):

```markdown
<!-- gen:routes -->

| Route | Guard | Shell | Screen |
|---|---|---|---|
| `/lessons/:lessonId/assignments` | `teacherGuard` | app | Assignments list |
| `/lessons/:lessonId/assignments/new` | `teacherGuard` | app | Assignment form (create) |

<!-- /gen -->
```

`PermissionsMatrixConformanceTests` text-parses `app.routes.ts` and fails when a route appears in one
and not the other. **Add a dummy route → the test goes red → revert.** That proof is part of finishing
the doc, not a nicety.

### 4. `Functional Requirements`
Numbered, in the 29148 shape, per
[spec-requirement-writing](../spec-requirement-writing/SKILL.md). Indicative mood, one `shall` each,
Hebrew UI strings quoted verbatim, no C# type names.

⚠️ **Every list screen must answer four questions here**, or the next person to touch the component
decides them alone — which is exactly how eleven list screens each ended up different:

- what is the **default sort**?
- what is the **page size**, and what options?
- **which fields** does the search box match?
- do **filters survive** navigating away and back?

### 5. `Applicable Rules`
`G-N` / `B-N` ids with their one-line titles. **Ids only — never the rule text.** A restated rule is a
second source of truth, and the copy is the one that goes wrong.

### 6. `Acceptance Criteria`
Given/When/Then, at least one per functional requirement, pass/fail with no interpretation.
**A criterion needing judgement violates *Verifiable* and goes back.**

### 7. `Screen Composition`  (the A5 deliverable)
What the screen **shows, and in what order** — as distinct from what it does. Four written questions
per screen:

1. **What comes off?** Every column, field or card not required for the decision this screen serves.
   **The default is to remove.**
2. **What does the eye hit first?** One screen, one focus. Two focuses means one is wrong.
3. **What is the reading order?** Top to bottom, right to left, most important first.
4. **How much information per row?** A nine-column table is a decision, and it has to be argued.

Per screen, record: what is shown · **what was removed and why** · where the focus is.
**A screen with nothing removed needs an explicit justification.** Written before touching code —
after, it is taste rather than specification.

### 8. `Explicitly Not Supported`
The most valuable section and the one everyone skips. Things a reader will reasonably assume work,
and do not — with a one-line reason each. "A teacher cannot delete a lesson that has submissions."
"There is no attempt cap on resubmission." "A class has no owner; any teacher can rename any class."

This section is what turns "the spec didn't say" into "the spec said no".

## The question bank

An area doc is **not finished** while it cannot answer every applicable question below. If a question
does not apply to the area, say so in one line rather than leaving it out — the silence is
indistinguishable from an oversight.

**Identity and scope**
1. Who reaches this area, and what stops everyone else — endpoint gate, row scope, or field redaction?
2. Whose rows does a person see here, and what happens when they ask for someone else's — 403 or 404?
3. Can a person reach a screen in this area without being logged in?

**Lifecycle**
4. What creates the objects in this area, and can they be created any other way?
5. Can an object be edited after it has been used — an assignment once submissions exist, a test case
   once a submission was graded against it?
6. What blocks deletion, and what is the message when it is blocked?
7. What becomes read-only, when, and who can undo it?

**Grading and correctness** *(where applicable)*
8. Which `G-N` rules reach a number on this screen, and in what order?
9. What does a student see that a teacher sees, minus which fields?
10. What is shown when there is no grade at all, as distinct from a grade of zero?

**Lists and screens**
11. Default sort, page size, searched fields, filter persistence — the four, per list screen.
12. What is the empty state, and what is the one action offered from it?
13. What is the loading state, and what is the error state?
14. What does the screen show while a background process is still running?

**Copy and interaction**
15. What exactly does the delete confirmation say, in Hebrew, verbatim?
16. Which actions toast, and which are silent?
17. Is every string gender-neutral?

**Boundaries**
18. What in this area is deliberately not supported, and why?
19. What does this area assume another area guarantees?
20. What is known to be modelled imperfectly here, and was accepted rather than fixed?

Question **5** is the one the old set could never answer, because no per-screen document owned it.
Question **18** is the one that prevents the next argument.

## Anti-patterns

| Anti-pattern | Why |
|---|---|
| Writing before the grounding pass | Produces "Current Solution & Pain Points" — 28 of 55 lines of whatever was noticed |
| A "Pain Points" section | Class C in a class B document. Defects go to a dated list outside `docs/` |
| `[Fix]` / `TODO` anywhere in an area doc | Expires silently on merge, and reads like open work forever after |
| Restating a `G-N`/`B-N` rule | Second source of truth |
| A route table typed by hand outside the marker block | Nothing fails when the route moves |
| Splitting one job across two docs "because the code is in two folders" | Reproduces the folder's blind spots |
| Omitting `Explicitly Not Supported` because nothing came to mind | Nothing came to mind because the grounding pass was skipped |

## See Also

- [spec-requirement-writing](../spec-requirement-writing/SKILL.md) — the shape of every sentence in sections 4 and 6.
- [spec-domain-doc-conformance](../spec-domain-doc-conformance/SKILL.md) — the `gen:routes` marker and the test that keeps section 3 true.
- [backend-role-based-field-redaction](../backend-role-based-field-redaction/SKILL.md) — the third authorization mechanism question 1 asks about.
- [client-student-area-pattern](../client-student-area-pattern/SKILL.md) — grounding material for `student.md`.
- [client-list-table-pattern](../client-list-table-pattern/SKILL.md) — grounding material for the list screens in every teacher doc.
