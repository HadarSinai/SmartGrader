# Business Rules

> SmartGrader · Version 1.1 · Last updated 2026-08-30 · Status: as-built

| Version | Date | Change |
|---|---|---|
| 1.0 | 2026-08-27 | First edition. `B-1 … B-52`. |
| 1.1 | 2026-08-30 | `B-50` closed: the seeder fills a missing admin email, and startup warns for every admin still without one. |

**What this document answers:** every non-grading rule the system enforces, with a stable id and a
place in the code to look.

**Why it exists.** These rules were real and enforced, but they lived only as C# comments — findable
by someone already reading the right file, and by nobody else. A rule nobody can cite is a rule that
gets re-argued.

Grading is [grading-rules.md](grading-rules.md) (`G-N`). The two registries never restate each other.

`BusinessRuleAnchorTests` asserts every id is unique and every cited path still exists. **The anchor
is where the rule lives, not proof that it works** — a moved file goes red here; a broken rule goes red
in its own test.

---

## Submissions and attempts

<!-- gen:rules B -->

| Id | Rule | Anchor |
|---|---|---|
| B-1 | There shall be exactly one submission row per student per assignment, enforced by a unique index. | `server/Infrastructure/Data/GradeSheetContext.cs` |
| B-2 | There shall be no cap on the number of attempts. | `server/Domain/Entities/Submission.cs` |
| B-3 | At least one minute shall pass between two attempts on the same assignment. | `server/Domain/Entities/Submission.cs` |
| B-4 | A submission shall be open to another attempt on any failure status, or when it is graded below the assignment's retry threshold. | `server/Domain/Entities/Submission.cs` |
| B-5 | When the sandbox was unavailable, the student shall not be offered a fix-and-resubmit action — the failure is not hers and there is nothing to correct. | `client/src/app/pages/my/my-feedback.component.ts` |
| B-6 | A teacher's extra-attempt grant shall override the retry threshold, shall require a written reason, and shall be consumed by the next submission. | `server/Domain/Entities/Submission.cs` |
| B-7 | A finalised lesson result or an archived class shall lock the affected submissions, overriding both the retry threshold and any teacher grant. | `server/Application/Common/Authorization/SubmissionLock.cs` |
| B-8 | ⚠️ A third lock condition — submissions predating the grading engine — was specified and **never implemented**; going live against a database with real history reopens every old submission below the threshold. | `server/Application/Common/Authorization/SubmissionLock.cs` |
| B-9 | Reopening a submission shall archive the current attempt and clear its score, breakdown, feedback, results and grading time together. | `server/Domain/Entities/Submission.cs` |
| B-10 | Only the ten most recent attempts shall retain their full content; older ones shall keep score, status and timestamps only. | `server/Domain/Entities/SubmissionAttempt.cs` |
| B-11 | Login shall return one identical message for every failure — unknown username, wrong password and locked account alike. | `server/Application/UseCases/Auth/Login/LoginHandler.cs` |
| B-12 | Five consecutive failed logins shall lock an account for fifteen minutes. | `server/Domain/Entities/User.cs` |
| B-13 | A failed login shall not be recorded while the account is already locked, so continued guessing cannot extend the lock indefinitely. | `server/Domain/Entities/User.cs` |
| B-14 | An expired lockout shall reset the failure counter, so ordinary typos spread over weeks do not accumulate into a lock. | `server/Domain/Entities/User.cs` |
| B-15 | The per-IP throttle shall stay deliberately generous, because a whole school sits behind one address; the per-account lockout is the precise control. | `server/Api/Program.cs` |
| B-16 | `forgot-password` shall return an identical empty 200 on every path — registered, unknown, student account and mail failure alike. | `server/Application/UseCases/Auth/ForgotPassword/ForgotPasswordHandler.cs` |
| B-17 | `reset-password` shall return one generic message for a missing, expired, superseded or already-used token. | `server/Application/UseCases/Auth/ResetPassword/ResetPasswordHandler.cs` |
| B-18 | A newly issued reset link shall supersede every outstanding link for that user. | `server/Domain/Entities/PasswordResetToken.cs` |
| B-19 | Only a hash of a reset token shall be stored; the token itself shall exist only in the emailed link. | `server/Domain/Entities/PasswordResetToken.cs` |
| B-20 | ⚠️ A password reset shall not revoke active sessions — a session signed in with the old password stays valid until its token expires. | `server/Api/Program.cs` |
| B-21 | Students shall be excluded from email recovery; a student recovers through her teacher, as a teacher recovers through the admin. | `server/Application/UseCases/Auth/ForgotPassword/ForgotPasswordHandler.cs` |
| B-22 | There shall be no self-registration; every account shall be created by someone above it — admin → teachers → students. | `server/Api/Controllers/TeachersController.cs` |
| B-23 | A username shall be immutable; an email shall be normalised to lowercase and unique. | `server/Domain/Entities/User.cs` |
| B-24 | A teacher's email shall be required by the validators even though the column is nullable, because a teacher without one can never recover her password. | `server/Domain/Entities/User.cs` |
| B-25 | There shall be no notification entity and no notifications table; every signal shall be computed on demand from submission rows in a date window. | `server/Application/Services/Notifications/ClassSignalDetector.cs` |
| B-26 | A class signal shall fire only at three or more affected students **and** at least half of those who submitted. | `server/Application/Services/Notifications/ClassSignalThresholds.cs` |
| B-27 | The "nobody passed" signal shall additionally require three submissions. | `server/Application/Services/Notifications/ClassSignalThresholds.cs` |
| B-28 | The "nobody passed" signal shall be suppressed when the compilation signal already fired on the same assignment. | `server/Application/Services/Notifications/ClassSignalDetector.cs` |
| B-29 | The signal window shall be cut on last submission time, not grading time, so submissions that never reached a grade are not silently dropped. | `server/Application/Services/Notifications/ClassSignalDetector.cs` |
| B-30 | "Passed" in a signal shall mean that all core test cases passed, not that a score threshold was reached. | `server/Application/Services/Notifications/ClassSignalDetector.cs` |
| B-31 | A hidden test case's input shall never appear in a signal's text — only its position — because the same sentence is emailed and email is forwarded. | `server/Application/Services/Notifications/ClassSignalDetector.cs` |
| B-32 | An advisory requirement shall never raise a signal. | `server/Application/Services/Notifications/ClassSignalDetector.cs` |
| B-33 | The reporting day shall be bounded by Israel local time, not UTC, so an evening lesson is not split across two digests. | `server/Application/Services/Notifications/ClassSignalPeriod.cs` |
| B-34 | A day with nothing to report shall send no email at all — never a "no news today". | `server/Api/BackgroundServices/TeacherDigestJob.cs` |
| B-35 | Each teacher's digest send shall be isolated, so one bad address cannot abort the run; a failed send shall be logged, and a teacher with no address shall be skipped silently. | `server/Api/BackgroundServices/TeacherDigestJob.cs` |
| B-36 | Class signals shall be visible to teachers and admins only. | `server/Api/Controllers/NotificationsController.cs` |
| B-37 | Code analysis shall be syntactic only — there is no semantic model and no type resolution. | `server/Infrastructure/Services/CodeAnalysis/RoslynCodeAnalysisService.cs` |
| B-38 | Recursion detection shall compare whole identifiers, never substrings. | `server/Infrastructure/Services/CodeAnalysis/RoslynCodeAnalysisService.cs` |
| B-39 | A matrix shall be a distinct construct from an array; a declaration of a one-dimensional array shall not satisfy a matrix requirement. | `server/Infrastructure/Services/CodeAnalysis/RoslynCodeAnalysisService.cs` |
| B-40 | ⚠️ Inheritance shall be reported as syntactically indistinguishable from interface implementation, because both are a base list and there is no semantic model to separate them. | `server/Infrastructure/Services/CodeAnalysis/RoslynCodeAnalysisService.cs` |
| B-41 | ⚠️ LINQ detection shall cover query syntax and the using directive only; **method chaining is not detected**. | `server/Infrastructure/Services/CodeAnalysis/RoslynCodeAnalysisService.cs` |
| B-42 | An `else if` shall count as two conditionals, because it is a nested `if` in the syntax tree. | `server/Infrastructure/Services/CodeAnalysis/RoslynCodeAnalysisService.cs` |
| B-43 | An implicitly typed variable shall not be counted toward a typed-variable requirement, because its type is unknown without a semantic model. | `server/Infrastructure/Services/CodeAnalysis/RoslynCodeAnalysisService.cs` |
| B-44 | Nesting depth shall be reported as a depth, not as a count of loops. | `server/Infrastructure/Services/CodeAnalysis/RoslynCodeAnalysisService.cs` |
| B-45 | Adding a construct to the catalog shall require one enum value and one analyzer case, and nothing else. | `server/Domain/Entities/CodeConstruct.cs` |
| B-46 | A test case shall default to core and to hidden — scored as central, invisible to the student — so data loaded without those flags fails closed. | `server/Domain/Entities/TestCase.cs` |
| B-47 | A teacher who owns lessons or courses shall not be deleted, and the refusal shall count them. | `server/Application/UseCases/Teachers/DeleteTeacher/DeleteTeacherHandler.cs` |
| B-48 | ⚠️ Deleting a teacher shall orphan four audit columns that carry no foreign key, degrading the audit trail rather than failing; there is no way to transfer lessons or courses to another teacher. | `server/Infrastructure/Data/GradeSheetContext.cs` |
| B-49 | An Excel import shall succeed partially: errors shall be collected per row with its number, and one bad row shall never roll back the others. | `server/Application/UseCases/Student/ImportStudents/ImportStudentsHandler.cs` |
| B-50 | The seeder shall never overwrite an existing admin's password or email, shall fill an address in only where none is held at all, and startup shall warn for every admin account still left without one. | `server/Api/Program.cs` |
| B-51 | Student-submitted source code shall be rendered as escaped text and never as HTML, because the session token lives in browser storage and any injection reads it. | `client/src/app/components/submitted-code/submitted-code.component.ts` |
| B-52 | Swagger and the Hangfire dashboard shall be served in development only. | `server/Api/Program.cs` |

<!-- /gen -->

---

## The three rules to read first

Out of forty-nine, these are the ones that decide arguments.

### `B-7` and `B-8` — the lock, and the condition that is missing

A finalised lesson or an archived class locks the affected submissions. The lock beats the retry
threshold **and** a teacher's explicit grant, because a final grade that has already been given to a
student must not move underneath her. Reopening the lesson result is the way back — deliberately a
different, more visible action than granting an attempt.

**`B-8` is a go-live risk, and it is written here rather than in a comment for that reason.** A third
condition — "submitted before the grading engine shipped" — was specified and never built, on the
decision that the database held development data that would be wiped. If the system goes live against
a database with real submission history, **every old submission below the retry threshold reopens on
the day of deployment.** The decision to accept or close that belongs to a person, before deployment,
not to whoever next reads `SubmissionLock.cs`.

### `B-11`, `B-16`, `B-17` — one message, on purpose

Three separate endpoints return deliberately uninformative responses, and all three will look like
bugs to someone trying to improve the error messages:

- login returns the same text for an unknown username, a wrong password, **and a locked account**
- `forgot-password` returns an empty 200 for a registered address, an unknown address, a student
  account, **and a mail server failure**
- `reset-password` returns one message for missing, expired, superseded and used tokens

Any difference — including an unhandled 500 — turns the endpoint into an oracle that confirms which
accounts exist. `ForgotPasswordHandler` catches its own send failures for exactly this reason, and
`B-35`'s log row is then the only trace that anything went wrong.

### `B-37` … `B-44` — what the analyzer cannot see

Syntax only. No compilation, no type resolution. A teacher writing a requirement is choosing from a
catalog whose limits are real:

- "must use inheritance" also passes on `class A : IB` (`B-40`)
- "must use LINQ" fails on `list.Where(...).Select(...)` (`B-41`)
- "at most 2 `if`" counts `if / else if / else` as **two** (`B-42`)
- "must declare a `bool`" does not see `var isSorted = true;` (`B-43`)

These are not defects to file — they are the price of an analyzer that runs in milliseconds, locally,
for free, on code that may not even compile. They belong in front of the teacher when she writes the
requirement, which is why they have ids.

---

### `B-48` and `B-50` — the two data-integrity holes

**`B-48`.** Only `Student.UserId` (set to null on delete) and `Lesson.TeacherId` / `Course.TeacherId`
(both restricted) are real foreign keys to `Users`. Four columns are plain nullable integers with no
key at all, so nothing stops a delete and nothing clears them:

| Column | Written by |
|---|---|
| `Log.UserId` | every logged action |
| `LessonResult.FinalScoreOverriddenByUserId` | a final-score override |
| `Submission.ScoreOverriddenByUserId` | a submission-score override |
| `Submission.ExtraAttemptGrantedByUserId` | an extra-attempt grant |

After a teacher is deleted these read "user 7 did X" where user 7 no longer exists — a degraded audit
trail, not a crash. **In practice the exposure is small but not zero:** a teacher is only deletable
with zero lessons and zero courses, and overrides are reachable only through a lesson she owns, so the
three grading columns are almost always empty for a deletable teacher. `Log.UserId` is the realistic
one — any teacher who ever signed in left rows behind.

Widening the delete guard, or nulling these on delete, was left open rather than decided quietly.
Related and also open: there is no way to transfer lessons or courses to another teacher, which is the
intended route past the guard.

**`B-50`.** The seeder never overwrites the admin's email — correctly, because overwriting would
silently revert a value the admin has since changed, on every restart. The consequence used to be that
an admin row created before the email column existed held none, could not be matched by recovery, and
**was the one account with nobody above it to reset the password.**

**Filling is not overwriting, and that is the whole fix.** Where the row holds no address at all,
startup writes `AdminUser:Email` into it; where it holds one, that value stands even if the
configuration disagrees. An empty address is not a choice the admin made — it is an account with no way
back.

Filling only reaches the username in `AdminUser:Username`, and only when `AdminUser:Email` is
configured. So startup also **counts every `Admin` row still holding no address and logs a warning
naming them.** A fresh environment, a restore from an older backup, or a second admin created some
other way each reproduce the hole silently, and it otherwise surfaces on the day she forgets her
password — the worst possible moment to discover it.

The warning names the accounts so the one-off repair can be aimed. Where configuration cannot be used:

```sql
UPDATE Users SET Email = 'her.address@example.com' WHERE Username = 'admin' AND Email IS NULL;
```

The `Email IS NULL` clause is not decoration — without it the statement overwrites a working address,
which is the behaviour this rule exists to prevent.

## Where these rules used to live

`server/CLAUDE.md` carried the prose for most of the identity, notification and deletion rules. That
prose is now replaced by links into this registry — one source of truth, cited by id. The reasoning
that was worth keeping came here with it.
