# Plan: Full UX Spec for SmartGrader Client Screens

## Goal

Produce comprehensive UX research artifacts (JTBD, User Journey Maps, Flow Specs, Accessibility
requirements) per the se-ux-ui-designer.agent.md methodology, in English, saved under `docs/ux/`,
covering:

1. Audit + improvement spec for Create/Delete (and general) UX gaps in the 4 existing feature areas:
   Lessons, Students, Assignments, Submissions.
2. Brand-new UX spec for the missing LessonResults feature (backend exists, no client UI yet).

Two personas apply: **Teacher** (admin: manages lessons/assignments/students, reviews & finalizes
grades) and **Student** (submits code directly via client, views own submissions/results).

## Context gathered

### Backend (server/)

- Entities: Lesson -> Assignment (N) -> Submission (N, by Student) ; Student -> LessonResult (per Lesson).
- SubmissionStatus enum: PendingAi -> ProcessingAi -> Done / AiFailed / CompilationFailed. AI grading
  is async (background job) — client must poll `GET /api/students/{studentId}/submissions/{submissionId}`.
- LessonResultController: `GET /api/lesson-results/{studentId}/{lessonId}` is LIVE.
  `POST /api/lesson-results/complete` is COMMENTED OUT in
  [server/Api/Controllers/LessonResultController.cs](server/Api/Controllers/LessonResultController.cs) —
  so "Complete/Finalize Lesson" UX flow can be designed but is BLOCKED on backend re-enabling this
  endpoint (flag as dependency, do not implement backend in this task).
- Full endpoint/DTO map for Lessons, Students, Assignments, Submissions, LessonResults already captured
  (see chat history) — includes nested routes `/lessons/{id}/assignments`, `/students/{id}/submissions`.

### Client (client/)

- Existing pages (all functional CRUD): lessons-list + lesson-form, students-list + student-form,
  assignments-list + assignment-form, submissions-list + submission-form + submission-detail, dashboard.
- Legacy duplicate/unused components found: `lessons.component.ts`, `students.component.ts` (old
  inline-template versions) — flag as cleanup candidates in audit findings, not deleted here.
- `client/spec.md` = existing **visual** design-token rollout spec (Figma-inspired), pilot = students-list.
  This UX task is complementary (behavior/flow spec), should reference and stay consistent with it.
- No `lesson-results.service.ts`, no model, no components exist client-side at all for LessonResults.
- Routing convention (from client.instructions.md): `/students/:studentId/submissions/...`,
  `/lessons/:lessonId/assignments/...` — new LessonResults screens must follow same nesting style,
  e.g. `/lessons/:lessonId/results` and/or `/students/:studentId/results`.
- Client conventions to respect in flow specs: PrimeNG `ConfirmationService` for deletes (never
  `window.confirm`), `MessageService` toasts for feedback, `ApiErrorInterceptor` for errors.

## Steps

### Phase 0 — Shared foundations (do first, blocks Phase 1)

1. Create `docs/ux/personas.md` — Teacher + Student persona detail (role, skill level, device/context,
   pain points) using JTBD Step-1 questions from the agent template. Grounds every subsequent artifact.
2. Create `docs/ux/accessibility-checklist.md` — reusable WCAG checklist (keyboard, screen reader,
   visual) adapted for the Hebrew/RTL PrimeNG client, per agent Step-5 template.

### Phase 1 — Per-feature UX audit + spec (5 tracks, run in parallel, each _depends on Phase 0_)

For each feature, invoke the **se-ux-ui-designer** agent to produce 3 files:
`docs/ux/{feature}-jtbd.md`, `docs/ux/{feature}-journey.md`, `docs/ux/{feature}-flow.md`.

**Tracks A–D (audit mode — existing screens)**: Lessons, Students, Assignments, Submissions.
Each audit must:

- Read the actual current component (list + form) to ground pain points in reality (not assumptions):
  e.g. is delete confirmed via `ConfirmationService`? are all backend DTO fields represented in the
  form? any dead/unnecessary UI elements? outdated visuals vs `client/spec.md` design tokens?
- JTBD framed around Create/Delete specifically (per user's stated concern: "screens aren't fully
  specified, missing things, some things are unnecessary, design feels dated").
- Journey map covering full lifecycle: list → create → edit → delete, with explicit pain points and
  opportunities for each stage.
- Flow spec proposing the improved Create/Delete UX (e.g., inline validation, confirm-delete dialog
  copy, empty/loading states, success/error toasts) — must stay within PrimeNG/PrimeFlex + existing
  routing conventions, no backend changes.

**Track E (new feature)**: LessonResults.

- JTBD for both personas: Teacher "finalize a student's lesson score", Student "view my final lesson
  result".
- Journey maps for both personas.
- Flow spec for new screens (e.g. `LessonResultsListComponent`, `LessonResultDetailComponent`,
  "Complete Lesson" action) — explicitly note the backend `POST /complete` dependency as a blocker/TODO
  before this flow can be wired to a real endpoint.

### Phase 2 — Consolidation (depends on Phase 1)

6. Create `docs/ux/README.md` — index of all artifacts + prioritized recommendations list:
   - Remove legacy duplicate components (`lessons.component.ts`, `students.component.ts`).
   - Roll out `client/spec.md` design tokens beyond the students-list pilot to all audited screens.
   - Backend dependency callout: re-enable `POST /api/lesson-results/complete` to unblock the
     "Complete Lesson" flow.
   - Cross-check every flow spec against `client.instructions.md` conventions (ConfirmationService,
     MessageService, routing pattern) for consistency.

## Relevant files

- `server/Api/Controllers/LessonResultController.cs` — confirms POST /complete is commented out.
- `client/spec.md` — existing visual design-token spec to reference/stay consistent with.
- `client/src/app/pages/{lessons,students,assignments,submissions}/*` — components to read during audit.
- `.github/agents/se-ux-ui-designer.agent.md` — methodology/template to follow for all artifacts.
- New: `docs/ux/*.md` (personas, accessibility-checklist, per-feature jtbd/journey/flow, README).

## Verification

- All 5 tracks produce 3 files each (15 files) + personas.md + accessibility-checklist.md + README.md
  = 18 files total under `docs/ux/`.
- Each audit doc references at least one concrete finding from reading the actual component file
  (not generic/assumed pain points).
- README cross-references and prioritizes findings; no contradictions between feature-level flow specs
  and `client.instructions.md` conventions.

## Decisions

- Output format: full UX research docs (JTBD + Journey + Flow + Accessibility), NOT a lightweight
  screen/field table — per user selection.
- Personas: both Teacher and Student are real UI users (student submits code directly).
- Language: English for all docs.
- Scope: improve Create/Delete UX across ALL 4 existing feature areas + add LessonResults spec (user
  selected option "1" = broadest scope).
- Backend changes (re-enabling POST /complete) are explicitly OUT of scope for this task — only flagged
  as a dependency/blocker in the LessonResults flow spec.
