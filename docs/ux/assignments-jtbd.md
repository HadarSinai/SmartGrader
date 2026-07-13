# Assignments — Jobs-to-be-Done

**Persona**: Teacher only (content-authoring surface — see [personas.md](./personas.md)).

## Job Statement

When I'm preparing a lesson, I want to define a coding assignment with clear instructions and test
cases, so the AI grader has everything it needs to fairly evaluate my students' submissions — and I
want the management screen itself to look and read as trustworthy as the rest of the app.

## Context

- **Situation**: Prep time, working lesson-by-lesson via `/lessons/:lessonId/assignments`.
- **Motivation**: Wants confidence that test cases are correctly captured (`TestCaseDto[]`) since they
  directly drive automated grading — a wrong test case silently produces wrong grades later.
- **Outcome**: A correct `AssignmentResponseDto` with `methodName` and `tests` that the AI grading
  pipeline can consume without ambiguity.

## Current Solution & Pain Points (grounded in current code)

Current implementation: [assignments-list.component.ts](../../client/src/app/pages/assignments/assignments-list.component.ts)

- [assignment-form.component.ts](../../client/src/app/pages/assignments/assignment-form.component.ts)
- [assignment-extended.model.ts](../../client/src/app/models/assignment-extended.model.ts).

* **Most severe language-consistency finding across the whole audit**: unlike Lessons and Students
  (whose _list_ pages are in Hebrew, only their form pages have the English-toast bug), the
  **entire Assignments feature — list and form — is 100% English-language**: toasts
  ("Error"/"Success"/"Failed to load assignments"), and even the delete-confirmation dialog itself
  (`"Are you sure you want to delete \"{title}\"?"`, header `"Confirm Delete"`, using PrimeNG's default
  English "Yes"/"No" buttons since no `acceptLabel`/`rejectLabel` is set). In an otherwise Hebrew/RTL
  app, this feature reads as if it belongs to a different, unfinished product.
* **Columns that render broken for every row, always**: the list table is typed as `AssignmentExtended`
  (`difficulty`, `completionRate`, `averageScore`, `dueDate`, `maxScore`, `status`), but
  `assignmentsService.getByLesson()` returns real `AssignmentResponseDto[]` from the backend — which
  has **none of those fields**. The template's `*ngIf="assignment.difficulty"` fallback correctly shows
  "—" for some cells, but the "דדליין" (`dueDate | date`), "נקודות" (`maxScore`), completion-rate
  progress bar, and average-score cells render as empty/blank/zero-width for _every single assignment_,
  since those properties are always `undefined`. This is a more widespread version of the Students
  fake-data problem: not fabricated wrong numbers, but half a table of dead, always-empty columns.
* **A genuinely good pattern already exists here — worth reusing elsewhere**: the `methodName` field in
  `assignment-form.component.ts` has real inline validation feedback (`<small class="p-error"
*ngIf="form.get('methodName')?.invalid && ...">שם המתודה הוא שדה חובה</small>`) — this is exactly the
  fix Lessons and Students forms are missing. It should become the standard pattern rolled out
  everywhere, not just kept here. Note `title` in this same form still lacks the same treatment,
  though, so even here it's inconsistent field-to-field.
* **No delete-confirmation copy issue with gendered Hebrew** (since it isn't in Hebrew at all yet), but
  once translated it must follow the gender-neutral standard set for Lessons/Students.

## Consequence of the Gaps

- A teacher moving from Lessons/Students into Assignments experiences a jarring language switch
  mid-task, and sees a table where most columns are permanently blank — undermining confidence that the
  screen is complete or that her assignment data (including the test cases that matter for grading) was
  saved correctly.
