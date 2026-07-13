# LessonResults — Jobs-to-be-Done

**Personas**: Both — **Teacher** (finalizes a student's lesson score) and **Student** (views their own
final result). See [personas.md](./personas.md). This is a **brand-new client feature** — no
`lesson-result.model.ts`, service, or components exist today (confirmed: no matches for
`lesson-result` anywhere under `client/src/app/`).

## Job Statement (Teacher)

When a lesson is finished and all assignments have been graded, I want to review each student's
progress and finalize their overall lesson score, so I have a clean, closed record I can reference
later without re-deriving it from individual submissions every time.

## Job Statement (Student)

When I've completed a lesson's assignments, I want to see my final result for that lesson in one place,
so I know how I did overall — not just assignment-by-assignment.

## Context

- **Backend status**: `GET /api/lesson-results/{studentId}/{lessonId}` is **live** and returns
  `LessonResultResponseDto` (`finalScore`, `isComplete`, `calculatedAt`, `totalAssignments`,
  `completedAssignments`). `POST /api/lesson-results/complete` is **commented out** in
  [server/Api/Controllers/LessonResultController.cs](../../server/Api/Controllers/LessonResultController.cs)
  — so a "view result" flow is fully supportable today, but a "finalize/complete result" flow is
  **blocked** until that endpoint is re-enabled server-side (explicitly out of scope for this UX task —
  flagged as a dependency, see Flow spec).
- **Situation (Teacher)**: End of lesson, reviewing whether every student's assignments are graded and
  deciding on (eventually) a final score.
- **Situation (Student)**: Checking back after a lesson to see an overall result, likely from the same
  place they already check individual submission status.

## Gaps vs. Current Client (net-new feature — no existing UI to audit)

- No way today for a student to see anything beyond `lessonResultsCount` (a bare number) surfaced on
  `StudentResponseDto` in the students list — there's no drill-down to see _which_ lesson, what score,
  or completion progress.
- No way today for a teacher to see per-student progress within a lesson (e.g., "3 of 5 assignments
  graded") without manually cross-referencing the Submissions list per student per assignment.
- Because `POST /complete` is disabled, there is currently no path to actually produce a _new_
  finalized score from the client even once UI exists — the UI can only ever call the read-only `GET`
  until the backend re-enables the write endpoint.

## Consequence

- Both personas currently have no visibility into this data at all, despite the backend already
  computing and exposing `totalAssignments`/`completedAssignments` progress — a fully-computed feature
  with zero UI. This is the highest-value net-new addition in this audit precisely because the backend
  work already exists; it's a pure client gap.
