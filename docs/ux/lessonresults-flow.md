# LessonResults — Flow Specification

**Scope**: Brand-new client feature: model, service, and 2 components, following the exact conventions
already used across Lessons/Students/Assignments/Submissions
(per [client.instructions.md](../../.github/instructions/client.instructions.md)). The "finalize" write
action is spec'd but gated behind a backend dependency (see below) — do not wire it to a real endpoint
until that dependency is resolved.

## New Files (routing & structure, following existing conventions)

- `client/src/app/models/lesson-result.model.ts` — `LessonResultResponseDto` matching the backend DTO
  exactly (`id`, `studentId`, `lessonId`, `finalScore: number | null`, `isComplete: boolean`,
  `calculatedAt: string`, `totalAssignments: number`, `completedAssignments: number`), plus
  `CompleteLessonRequestDto` (`studentId`, `lessonId`, `finalScore`) for future use.
- `client/src/app/services/lesson-results.service.ts` — one method now:
  `getResult(studentId: number, lessonId: number): Observable<LessonResultResponseDto>` calling
  `GET /api/lesson-results/{studentId}/{lessonId}` via `ApiClient`. A `complete()` method should be
  added to the service **only once the backend endpoint is re-enabled** — do not call a commented-out
  endpoint from the client.
- Routes (following the existing nested-resource pattern):
  - `/lessons/:lessonId/results` — Teacher-facing aggregate view (all students' results for a lesson).
  - `/students/:studentId/submissions` (existing) gains a link to
    `/students/:studentId/lessons/:lessonId/result` — Student-facing single-result view.

## User Flow: Teacher Views Lesson Results (Aggregate)

**Entry Point**: `/lessons` list → a new "תוצאות" action/link per lesson row (alongside the existing
"תרגילים" shortcut button pattern already used in `lessons-list.component.ts`) → `/lessons/:lessonId/results`.

**Flow Steps**:

1. `LessonResultsListComponent` loads the lesson's roster (existing `StudentsService.getAll()` or a
   lesson-scoped student list, whichever the app already supports) and, per student, calls
   `GET /api/lesson-results/{studentId}/{lessonId}`.
2. Render a table: student name, `completedAssignments / totalAssignments` (reuse the "fraction" visual
   pattern already present in `students-list.component.html`'s "הוגש בזמן" column — but backed by real
   data this time, not demo values), `finalScore` (or "טרם נקבע" / "not yet set" if `null`), and an
   `isComplete` status tag.
3. Empty/loading states follow the same `p-table` `emptymessage`/`[loading]` pattern as every other list
   page.

**Exit Points**: Teacher clicks a row → drills into that student's detail (Stage below) or back to
`/lessons`.

## User Flow: Teacher Finalizes a Student's Result (spec'd, backend-blocked)

**Entry Point**: A "סיום שיעור" (finalize) button per ready row (`completedAssignments ===
totalAssignments && !isComplete`).

**Flow Steps**:

1. Click opens a small dialog (`p-dialog`) to enter/confirm a `finalScore` (0–100), reusing
   `p-inputNumber` as already used for `bonusValue` in `assignment-form.component.ts`.
2. **[Blocked]** Submitting should call a `complete()` method on `LessonResultsService` →
   `POST /api/lesson-results/complete`. **This endpoint is currently commented out** in
   [LessonResultController.cs](../../server/Api/Controllers/LessonResultController.cs).
3. **Until the backend is unblocked**: render the "סיום שיעור" button as disabled with a tooltip
   ("פונקציונליות זו תופעל בקרוב") rather than omitting the button entirely — this keeps the spec
   future-proof and signals the feature is coming, not broken.

**Exit Points (once unblocked)**: Success → toast "התוצאה נשמרה בהצלחה", row updates to `isComplete:
true`. Error → toast "שמירת התוצאה נכשלה", dialog stays open for retry.

## User Flow: Student Views Their Own Lesson Result

**Entry Point**: A "התוצאה שלי" link from the student's own submissions list (or lesson context) →
`/students/:studentId/lessons/:lessonId/result`.

**Flow Steps**:

1. `LessonResultDetailComponent` calls `GET /api/lesson-results/{studentId}/{lessonId}`.
2. Renders progress (`completedAssignments`/`totalAssignments`, e.g. as the same fraction/progress-bar
   visual pattern used elsewhere) and, if `isComplete`, the `finalScore`; if not yet complete, an
   honest status message distinguishing "still submitting assignments" from "all done, awaiting
   teacher's final sign-off" (see Journey map Stage 2 for exact copy).

**Exit Points**: Back to submissions list.

## Design Principles for This Flow

1. **Reuse, don't reinvent**: model/service/component naming and the fraction/progress-bar visual
   pattern should mirror what already exists (`students-list.component.html`'s fraction display,
   `assignment-form.component.ts`'s `p-inputNumber` usage) rather than introducing new UI idioms.
2. **Never silently fail on a disabled feature**: the finalize action must be visibly present but
   honestly disabled with an explanatory tooltip until the backend dependency is resolved — never a
   button that does nothing on click, and never hidden entirely (hiding it would make the feature
   invisible to teachers who need to know it's coming).
3. **All new strings in Hebrew from day one** — this is a new feature, so there's no legacy
   English-toast debt to inherit; hold it to the same bar the rest of this audit is trying to reach.
4. **Follow the existing nested-route convention** exactly (`/lessons/:lessonId/...`,
   `/students/:studentId/...`) per `client.instructions.md`.

## Backend Dependency (explicitly out of scope for this UX task)

Re-enabling `POST /api/lesson-results/complete` (uncommenting the controller action, verifying
`CompleteLessonCommand`/`CompleteLessonHandler`/`LessonResultProfile` are wired) is a **backend** change
tracked separately — this flow spec assumes it will eventually be re-enabled and designs the UI to be
ready for that, but does not implement it here.

## Accessibility

See [accessibility-checklist.md](./accessibility-checklist.md). New for this feature: the disabled
"finalize" button's tooltip explanation must also be exposed via `aria-describedby` (not just a visual
`pTooltip`), so screen-reader users understand _why_ it's disabled, not just that it is.
