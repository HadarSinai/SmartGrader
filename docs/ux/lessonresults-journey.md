# LessonResults — User Journey Maps

**Personas**: Teacher and Student (separate journeys below) — see [personas.md](./personas.md).
Net-new feature; journeys describe the intended experience once built.

---

## Teacher Journey: Reviewing & (eventually) Finalizing a Lesson's Results

### Stage 1: Awareness

**Doing**: Finishes teaching a lesson, wants to know how the class did overall.
**Thinking**: "Is everyone graded yet? Who's still pending?"
**Feeling**: 😐 Currently has no single place to check this — has to open each student's submissions
individually.
**Pain point**: No aggregate, per-lesson view exists.
**Opportunity**: A "תוצאות שיעור" (lesson results) view reachable from `/lessons/:lessonId`, listing
every enrolled student with their `completedAssignments`/`totalAssignments` progress and current
`finalScore`/`isComplete` state.

### Stage 2: Exploration

**Doing**: Scans the per-student progress list for the lesson.
**Thinking**: "This student is 2/5 — not ready to finalize yet. This one is 5/5 — ready."
**Feeling**: 🙂 Confident once the aggregate view exists, since `totalAssignments`/`completedAssignments`
are already computed server-side.
**Opportunity**: Visually distinguish "ready to finalize" (completedAssignments === totalAssignments)
from "still in progress" students, e.g. with a `p-tag` severity difference.

### Stage 3: Action (blocked today)

**Doing**: Wants to click "סיום שיעור" (finalize) for a ready student to set their `finalScore`.
**Thinking**: "I clicked finalize — why did nothing happen / why is this button disabled?"
**Feeling**: 😤 Blocked — `POST /api/lesson-results/complete` is commented out server-side.
**Pain point**: The finalize action cannot be wired to a real endpoint yet.
**Opportunity**: Build the finalize **UI** now (dialog/form to enter or confirm a final score) but gate
the actual submit button behind a clear, honest state — e.g. disabled with a tooltip
"פונקציונליות זו תופעל בקרוב" (this functionality will be enabled soon) — rather than silently failing
or hiding the feature. This keeps the UX spec ready to wire up the moment the backend endpoint is
re-enabled, without shipping a broken/silently-failing button today.

### Stage 4: Outcome

**Doing**: Once unblocked (future), sees the student's `LessonResult` marked complete with a final
score.
**Success metric**: Every student in a lesson has either a finalized score or a clearly visible
"still in progress" state, checkable in one screen instead of per-submission cross-referencing.

---

## Student Journey: Viewing My Own Lesson Result

### Stage 1: Awareness

**Doing**: Finishes a lesson's assignments, wonders about their overall result (not just individual
submission scores).
**Thinking**: "I know my score on each assignment — but what's my overall result for this lesson?"
**Feeling**: 😐 Currently no way to answer this from the client at all.
**Pain point**: Zero UI surfacing `LessonResult` data to a student.
**Opportunity**: A "התוצאה שלי" (my result) link from the student's own submissions view or a lesson
context, calling the already-live `GET /api/lesson-results/{studentId}/{lessonId}`.

### Stage 2: Exploration

**Doing**: Opens their lesson result view.
**Thinking**: "Am I done with everything? What's my score so far?"
**Feeling**: 🙂 Reassured once they see `completedAssignments`/`totalAssignments` progress and
(if `isComplete`) a `finalScore`.
**Pain point (until finalize is unblocked)**: If `isComplete` is always `false` (since nothing can call
`POST /complete` yet), students may see "still in progress" indefinitely even after finishing every
assignment — this must be communicated honestly, not as if it's their fault.
**Opportunity**: If `completedAssignments === totalAssignments` but `isComplete` is `false`, show a
message like "כל התרגילים הוגשו — ממתין לאישור סופי מהמורה" (all assignments submitted — awaiting
teacher's final sign-off), so the student isn't confused about who's responsible for the next step.

### Stage 3: Outcome

**Doing**: Once `isComplete` is `true` (future, once backend unblocked), sees their finalized score.
**Success metric**: A student can always tell, in one glance, whether they're still working through a
lesson, done and waiting on the teacher, or fully finalized with a score.
