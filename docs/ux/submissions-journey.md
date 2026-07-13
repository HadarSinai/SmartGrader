# Submissions — User Journey Map

**Personas**: Student (submit/track) and Teacher (review) — see [personas.md](./personas.md).
**Scope**: Full lifecycle — List → Create → (missing) Delete, plus the async-grading waiting
experience — grounded in the current `submissions-list`/`submission-form`/`submission-detail`
components.

## Stage 1: List / Discover

**Doing**: Opens `/students/:studentId/submissions`, scans a table/list of past submissions with a
status tag per row.
**Thinking (Student)**: "Wait, what does 'PendingAi' mean? Is that an error?"
**Thinking (Teacher)**: "I can't tell at a glance which of these are actually done vs. still running."
**Feeling**: 😕 Confused — raw enum strings (`PendingAi`, `ProcessingAi`, `AiFailed`) appear as literal
row labels for anything except `CompilationFailed`.
**Pain points**: No human-readable status mapping; no way to delete a row; no visual indication that a
`ProcessingAi` row might update soon (no polling, no "refresh" affordance).
**Opportunity**: Full Hebrew status label map (see Flow spec) + a manual "רענון" (refresh) button at
minimum, ideally short-interval polling while any row is `PendingAi`/`ProcessingAi`.

## Stage 2: Create (Submit Code)

**Doing (Student)**: Clicks "הגשה חדשה", picks lesson → assignment from dropdowns, pastes code into
the `p-editor`, submits.
**Thinking**: "Okay, I clicked submit — now what? Is it graded yet?"
**Feeling**: 🙂 The dropdown-cascade UX (lesson → assignment) is clear and well-built; 😰 anxious
immediately after submitting, since there's no in-page indication of grading progress.
**Pain points**: After `POST` succeeds, the flow navigates away (per typical pattern) with no explicit
"your submission is now queued for grading" messaging tailored to the async nature of this specific
action (as opposed to a generic "created successfully" toast that undersells what happens next).
**Opportunity**: Success toast/message specific to submissions: "ההגשה נשלחה ונמצאת בתור לבדיקה" (queued
for grading), setting the right expectation up front.

## Stage 3: Waiting for AI Grading

**Doing**: Student or teacher returns to the detail page or list to check status.
**Thinking**: "Is this stuck? Should I resubmit?"
**Feeling**: 😰 Uncertain — `ProcessingAi` has a Hebrew label on the detail page ("Processing AI" is
actually still English text inside a Hebrew-labeled tag) but with no spinner/live update, it's
indistinguishable from a frozen page.
**Pain points**: No animated/spinner treatment for `ProcessingAi` beyond a static `pi-spinner` icon
(the icon itself doesn't actually spin unless CSS animation is applied); no auto-refresh.
**Opportunity**: Add a spin animation class to the `pi-spinner` icon and short-interval polling
(e.g. every 5-10s while status is `PendingAi`/`ProcessingAi`) on the detail page specifically, since
that's where a student is most likely to be actively waiting.

## Stage 4: Result — Done / Failed

**Doing**: Sees final score + AI comments (`Done`), or a compile-error box (`CompilationFailed`), or an
AI-failure message (`AiFailed`).
**Thinking**: "Good, I can see my score and feedback." / "What do I do about a compile error — can I
just fix and resubmit?"
**Feeling**: 🙂 Done state is genuinely well laid out (score, comments, compile-error box all present
and readable); 😕 no explicit resubmit call-to-action is surfaced from the failed/compile-error states
even though `PUT` (resubmit) exists on the backend and an edit route exists in the client.
**Pain points**: Failure states don't visually point the user toward "עריכה" (edit/resubmit) as the
obvious next action, even though it's one click away in the header.
**Opportunity**: Add a prominent "ערכי והגישי מחדש" (edit and resubmit) call-to-action directly inside
the `CompilationFailed`/`AiFailed` result blocks, not just the generic header edit button.

## Stage 5: Delete (currently missing entirely)

**Doing**: N/A — there is no delete action anywhere in this feature today.
**Thinking**: "I submitted the wrong assignment by mistake — how do I remove this?"
**Feeling**: 😤 Blocked — no path forward in the UI at all, despite the backend supporting it.
**Pain points**: Complete feature gap.
**Opportunity**: Add delete to the list (and optionally the detail page), matching the
`ConfirmationService` + gender-neutral-copy pattern established for Lessons/Students/Assignments.

## Success Metric

Both personas can read every submission's status in plain Hebrew without guessing, know within a few
seconds of return-visit whether grading finished, and can delete/remove a mistaken submission without
contacting anyone.
