# Plan: Security and Operations Fixes

## TL;DR

Eight defects found by auditing the existing code. Each was verified against the source, not inferred. They are
unrelated to each other and to the account-management plans, so this file can ship at any time — but **fixes 1,
2 and 7 are worth doing first**, because all three are cases where the code's stated intent and its actual
behaviour have silently diverged.

Nothing here is a known exploit. The system is still in development with no real students.

**Related:** [plan-adminTeacherManagement](plan-adminTeacherManagement.prompt.md) ·
[plan-passwordRecovery](plan-passwordRecovery.prompt.md) · [plan-personalArea](plan-personalArea.prompt.md)

---

## ⚠️ MANDATORY — load the relevant skill before writing code

| Work | Skill |
|---|---|
| `LoginHandler` changes, the shared password policy | `backend-mediatr-query-handler-pattern` |
| `GetByClassIdsAsync` scoping in the export handler | `backend-repository-query-pattern` |
| Anything touching endpoint auth | `backend-controller-endpoint-pattern` |

Hebrew copy, feminine form. Follow [server/CLAUDE.md](../../server/CLAUDE.md).

---

## Fix 1 — 🔴 The `"ai"` rate-limit policy silently degrades to per-IP

`Program.cs:148-151` orders the pipeline `UseRateLimiter()` → `UseAuthentication()` → `UseAuthorization()`.
The `"ai"` policy partitions by the signed-in user:

```csharp
partitionKey: httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
              ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
```

At the point the limiter runs, authentication has not happened yet, so `httpContext.User` carries no claims and
the key **always** falls through to the IP. That is precisely the behaviour the comment at `Program.cs:88-91`
says the policy was written to avoid: every teacher in the building shares one 10-per-minute quota on paid AI
operations.

**Fix:** move `app.UseRateLimiter()` to after `app.UseAuthentication()` and `app.UseAuthorization()`.

**Verify:** sign in as teacher A and issue 11 AI-triggering calls inside a minute — the 11th returns 429, while
teacher B on the same network is unaffected. Before the fix, B is throttled by A's traffic.

## Fix 2 — 🔴 The admin has no way back into her own account

The seeding block at `Program.cs:114-131` creates the admin **only when the username does not already exist**:

```csharp
if (!await users.ExistsByUsernameAsync(adminUsername))
```

So changing `AdminUser:Password` in configuration after the first run does nothing. An admin who forgets her
password can only get back in by editing the database directly or by adding a second admin username. This is
also the one account with no teacher above it to reset it.

**Fix:** extend the seeding block to write `AdminUser:Email` into the admin `User` row's `Email` column, so she
recovers through the same flow as everyone else. Do **not** make the seeder overwrite the password when the
config differs — that would silently reset the admin's password on every restart to a value that lives in a
config file.

**Depends on** the `Email` column from
[plan-adminTeacherManagement](plan-adminTeacherManagement.prompt.md), and is only useful together with
[plan-passwordRecovery](plan-passwordRecovery.prompt.md). Until both land, document the limitation in the repo
rather than leaving it implicit.

**Verify:** with an existing admin row, change `AdminUser:Password` and restart — sign-in still requires the
original password (documents the real behaviour). After the recovery plan ships, the admin can reset by email.

## Fix 3 — 🟠 No per-account lockout, and per-IP limiting punishes the whole school

The `"auth"` policy allows 5 requests per minute **per IP** and nothing counts failures per account. Two
consequences:

- An attacker gets 5 guesses a minute against a chosen account indefinitely — roughly 7,200 a day. With
  8-character passwords and no special-character requirement, that is a real window over weeks.
- One teacher mistyping her password five times throttles **everyone else behind the same NAT** for a minute.
  The codebase already knows about the shared-NAT problem — it is documented on the `"ai"` policy — but
  `"auth"` still partitions by IP.

**Fix:** keep the IP limiter as coarse anti-flooding but raise `PermitLimit` (≈20/min), and add the precise
control on the account itself — `FailedLoginAttempts` and `LockoutEndsAt` columns on `User`, incremented in
`LoginHandler` on failure, reset on success, blocking sign-in while locked. One migration.

⚠️ The lockout message must stay generic. Telling the caller "this account is locked" confirms the account
exists, which is exactly what `LoginHandler`'s existing generic error avoids.

**Verify:** six wrong passwords for one account lock it for the configured window while a different account on
the same IP still signs in; a correct password before the threshold clears the counter.

## Fix 4 — 🟠 No CORS configuration at all

There is no `AddCors` or `UseCors` anywhere in the server. Development works only because of
`client/proxy.conf.json`. In production, if the client is served from a different origin than the API, every
browser request is blocked.

This fails closed, so it is not a vulnerability today — the risk is the fix applied under deployment pressure,
which is typically `AllowAnyOrigin`.

**Fix:** add an explicit allow-list bound to an `App:AllowedOrigins` configuration array, with the real values
in the gitignored `appsettings.Development.json` / production environment variables. The client authenticates
with a Bearer header rather than cookies, so do **not** enable `AllowCredentials`, and never pair it with
`AllowAnyOrigin`.

**Verify:** a browser request from a listed origin succeeds; from an unlisted one it is blocked.

## Fix 5 — 🟠 Every teacher exports every student in the school

`IStudentRepository.cs:13-16` carries an explicit warning that reports and exports must use
`GetByClassIdsAsync`, "otherwise every teacher exports a list containing every student in the school".
`ExportStudentsHandler.cs:21` calls `GetAllAsync(ct)` — the exact method the warning names.

`GetStudentsHandler.cs:28` has the same shape on the list screen.

**Decision (given): a teacher sees only the students in her own classes.** The warning comment is correct and
the code is wrong.

**Fix:** scope both `ExportStudentsHandler` and `GetStudentsHandler` through `GetByClassIdsAsync`, with the
class ids derived from the caller's own lessons. Admin stays unscoped through `OwnerScopeTeacherId` (null =
no filter), the same escape hatch every other handler uses.

⚠️ `Student` has no `TeacherId`, so "her classes" must be resolved indirectly — through the lessons she owns
and the `Lesson`↔`SchoolClass` many-to-many. Follow whatever the lessons list already does for class
filtering rather than inventing a second definition of ownership.

**Verify:** teacher A's list and export contain only students in classes attached to her lessons; a student in
another teacher's class appears for neither; Admin sees everyone.

## Fix 6 — 🟡 Excel import applies weaker password rules than everywhere else

`ImportStudentsHandler.cs:143-146` re-implements the password rules inline — length, uppercase, lowercase,
digit — and **omits the Hebrew-character check** that `PasswordRuleExtensions` enforces. A password containing
Hebrew letters is accepted through import and rejected on every other path.

**Fix:** extract the rule set into one shared policy (a plain static predicate returning the failure reason),
and have both `PasswordRuleExtensions` and `ImportStudentsHandler` call it. One source of truth, so the next
divergence cannot happen.

**Verify:** an import row with a Hebrew-letter password is rejected with a row-numbered Hebrew error, matching
what the student form already does.

## Fix 7 — 🟠 The final lesson score is whatever the client sends, with no record of where it came from

`CompleteLessonHandler` verifies lesson ownership and blocks finalizing while any assignment's latest
submission is still `PendingAi` / `ProcessingAi` / `CompilationFailed` / `JudgeUnavailable`. It does **not**
compute the score: `CompleteLessonRequestDto.FinalScore` arrives from the client and is written through
`result.CompleteWith(command.FinalScore, command.HasBonus)` unchanged. The client pre-fills the average of the
per-assignment scores, and the teacher may edit it before sending.

Per-assignment scores are decided by the system — `ScoreCalculator` is a pure function, documented as
*"מודל שפה לא נוגע בציון בשום שלב"*. The final lesson score is not: the server accepts any value in 0–100
(0–150 with bonus).

This is inconsistent with how the same concern is handled one level down: `Submission.OverrideScore` requires a
reason (*"A reason is required — it is the audit trail"*) and records `ScoreOverriddenByUserId`
(`Submission.cs:79-84`). Overriding a submission score is a deliberate, audited act; overriding a final lesson
score is neither, and afterwards there is no way to tell a computed score from a typed one.

**Fix — decide which of these the product means:**

- **The system decides:** compute the final score server-side from the student's graded submissions in the
  lesson and ignore any score in the request. The endpoint keeps only `StudentId` / `LessonId`.
- **The system proposes, the teacher may override:** compute it server-side as well, but accept an explicit
  override — and audit it the way submissions already are (`FinalScoreOverriddenByUserId` + reason on
  `LessonResult`, one migration), so the distinction survives.

Either way the server must compute the number rather than trust the client. Do not leave the current state,
where the only place the score is derived is the browser.

**Verify:** `POST /api/lesson-results/complete` with a `FinalScore` that contradicts the student's graded
submissions — it is recomputed or rejected, never stored blindly. The existing block on non-final submissions
still applies. Reopening and re-finalizing still works.

## Fix 8 — 🟡 Two stale copies of the coding rules

`.github/instructions/server.instructions.md` and `client.instructions.md` are Copilot-era duplicates of
`server/CLAUDE.md` and `client/CLAUDE.md`, and `.github/copilot-instructions.md` duplicates the root
`CLAUDE.md`. A line-by-line comparison confirms the `CLAUDE.md` copies are strict supersets — they also carry
the Excel Import/Export and Secrets sections, which the `.github` copies never received. The drift has already
happened.

**Fix:** the `CLAUDE.md` files are the single source of truth. Reduce each `.github` file to a short pointer at
its counterpart, keeping the Copilot frontmatter (`applyTo:`) so Copilot still resolves it. Do not maintain two
bodies of the same rules.

**Verify:** every rule that existed only in a `.github` file is present in the corresponding `CLAUDE.md` before
the body is replaced.

---

## Out of scope

- **JWT revocation.** Documented in [plan-passwordRecovery](plan-passwordRecovery.prompt.md); it needs a
  denylist or short-lived tokens with refresh.
- **Adding a real FK from `Lesson.TeacherId` / `Course.TeacherId` to `Users`.** Discussed in
  [plan-adminTeacherManagement](plan-adminTeacherManagement.prompt.md).
- **Requiring special characters in passwords.** A policy change affecting every existing account; decide
  separately.
