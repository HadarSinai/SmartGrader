# Submissions — Jobs-to-be-Done

**Persona**: Both — **Student** (submits/resubmits code, checks grading result) and **Teacher**
(reviews submissions, reads AI feedback). See [personas.md](./personas.md).

## Job Statement (Student)

When I've finished writing code for an assignment, I want to submit it and clearly know whether it's
still being graded or already has a result, so I don't waste time wondering if something is broken.

## Job Statement (Teacher)

When I check in on a student's progress, I want to quickly see the real status and score of each
submission, so I know who needs help without misreading a stale or ambiguous status label.

## Context

- AI grading is asynchronous: `PendingAi → ProcessingAi → Done / AiFailed / CompilationFailed`
  (per `server/Domain/Entities/Submission.cs`). The client has to represent this waiting period clearly.
- **Both personas share the same list/detail screens** — there's no student-only vs teacher-only view
  distinction in the current client, so any status/label problems affect both equally.

## Current Solution & Pain Points (grounded in current code)

Current implementation: [submissions-list.component.ts](../../client/src/app/pages/submissions/submissions-list.component.ts)

- [submission-form.component.ts](../../client/src/app/pages/submissions/submission-form.component.ts)
- [submission-detail.component.ts](../../client/src/app/pages/submissions/submission-detail.component.ts).

* **Missing Delete entirely — the most direct match to this audit's stated focus on Create/Delete
  UX**: unlike every other feature (Lessons, Students, Assignments), `submissions-list.component.ts` has
  **no delete button, no `confirmDelete()`/`deleteSubmission()` method, and does not even import
  `ConfirmationService`** — despite the backend fully supporting
  `DELETE /api/students/{studentId}/submissions/{submissionId}`. A student or teacher who submitted
  something by mistake, or wants to remove a duplicate/test submission, has no way to do so from the UI.
* **Raw enum values leak into the list UI**: `getStatusLabel()` only special-cases
  `CompilationFailed` → `"Compile Error"`; every other status (`PendingAi`, `ProcessingAi`, `Done`,
  `AiFailed`) is returned **verbatim** as the C#-style enum string, so the list table literally displays
  "PendingAi" / "ProcessingAi" / "AiFailed" to the end user instead of a human label.
* **No polling / auto-refresh for async grading**: neither the list nor the detail component has any
  interval/polling logic. A student who submits code and stays on the page has no way to see the status
  change from `PendingAi`/`ProcessingAi` to `Done` without manually navigating away and back (or
  reloading). This directly contradicts the persona's stated need to know grading finished.
* **Mixed-language status labels**: `submission-detail.component.ts` hardcodes English tag values
  ("Done", "Pending AI", "Processing AI", "AI Failed", "Compile Error", "Unknown") even though every
  other string on the same page is Hebrew ("סטודנט", "תרגיל", "נשלח", "סטטוס", "ציון").
* **Same English-toast bug as other features**: `submissions-list.component.ts` uses
  `"Error"`/`"Failed to load submissions"`.

## Consequence of the Gaps

- The missing delete capability is a functional gap, not just a polish issue — it's the one feature area
  where the user's stated "מחיקה" (deletion) concern is fully unaddressed today.
- Raw enum leakage ("PendingAi" shown as literal text) is the single worst readability bug found in
  this entire audit — it's technical/internal naming exposed directly to end users.
