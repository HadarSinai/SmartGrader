# SmartGrader Client — UX Specification Index

This folder contains the full UX research/spec artifact set produced for the SmartGrader Angular
client, covering an audit of the 4 existing feature areas plus a net-new spec for the missing
LessonResults feature. Methodology follows
[.github/agents/se-ux-ui-designer.agent.md](../../.github/agents/se-ux-ui-designer.agent.md)
(JTBD → Journey Map → Flow Spec → Accessibility).

## Foundations

- [personas.md](./personas.md) — Teacher (admin) and Student personas, used across every artifact.
- [accessibility-checklist.md](./accessibility-checklist.md) — reusable WCAG/RTL checklist referenced
  by every flow spec.

## Per-Feature Artifacts

| Feature             | JTBD                                             | Journey                                                | Flow                                             |
| ------------------- | ------------------------------------------------ | ------------------------------------------------------ | ------------------------------------------------ |
| Lessons             | [lessons-jtbd.md](./lessons-jtbd.md)             | [lessons-journey.md](./lessons-journey.md)             | [lessons-flow.md](./lessons-flow.md)             |
| Students            | [students-jtbd.md](./students-jtbd.md)           | [students-journey.md](./students-journey.md)           | [students-flow.md](./students-flow.md)           |
| Assignments         | [assignments-jtbd.md](./assignments-jtbd.md)     | [assignments-journey.md](./assignments-journey.md)     | [assignments-flow.md](./assignments-flow.md)     |
| Submissions         | [submissions-jtbd.md](./submissions-jtbd.md)     | [submissions-journey.md](./submissions-journey.md)     | [submissions-flow.md](./submissions-flow.md)     |
| LessonResults (new) | [lessonresults-jtbd.md](./lessonresults-jtbd.md) | [lessonresults-journey.md](./lessonresults-journey.md) | [lessonresults-flow.md](./lessonresults-flow.md) |

## Prioritized Recommendations (cross-feature)

Ordered roughly by severity/impact, based on concrete findings from reading the real components (not
assumptions):

1. **[Students] Remove fabricated per-student metrics** — `toVm()` in `students-list.component.ts`
   invents `gradeAvg`/`attendance`/`onTimeFraction`/`stars` from static demo arrays and displays them as
   if real. This is the single most serious finding: a teacher could make decisions based on numbers
   that aren't tied to the actual student at all. See [students-flow.md](./students-flow.md).
2. **[Submissions] Add the missing Delete action** — the only feature area with zero delete capability
   in the client despite the backend endpoint already existing. Directly addresses the stated
   Create/Delete UX focus of this audit. See [submissions-flow.md](./submissions-flow.md).
3. **[Submissions] Fix raw enum leakage in status labels** — `getStatusLabel()` shows literal C# enum
   strings (`PendingAi`, `ProcessingAi`, `AiFailed`) to end users instead of Hebrew labels. See
   [submissions-flow.md](./submissions-flow.md).
4. **[Assignments] Full localization** — the only feature area that is 100% English (list, form, and
   the delete-confirmation dialog), including PrimeNG's default English Yes/No buttons. See
   [assignments-flow.md](./assignments-flow.md).
5. **[Assignments] Remove dead/always-empty columns** — `AssignmentExtended` fields (`difficulty`,
   `status`, `dueDate`, `maxScore`, `completionRate`, `averageScore`) are declared but never populated
   by the real `AssignmentResponseDto`, leaving 5 of 9 table columns permanently blank. See
   [assignments-flow.md](./assignments-flow.md).
6. **[Cross-cutting] Gendered Hebrew confirm-dialog copy** — Lessons and Students both hardcode feminine
   grammar (`"בטוחה"`, `"מחקי"`) in delete confirmations; Students' own success toast already uses
   gender-neutral phrasing, so this is also an internal inconsistency, not just a correctness issue.
   Fix across [lessons-flow.md](./lessons-flow.md) and [students-flow.md](./students-flow.md).
7. **[Cross-cutting] English-hardcoded form-page toasts** — Lessons, Students, and Assignments' form
   components all hardcode `"Error"`/`"Success"`/`"Failed to ..."` in English while their list pages are
   in Hebrew. Fix in each feature's flow spec.
8. **[Cross-cutting] Inconsistent inline field validation** — only `assignment-form.component.ts`'s
   `methodName` field has an inline required-field error message; every other required field across
   every form only disables the Save button with no explanation. Roll the existing `methodName` pattern
   out everywhere (see [assignments-flow.md](./assignments-flow.md) for the reference implementation).
9. **[Submissions] No async-grading feedback** — no polling/auto-refresh and a non-animated spinner icon
   leave students unsure whether `ProcessingAi` is stuck or working. See
   [submissions-flow.md](./submissions-flow.md).
10. **[LessonResults] Net-new feature** — fully computed on the backend (`totalAssignments`/
    `completedAssignments`/`finalScore`) but zero client UI exists. Highest-value net-new addition since
    the backend work is already done. See [lessonresults-flow.md](./lessonresults-flow.md).

## Backend Dependency Callout

The LessonResults "finalize" flow depends on re-enabling `POST /api/lesson-results/complete`, which is
currently commented out in
[server/Api/Controllers/LessonResultController.cs](../../server/Api/Controllers/LessonResultController.cs).
This is a **backend** change, tracked separately and explicitly out of scope for this UX spec — the
[lessonresults-flow.md](./lessonresults-flow.md) document designs the UI to be ready for it (disabled
button + tooltip) without implementing it.

## Codebase Hygiene Notes (non-UX, flagged for a separate cleanup pass)

- `client/src/app/pages/lessons/lessons.component.ts` — unused legacy duplicate of
  `lessons-list.component.ts`.
- `client/src/app/pages/students/students.component.ts` — unused legacy duplicate of
  `students-list.component.ts`.

## Consistency Check vs. `client.instructions.md`

All proposed flows in this spec set stay within the existing conventions: PrimeNG-only UI,
`ConfirmationService` for every delete (never `window.confirm`), `MessageService` toasts for feedback,
`ApiErrorInterceptor`-routed error messages, and the existing nested-route pattern
(`/lessons/:lessonId/...`, `/students/:studentId/...`). No new flows, no service/API contract changes
beyond the net-new `lesson-results.service.ts` (which mirrors the shape of every other entity service).
