# Plan: Student Area "המסע שלי" — 5 Screens Wired to the Real API

## TL;DR

Build the student-facing area ("המסע שלי") from [docs/ux/master-spec.md](../../docs/ux/master-spec.md) §3 as five Angular screens under a new `/my/...` route branch: My Lessons → My Assignments in Lesson → Submit Code → AI Feedback → My Grades. Auth is already fully implemented (JWT, `AuthService` signals, `authInterceptor`, guards) — all screens call the **real API** with `studentId` taken **only from the token claims** (`AuthService.studentId()`), never from the URL. Login screen is OUT of scope (handled by another agent). No resubmit for students — feedback failure states are view-only (only the teacher edits submissions).

## Decisions (agreed with Hadar, 14.07.2026)

| Topic              | Decision                                                                                                                                                                    |
| ------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Data source        | Real API — no mock. Auth (Phases 1–3 of [docs/auth-plan.md](../../docs/auth-plan.md)) is already implemented and verified in code                                           |
| Scope              | All 5 screens from master-spec §3                                                                                                                                           |
| Login screen       | Excluded — another agent handles it                                                                                                                                         |
| Resubmit           | **Students cannot resubmit.** Feedback failure states (compilation/AI error) are view-only; only the teacher edits submissions (`PUT` is Teacher-only on the server)        |
| Old student routes | **Option A**: add `teacherGuard` to `/students/:studentId/submissions*` and `/students/:studentId/lessons/:lessonId/result` — the student experience lives only under `/my` |
| `studentId`        | Always from token claims via `AuthService.studentId()`; never in the `/my` URLs (server also enforces via `IsAllowedForStudent`)                                            |

## Verified API surface available to a Student

| Screen         | Endpoint                                                                  | Authorization                                                  |
| -------------- | ------------------------------------------------------------------------- | -------------------------------------------------------------- |
| My Lessons     | `GET /api/lessons`                                                        | Any authenticated                                              |
| My Assignments | `GET /api/lessons/{lessonId}/assignments`                                 | Any authenticated                                              |
| My submissions | `GET /api/students/{studentId}/submissions`                               | Self-only (claim check)                                        |
| Submit code    | `POST /api/students/{studentId}/submissions` `{assignmentId, sourceCode}` | Self-only (explicit student exception in `StudentsController`) |
| Feedback       | `GET /api/students/{studentId}/submissions/{submissionId}`                | Self-only                                                      |
| Final score    | `GET /api/lesson-results/{studentId}/{lessonId}`                          | Any authenticated (404 when none)                              |

Existing client services cover everything: `LessonsService`, `AssignmentsService`, `SubmissionsService`, `LessonResultsService`, `AuthService`. No server changes needed.

## Spec & pattern sources

- [docs/ux/master-spec.md](../../docs/ux/master-spec.md) §3 (student-area flow, entry/exit points), §4 (three mother-templates), §5 (shared patterns: empty/loading states, toasts, date `dd.MM.yy HH:mm`, semantic status colors).
- [client/spec.md](../../client/spec.md) — design tokens (`--radius/--shadow/--space/--text`, `sg-*` classes), RTL at 360/768/1280px, no parallel colors.
- Skills: `client-list-table-pattern` (list anatomy — reduced, view-only variant), `client-flow-fix-implementation-pattern` (inline validation via the `methodName` pattern, Hebrew gender-neutral copy), `client-design-token-rollout-pattern`.
- Status labels: reuse `STATUS_LABELS_HE` from [client/src/app/models/submission.model.ts](../../client/src/app/models/submission.model.ts).
- Template references: [lessons-list.component.ts](../../client/src/app/pages/lessons/lessons-list.component.ts) (list), [assignment-form.component.ts](../../client/src/app/pages/assignments/assignment-form.component.ts) (inline validation), [app-layout.component.ts](../../client/src/app/components/layout/app-layout.component.ts) (layout base), submission-detail (unified status area + `sg-code-box`).

## Steps

### Phase 1 — Routing & layout

1. **Student layout** — new `client/src/app/components/layout/student-layout.component.ts`: same `sg-shell`/`--app-bg` styling as the teacher layout, minimal topbar — brand, nav "השיעורים שלי" / "הציונים שלי", real user name from `AuthService.fullName()`, logout button (`auth.logout()` → `/login`). No teacher nav/footer links.
2. **Routes** — in [app.routes.ts](../../client/src/app/app.routes.ts) add a `/my` parent under `StudentLayoutComponent` with `canActivate: [studentGuard]` (guard already exists in [auth.guards.ts](../../client/src/app/core/guards/auth.guards.ts) but is unused):
   - `my/lessons` · `my/lessons/:lessonId/assignments` · `my/lessons/:lessonId/assignments/:assignmentId/submit` · `my/submissions/:submissionId` · `my/grades`
3. **Option A hardening** — add `canActivate: [teacherGuard]` to the old routes `students/:studentId/submissions`, `.../submissions/new`, `.../submissions/:submissionId`, and `students/:studentId/lessons/:lessonId/result`.
4. **`homeRoute()`** in [auth.service.ts](../../client/src/app/services/auth.service.ts): student → `["/my", "lessons"]` (replaces the current redirect to the teacher-template submissions page).

### Phase 2 — Screens (list → drill-down order; 5 standalone components under `client/src/app/pages/my/`)

5. **My Lessons** — `my-lessons-list.component.ts` (list template, reduced/view-only: no checkboxes, no ⋯ menu, no create): lesson name, subject, date (`dd.MM.yy HH:mm`), personal status tag — `GET /api/lessons` + per-lesson `LessonResultsService.getResult(studentId, lessonId)` via `forkJoin` with per-call `catchError` (404 → "בתהליך"; `isComplete` → "הושלם" + final score, success color). Row action → my assignments. Empty state (`pi-inbox` + "אין שיעורים להצגה.") and `[loading]`.
6. **My Assignments in Lesson** — `my-assignments-list.component.ts`: breadcrumb back to `/my/lessons`; `forkJoin` of lesson assignments + my submissions; per assignment derive personal status (לא הוגש / ממתין לבדיקה / בבדיקה / נבדק + ציון / שגיאת קומפילציה / שגיאת בדיקה — icon + semantic color via `STATUS_LABELS_HE`); bonus chip where relevant; CTA per row: "הגשת קוד" (when no submission) or "צפייה בפידבק" (when one exists, latest submission).
7. **Submit Code** — `submit-code.component.ts` (form template): assignment context header (title, method name), `sourceCode` textarea — LTR, monospace, `sg-code-box`-style; required with inline `p-error` after `touched` (the `methodName` pattern); actions "ביטול" (dirty → "שינויים שלא נשמרו" confirm) + "הגשה". On submit: `POST` → success toast "הקוד נשלח לבדיקה" → navigate to `/my/submissions/{id}`.
8. **AI Feedback** — `my-feedback.component.ts` (detail template): key-value grid (assignment, submitted at, score) + **unified status area** — one box switching by status: בבדיקה (amber, `pi-spinner`) / נבדק (success + score + AI comments) / שגיאת קומפילציה (terracotta + compiler output in `sg-code-box`) / שגיאת בדיקה. **Real polling**: while `PendingAi`/`ProcessingAi`, re-fetch every ~5s (AiWorker processes in background); stop on terminal status and on destroy. **No resubmit CTA** — failure states are view-only with an explanatory sentence (המורה מטפל/ת בהגשות שנכשלו). Back link to the lesson's assignments.
9. **My Grades** — `my-grades.component.ts` (list template, view-only): my submissions (assignment, lesson, date, status, score) + a final-scores section per completed lesson (lesson-results; skip 404s). Empty states per section.

### Phase 3 — Verification

10. `npm run build` in `client/` — zero errors.
11. Manual E2E as a student: login → lands on `/my/lessons` → lesson → assignment → submit code → toast → feedback (watch polling flip amber → graded) → grades. Then attempt `/students/{id}/submissions` directly → redirected (Option A).
12. RTL at 360/768/1280px per [client/spec.md](../../client/spec.md); [accessibility-checklist.md](../../docs/ux/accessibility-checklist.md): labels bound to inputs, Hebrew aria-labels on icon buttons, status = color + icon, logical tab order.

## Constraints

- Hebrew-only, gender-neutral copy; PrimeNG `MessageService`/`ConfirmationService`; HTTP errors via existing `ApiErrorInterceptor`.
- Design tokens and `sg-*` classes only — no new ad-hoc colors/classes (additions to `styles.css` only if a truly shared style is missing).
- No server changes. Teacher screens untouched except the guard additions in step 3.

## Out of scope

- Login/register screens (another agent) · resubmit for students · notifications · CSV import · any backend work.
