# Plan: Submission → Compilation → AI Feedback Pipeline Fixes

## TL;DR

Investigated the full submission grading pipeline (student submits code → `CreateSubmissionHandler` saves
`Submission` with status `PendingAi` and enqueues a Hangfire job → `AiWorker` picks it up, flips to
`ProcessingAi`, calls `Judge0CodeRunner` to compile/run the code against test cases → on compile failure,
`MarkCompilationFailed` and stop; otherwise call OpenAI for feedback and `MarkDone`).

Found several compounding problems that make this stage fragile and hard to diagnose:

1. **Judge0 infrastructure failures (service down / timeout / network error) are indistinguishable from
   genuine AI-feedback failures.** Both land in the same generic `catch` block in `AiWorker` and get marked
   `AiFailed`, so logs can't tell "Judge0 was down" apart from "the AI failed."
2. **A submission that fails compilation is a dead end.** There is no status transition back out of
   `CompilationFailed`. Worse, the UI already ships an "edit and resubmit" button for this case, but it's
   **broken for students today**: it calls `PUT /api/students/{studentId}/submissions/{id}`, which is
   restricted to `Roles = "Teacher,Admin"` only — a student who clicks it gets a 403. Even if a teacher calls
   it successfully, the handler maps the new source code onto the entity but never resets the status and
   never re-enqueues a Hangfire job, so grading never actually re-runs.
3. **No timeout/retry around Judge0 HTTP calls**, while the OpenAI client already has an explicit timeout —
   an inconsistency in the codebase.
4. **Judge0 `BaseUrl` configuration conflicts** across `appsettings.json` / `Judge0Options` defaults /
   `docker-compose.yml`. The user is handling the move to Docker on their own server themselves — **out of
   scope for this plan.**
5. **Hangfire uses in-memory storage only** — queued jobs are lost on app restart. Flagged but not fixed here
   (would require a new NuGet package + persistent connection string — a separate, larger change).
6. Secondary issues: no submission size limit, no linked audit trail across resubmissions, no dedicated
   `LogActionTypes` entry for infrastructure failures, Hangfire Dashboard is dev-only.

**Decisions already confirmed with the user:**
- A genuine compilation failure (real syntax error in the student's code) must **not** proceed to AI
  feedback. It stays `CompilationFailed`, but the student must get a real, working resubmission path.
- An infrastructure failure (Judge0 down / network / timeout) must **not** auto-retry. Just mark it clearly
  and distinctly, so it's obvious this is a Judge0 problem, not the student's code or the AI.
- Docker/BaseUrl production config is being handled separately by the user — not touched here.

## Steps

### Phase 1 — Domain: new `JudgeUnavailable` status (infrastructure failure, distinct from `CompilationFailed`/`AiFailed`)

1. In `server/Domain/Entities/Submission.cs`, add to the `SubmissionStatus` enum (after `CompilationFailed`):
   `JudgeUnavailable = 5`.
2. Add a new transition method `MarkJudgeUnavailable(string error)`, allowed only from `PendingAi` or
   `ProcessingAi` (same guard shape as the existing `MarkCompilationFailed`). Store the error message on the
   existing `AiError` field unless a dedicated `CompileError`-style field already exists — check and reuse
   before adding a new column.
3. Extend `MarkPendingAi()` so it also accepts `CompilationFailed` and `JudgeUnavailable` as valid source
   states (currently it only accepts `AiFailed`) — this is what makes resubmission possible (Phase 4).
4. Add a `JudgeUnavailable` entry to `LogActionTypes` (find the exact file — likely under
   `server/Domain/Constants/` or similar) alongside the existing `AiGradingStarted` / `AiGradingCompleted` /
   `CompilationFailed` / `AiFailed` / `UnhandledError` entries.

### Phase 2 — `AiWorker`: split infrastructure failures from AI failures (depends on Phase 1)

5. In `server/Api/BackgroundServices/AiWorker.cs`, wrap the call to `_codeRunner.RunAsync(...)` in its own
   `try/catch` that specifically catches `HttpRequestException`, `JsonException`, and
   `TaskCanceledException` (timeout) — placed before the existing generic `catch (Exception ex)`.
6. In that catch block: call `submission.MarkJudgeUnavailable(ex.Message)`, save, write a log entry with
   `LogActionTypes.JudgeUnavailable`, and `return` — no automatic retry, no fallthrough to AI feedback (per
   the confirmed decision).
7. Fix the re-entrancy guard (currently `if (submission.Status is SubmissionStatus.Done or
   SubmissionStatus.AiFailed) return;`) to also short-circuit on `CompilationFailed` and
   `JudgeUnavailable` — today a Hangfire retry against an already-`CompilationFailed` submission falls
   through and throws a swallowed `InvalidOperationException` inside `MarkProcessingAi()`.
8. (Recommended, optional) In `Judge0CodeRunner`, add explicit handling for Judge0 status id **13 (Internal
   Error)** — today it silently falls into the generic "failed test case" bucket even though it's actually a
   Judge0-side infrastructure problem, not a student code problem. Throw a dedicated exception type so it
   also routes to `MarkJudgeUnavailable` via the new catch block.

### Phase 3 — HTTP timeout for the Judge0 client (no retry, no new dependency)

9. In `server/Infrastructure/DependencyInjection.cs`, add a timeout to
   `services.AddHttpClient<ICodeRunnerService, Judge0CodeRunner>()`, mirroring the existing pattern used for
   the OpenAI client (`c.Timeout = TimeSpan.FromSeconds(...)`).
10. Add a new config field distinct from the existing `Judge0Options.TimeoutSeconds` (which today only
    controls Judge0's own `cpu_time_limit` request parameter, not the HTTP client) — e.g.
    `Judge0Options.HttpTimeoutSeconds`, defaulting to something in the 30–40s range (enough headroom over
    Judge0's own execution time limit, but well under the current 100s `HttpClient` default).
11. Do **not** add Polly or `Microsoft.Extensions.Http.Resilience` — neither is referenced in the project
    today, and the confirmed decision is no automatic retry, just a fast, clear failure.

### Phase 4 — Fix the resubmission flow (highest-priority fix — currently broken in production)

12. In `server/Api/Controllers/StudentsController.cs`, the existing `[HttpPut]` submission-update action
    (currently `Roles = "Teacher,Admin"` only) needs a student-ownership check equivalent to the
    `IsAllowedForStudent` check already used in `CreateSubmission`, so a student can edit/resubmit **only
    their own** submission, and only when its status allows it.
13. In the update handler (`UpdateSubmissionHandler.cs` or wherever it lives), currently it does
    `_mapper.Map(request.Dto, submission)` directly and never touches status or Hangfire. Fix it to:
    - Reject the update unless current status is `CompilationFailed`, `JudgeUnavailable`, or `AiFailed`
      (block edits to `Done` / `ProcessingAi` / `PendingAi` submissions).
    - Update the source code/files, call `submission.MarkPendingAi()` (now valid per Phase 1 step 3), save.
    - Inject `IBackgroundJobClient` and enqueue a new job:
      `_jobClient.Enqueue<IGradeSubmissionJob>(job => job.ExecuteAsync(submission.Id));` — same call as
      `CreateSubmissionHandler` uses.
14. In `client/src/app/pages/submissions/submission-detail.component.ts`, make sure the "edit and resubmit"
    button is shown for `JudgeUnavailable` too, not just `CompilationFailed`/`AiFailed`, and confirm
    `navigateToEdit()` hits the now-fixed endpoint correctly.

### Phase 5 — Frontend: surface the new status (depends on Phase 1)

15. In `client/src/app/models/submission.model.ts`, add `"JudgeUnavailable"` to the `SubmissionStatus` union
    type and add a label entry to `STATUS_LABELS_HE` (final wording to confirm, something like a distinct
    "system/judge error" label — should read clearly as *not* the student's fault).
16. Update every place that switches on submission status to add a branch for `JudgeUnavailable`:
    - `client/src/app/pages/submissions/submission-detail.component.ts` — the `ngSwitch` block (tag + icon +
      severity), the error-box display condition, and the resubmit-button visibility condition.
    - `client/src/app/pages/my/my-assignments-list.component.ts` — the `switch (status)` mapping to
      `statusSeverity`/`statusIcon`.
    - `client/src/app/pages/my/my-feedback.component.ts`, `client/src/app/pages/my/my-grades.component.ts`,
      `client/src/app/pages/logs/logs-list.component.ts` — check each for the same status-branching pattern
      and update if present.
    - Recommendation: give `JudgeUnavailable` a visually distinct severity/color from `CompilationFailed`
      (e.g. neutral/warning instead of error-red) to signal it isn't the student's fault.

### Phase 6 — Guard `CompleteLesson` against non-`Done` submissions

17. In `server/Application/UseCases/LessonResults/CompleteLesson/CompleteLessonCommandValidator.cs` and/or
    `CompleteLessonHandler.cs`, add a check that a lesson cannot be marked complete with a final score while
    the underlying submission is `PendingAi`, `ProcessingAi`, `CompilationFailed`, or `JudgeUnavailable`.
    Only allow completion from `Done` (consider whether `AiFailed` should be an explicit teacher-override
    case rather than fully blocked — don't hard-block a teacher who wants to grade manually).

### Phase 7 — Verification

18. Run `dotnet build` on `server/SmartGrader.sln` — confirm a clean build.
19. Run `ng build` in `client/` — confirm a clean build.
20. Manual/integration check: submit code with a real syntax error → confirm status lands on
    `CompilationFailed`, and that the resubmit button actually works end-to-end (edits code, re-enqueues,
    re-runs).
21. Simulate a Judge0 outage (stop the local service, or point `BaseUrl` at an unreachable address
    temporarily) → confirm the submission lands on `JudgeUnavailable` (not `AiFailed`), and that the log
    entry uses the new `LogActionTypes.JudgeUnavailable` action type.
22. Confirm `CompleteLesson` is rejected when the submission status doesn't allow it.

## Out of scope (handled separately by the user)

- Changing `Judge0.BaseUrl` / appsettings for production against the new Docker Compose setup on their server.
- Moving Hangfire to persistent storage (SQL/Postgres) — requires a new NuGet package, not just a config
  change. Worth a separate plan if wanted later.
- Optimistic concurrency (`RowVersion`) on `Submission` to guard against two Hangfire workers racing on the
  same submission — a narrow edge case, not critical for this plan.
- Submission size/content limits.
