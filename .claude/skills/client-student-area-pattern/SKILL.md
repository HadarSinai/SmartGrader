---
name: client-student-area-pattern
description: "Use when adding or reviewing screens in the SmartGrader student area ('המסע שלי', routes under /my with StudentLayoutComponent + studentGuard): view-only lists with a personal status per row, studentId taken ONLY from AuthService.studentId() token claims (never from the URL), forkJoin of lessons/assignments + my submissions to derive personal status, 404-tolerant lesson-result lookups (catchError → 'בתהליך'), submission polling every 5s while PendingAi/ProcessingAi, and no-resubmit failure states. USE FOR: 'add a screen to the student area', 'add a /my route', 'show the student their status/grades/feedback', 'student-facing list or detail page'. NOT for teacher screens (see client-list-table-pattern), design tokens (client-design-token-rollout-pattern), or copy/validation fixes (client-flow-fix-implementation-pattern)."
---

# Client Student Area Pattern

The pattern for every screen in the SmartGrader student-facing area ("המסע שלי"), built per
[docs/ux/master-spec.md](../../../docs/ux/master-spec.md) §3. The area lives under the `/my/...`
route branch, wrapped by `StudentLayoutComponent` and protected by `studentGuard`.

Existing screens (reference implementations, all under `client/src/app/pages/my/`):

| Screen              | File                                                                                                  | Template type                    |
| ------------------- | ----------------------------------------------------------------------------------------------------- | -------------------------------- |
| השיעורים שלי        | [my-lessons-list.component.ts](../../../client/src/app/pages/my/my-lessons-list.component.ts)         | view-only list + personal status |
| התרגילים שלי בשיעור | [my-assignments-list.component.ts](../../../client/src/app/pages/my/my-assignments-list.component.ts) | view-only list + per-row CTA     |
| הגשת קוד            | [submit-code.component.ts](../../../client/src/app/pages/my/submit-code.component.ts)                 | form                             |
| פידבק AI            | [my-feedback.component.ts](../../../client/src/app/pages/my/my-feedback.component.ts)                 | detail + polling                 |
| הציונים שלי         | [my-grades.component.ts](../../../client/src/app/pages/my/my-grades.component.ts)                     | double view-only list            |

## When to Use

- Adding a new screen or route under `/my/...` (student area).
- Showing a student their own data: submissions, feedback, grades, lesson progress.
- Reviewing a PR that touches `client/src/app/pages/my/` or `student-layout.component.ts`.

## Iron Rules (differences from the teacher area)

1. **`studentId` comes ONLY from token claims** — `AuthService.studentId()`. Never a route
   param under `/my`. Guard the null case:
   ```typescript
   const studentId = this.auth.studentId();
   if (studentId === null) return;
   ```
2. **View-only lists** — no multi-select checkboxes, no ⋯ overflow menu, no create/edit/delete
   buttons. The only row actions are drill-down navigation or a CTA ("הגשת קוד" / "צפייה בפידבק").
3. **No resubmit** — failure states (`CompilationFailed`/`AiFailed`) are view-only with the
   explanatory sentence "אין צורך לעשות דבר — צוות ההוראה מטפל בהגשות שנכשלו." Only the teacher
   edits submissions (server enforces: `PUT` is Teacher-only).
4. **Teacher routes stay teacher-only** — `/students/:studentId/submissions*` and
   `.../lessons/:lessonId/result` carry `canActivate: [teacherGuard]`; the student experience
   lives only under `/my`.

## Workflow (adding a new student screen)

1. **Route**: add a child under the existing `/my` branch in
   [app.routes.ts](../../../client/src/app/app.routes.ts) (parent already has
   `StudentLayoutComponent` + `canActivate: [studentGuard]` — do not re-add guards per child).
2. **Component**: standalone component in `client/src/app/pages/my/`, template per the matching
   mother-template (list/form/detail) using `sg-page` → `p-card styleClass="sg-card"` →
   `sg-title`/`sg-h1`/`sg-h2`, breadcrumb back-link (`sg-breadcrumb-link` + `pi-arrow-right`).
3. **Personal status derivation** — combine general data with _my_ data via `forkJoin`:
   ```typescript
   forkJoin({
     assignments: this.assignmentsService.getByLesson(this.lessonId),
     submissions: this.submissionsService.getByStudent(studentId),
   });
   ```
   Pick the **latest** submission per assignment (sort by `submittedAt` desc). Status labels come
   from `STATUS_LABELS_HE` in
   [submission.model.ts](../../../client/src/app/models/submission.model.ts) — never hardcode new
   Hebrew status strings.
4. **404-tolerant lesson-result lookups** — a missing lesson result is an expected state
   ("בתהליך"), not an error. Per-call `catchError(() => of(null))` inside `forkJoin`, and note the
   interceptor already suppresses the toast for
   `404` on `/api/lesson-results/{studentId}/{lessonId}` (see
   [api-error.interceptor.ts](../../../client/src/app/core/http/api-error.interceptor.ts)).
5. **Polling for AI feedback** — while status is `PendingAi`/`ProcessingAi`, re-fetch every 5s
   with `setInterval`; stop on terminal status AND in `ngOnDestroy`; swallow transient errors
   silently (keep polling). Copy the `syncPolling`/`refreshSilently`/`stopPolling` trio from
   [my-feedback.component.ts](../../../client/src/app/pages/my/my-feedback.component.ts).
6. **Passing context between /my screens** — the submission DTO has no `lessonId`; pass it as a
   `queryParams: { lessonId }` on navigation (there is NO single-assignment endpoint the client
   can call — `assignmentsService.getById(id)` single-arg overload points to a dead URL).
7. **Code display/input** — always LTR: `sg-code-box` (`<pre>`) for read-only,
   `dir="ltr"` + monospace class for the textarea. Required-field validation follows the
   `methodName` inline pattern (`p-error` + `touched`).
8. **Verify**: `npm run build` (zero errors), RTL at 360/768/1280px, empty state (`pi-inbox` +
   Hebrew sentence), Hebrew gender-neutral copy, `aria-label` on icon-only buttons.

## Common Pitfalls

- Reading `studentId` from `route.paramMap` — breaks the security model; always token claims.
- Adding teacher affordances (edit/delete/checkboxes) to a student list — the area is view-only.
- Treating lesson-result 404 as an error — it's the normal "not completed yet" state.
- Forgetting `clearInterval` in `ngOnDestroy` — the poll keeps hitting the API after navigation.
- Inventing new status labels/colors — reuse `STATUS_LABELS_HE` + semantic `--status-*` tokens.

## See Also

- [client-list-table-pattern](../client-list-table-pattern/SKILL.md) — the full teacher list pattern
  (this skill is its reduced, view-only sibling).
- [client-design-token-rollout-pattern](../client-design-token-rollout-pattern/SKILL.md) — tokens.
- [client-flow-fix-implementation-pattern](../client-flow-fix-implementation-pattern/SKILL.md) —
  copy/validation conventions.
- [docs/ux/master-spec.md](../../../docs/ux/master-spec.md) §3 — the student-area UX spec.
