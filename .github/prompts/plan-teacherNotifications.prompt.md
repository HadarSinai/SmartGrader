# Plan: Teacher Notifications — Class Signals Instead of Individual Submissions

## TL;DR

The bell today lists **individual graded submissions** (`GetRecentGradedSubmissionsHandler`). A teacher does not
need to be told that each of thirty students had her code checked — that is noise, and it is why the bell goes
unread.

Replace what counts as a notification. All four kept signals are **aggregates over one assignment or lesson**,
and each answers a question the teacher would otherwise have to go looking for:

| # | Signal | Why it earns a place |
|---|---|---|
| 1 | A structural requirement failed for many students in the class | Tells her what to reteach tomorrow |
| 2 | One test case failed for many students | Usually means the wording of the exercise is unclear |
| 3 | **No** student passed a given assignment | The exercise itself is broken — wrong expected output or method signature |
| 4 | Most students could not even compile a given assignment | The instructions or the method signature are wrong, not the class |

Signals 1–2 are about the class. Signals 3–4 are about the exercise. Keeping both matters: without 3–4 a
teacher concludes her students failed, when in fact she wrote the exercise wrong.

Delivery: **the bell in real time, plus one daily digest email.** A day with nothing to report sends **no email
at all** — that rule is what separates a digest people read from one they filter away.

**Requires first:** [plan-adminTeacherManagement](plan-adminTeacherManagement.prompt.md) for `User.Email`.

---

## ⚠️ MANDATORY — load the relevant skill before writing code

| Work | Skill |
|---|---|
| The aggregation queries and their handlers | `backend-mediatr-query-handler-pattern` |
| Any new repository reads | `backend-repository-query-pattern` |
| The notifications endpoint | `backend-controller-endpoint-pattern` |
| Never leaking one teacher's classes to another | `backend-role-based-field-redaction` |
| The bell's rendering and Hebrew copy | `client-flow-fix-implementation-pattern` |

Hebrew copy, feminine form. Follow [server/CLAUDE.md](../../server/CLAUDE.md) and
[client/CLAUDE.md](../../client/CLAUDE.md).

---

## What exists today — read this before designing

- **There is no `Notification` entity and no notifications table.** `GetRecentGradedSubmissionsHandler` is a
  *derived view*: it queries recently graded submissions and maps them. Nothing is stored, sent, or marked read.
- **Keep it that way.** All four signals are aggregates computable from submissions on demand, and the digest
  covers a fixed daily window, so it is naturally idempotent by date. **No new table, no "already sent" state,
  no migration.** Resist adding one — it would exist only to record what a date range already determines.
- **`IEmailSender` can only send to the admin** (`SendToAdminAsync`). Add
  `SendAsync(string to, string subject, string body, CancellationToken ct = default)` — the same addition
  [plan-passwordRecovery](plan-passwordRecovery.prompt.md) needs. Whichever plan lands first adds it.
- `RecurringJob.AddOrUpdate` is already used for the daily log cleanup (`Program.cs:160-163`) — the digest job
  follows that pattern.
- Structural results are stored as JSON on the submission (`Submission.StructuralResultsJson`), not in a table,
  so signal 1 aggregates in memory rather than by SQL `GROUP BY`. Fine at class scale; do not build an index
  for it.

---

## Steps

### 1. Define the thresholds in one place

"Many" must be a single named rule, not a number repeated across four queries. Proposal, to live in
configuration:

- Signals 1, 2, 4: at least **3** affected students **and** at least **50%** of those who submitted.
- Signal 3: **zero** students passed, with at least **3** submissions — otherwise a single early submission
  raises a false alarm.

The minimum count is what stops a class of four from triggering a notification on every hiccup.

### 2. Aggregation queries

One handler per signal under `server/Application/UseCases/Notifications/`, each scoped to the caller's own
lessons. ⚠️ Scope through lesson ownership exactly as the existing handlers do — a teacher must never see a
signal about another teacher's class. Admin (`OwnerScopeTeacherId == null`) sees all.

Each returns a small record: the lesson, the assignment, the signal type, the affected count, the total, and
enough identity to link into the relevant screen.

### 3. Bell endpoint

Replace what `GET /api/notifications/graded-submissions` returns, or add a sibling and retire the old route
once the client no longer calls it. The student side of the bell is out of scope here — see Out of scope.

### 4. Daily digest job

A Hangfire recurring job that, per teacher with an email:

1. Runs the four aggregations over the previous day's activity.
2. **If there is nothing to report, sends nothing.** No "no news today" email, ever.
3. Otherwise sends one Hebrew email, feminine form, grouped by lesson, each line linking into the app.

⚠️ Wrap each teacher's send so one failure does not abort the rest of the run, and log failures where the admin
will see them — `SmtpEmailSender` no-ops silently when SMTP is unconfigured, which would otherwise make a
broken digest indistinguishable from a quiet day.

### 5. Client

Rewrite `notifications-bell.component.ts` to render the new signal types — each row a sentence plus a link,
not a submission row. Keep the existing polling.

---

## Verification

`dotnet build server/SmartGrader.sln`, `cd client && npm run build`, then seed a lesson and submit as several
students:

| # | Scenario | Expected |
|---|---|---|
| 1 | 8 of 12 students fail the same structural requirement | One signal naming the requirement and the count |
| 2 | 2 of 12 fail it | **No** signal — below the threshold |
| 3 | 9 of 12 fail the same test case | One signal naming the test case |
| 4 | No student passes an assignment, on 3+ submissions | The "exercise may be broken" signal |
| 5 | Same, on only 1 submission | No signal |
| 6 | 10 of 12 fail to compile | The compilation signal, distinct from #4 |
| 7 | Teacher B checks her bell | Sees nothing about teacher A's class |
| 8 | Admin checks the bell | Sees signals across all teachers |
| 9 | Run the digest job for a day with activity | One email per teacher with an address, grouped by lesson |
| 10 | Run it for a day with **no** activity | **No email is sent at all** |
| 11 | Run it with a teacher whose email is `NULL` | Skipped, no crash, rest of the run continues |
| 12 | Run it with SMTP unconfigured | Logged where the admin can see it, not silently swallowed |

---

## Out of scope

- **Ideas considered and not taken now:** students who exhausted their attempts; a student silent for two weeks;
  lessons with final scores still pending; notable score improvements. Each is an independent aggregation that
  hangs off the same mechanism, so any of them can be added later without rework.
- **Plagiarism detection** (near-identical code between two students). Genuinely wanted, but there is no
  similarity engine in the system and it would need one from scratch. A feature of its own.
- **A submission deadline.** `Lesson` has only `LessonDate` — there is no "due by" field, so no signal here can
  be lateness-based. Adding one is small but is a schema change.
- **The student's bell.** Listed in `.github/תיקונים.md` under work that did not turn out well; decide its fate
  separately rather than inside a teacher-facing change.
