# SmartGrader – Backend Rules (server/)

## Architecture

- **Clean Architecture**: dependency flow is always `Api → Application → Domain`, `Infrastructure → Domain`. Never reference `Infrastructure` from `Application` or `Domain`.
- **CQRS via MediatR**: all business logic lives in handlers. Controllers only call `_mediator.Send(...)`.
- **No business logic in controllers or repositories**.

---

## Naming Conventions

| Type                      | Pattern                                     | Example                        |
| ------------------------- | -------------------------------------------- | ------------------------------- |
| Command                   | `{Verb}{Entity}Command`                     | `CreateLessonCommand`          |
| Query                     | `Get{Entity}Query` / `Get{Entity}ByIdQuery` | `GetLessonsQuery`              |
| Handler                   | `{Command/Query}Handler`                    | `CreateLessonHandler`          |
| Validator                 | `{Command}Validator`                        | `CreateLessonCommandValidator` |
| Request DTO               | `{Verb}{Entity}RequestDto`                  | `CreateLessonRequestDto`       |
| Response DTO              | `{Entity}ResponseDto`                       | `LessonResponseDto`            |
| Repository interface      | `I{Entity}Repository`                       | `ILessonRepository`            |
| Repository implementation | `{Entity}Repository`                        | `LessonRepository`             |
| AutoMapper profile        | `{Entity}Profile`                           | `LessonProfile`                |

---

## Commands & Queries

- Commands and queries are `record` types.
- Each command/query has a corresponding handler class implementing `IRequestHandler<TRequest, TResponse>`.
- Each command has a paired `AbstractValidator<TCommand>` with FluentValidation rules.
- Validation runs automatically via `ValidationBehavior<TRequest, TResponse>` pipeline behavior.

```csharp
// Correct pattern
public record CreateLessonCommand(CreateLessonRequestDto Dto) : IRequest<LessonResponseDto>;

public class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
{
    public CreateLessonCommandValidator()
    {
        RuleFor(x => x.Dto.Name).NotEmpty();
    }
}

public class CreateLessonHandler : IRequestHandler<CreateLessonCommand, LessonResponseDto>
{
    public async Task<LessonResponseDto> Handle(CreateLessonCommand request, CancellationToken ct)
    {
        var entity = _mapper.Map<Lesson>(request.Dto);
        await _repository.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return _mapper.Map<LessonResponseDto>(entity);
    }
}
```

---

## Domain Entities

- Protected constructors — never instantiate entities with `new` from outside the domain.
- Use static factory methods: `Entity.Create(...)`.
- All entities have a `CreatedAt` property (UTC).
- Use `TestCase` list stored as JSON (`TestsJson` column) on `Assignment`.

---

## Repository Pattern

- Repositories return `IReadOnlyList<T>` for collections.
- Use `AsNoTracking()` for all read queries.
- Most repositories have no `Update` method — use EF change tracking. `StudentRepository` and `UserRepository` do have one, because their read methods return detached entities (`AsNoTracking`) and a handler that mutates one otherwise saves nothing at all.
- Always pass `CancellationToken` to every async method.

```csharp
// Correct repository method signature
Task<IReadOnlyList<Lesson>> GetAllAsync(CancellationToken ct = default);
```

---

## Unit of Work

- Never call `SaveChangesAsync` on the DbContext directly outside of `UnitOfWork`.
- Always call `await _unitOfWork.SaveChangesAsync(ct)` after mutations.

---

## Custom Exceptions → HTTP Mapping

| Exception                   | HTTP Status                  |
| ---------------------------- | ----------------------------- |
| `AppValidationException`    | 400 ValidationProblemDetails |
| `NotFoundException`         | 404 ProblemDetails           |
| `UniqueConstraintException` | 409 ProblemDetails           |
| `BusinessRuleException`     | 400 custom JSON              |

- Throw exceptions from handlers/services, never from controllers.
- `GlobalExceptionMiddleware` handles all mapping (and emails the admin on unhandled errors via `IEmailSender`).

---

## AutoMapper

- One profile class per entity in `Application/Common/Mapping/`.
- Profiles map: `Entity → ResponseDto` and `RequestDto → Entity`.

---

## Dependency Injection

- Application services registered in `Application/DependencyInjection.cs`.
- Infrastructure services registered in `Infrastructure/DependencyInjection.cs`.
- Both called from `Program.cs`: `builder.Services.AddApplication()` and `builder.Services.AddInfrastructure(config)`.
- Repositories are `Scoped`. Background services are `Singleton` (queue) + `HostedService`.

---

## Background AI Processing

- Use `IAiJobQueue` to enqueue submissions for AI processing.
- `AiWorker` (BackgroundService) dequeues and calls `IFeedbackService`.
- `Submission.Status` enum: `PendingAi` → `ProcessingAi` → `Done` / `AiFailed`.
- Never call OpenAI directly from a controller or handler — always enqueue.

---

## Excel Import/Export

- Export use cases return `byte[]` through MediatR; controllers wrap them in `File(...)`.
- Import receives a `Stream`, never `IFormFile` — the Application layer must not reference AspNetCore; the controller converts at the boundary.
- Import is partial-success: collect `{ RowNumber, Message }` errors per row and keep going, never roll back all rows for one bad row.
- See [.claude/skills/backend-excel-closedxml-pattern/SKILL.md](../.claude/skills/backend-excel-closedxml-pattern/SKILL.md) for the full pattern.

---

## Database

- SQLite via EF Core.
- Connection string key: `"Default"` in `appsettings.json`.
- Use `dotnet ef migrations add <Name>` to add migrations. Never edit migration files manually.

---

## Secrets

- Never put real credentials (admin password, SMTP credentials, API keys) in `appsettings.json` — it's committed to git. Use `appsettings.Development.json` (gitignored) or user-secrets for real local values; keep `appsettings.json` placeholders empty.

---

## CORS

- Allowed origins come from the `App:AllowedOrigins` configuration array. `appsettings.json` ships it **empty**, which emits no CORS headers at all — the same behaviour as before the policy existed, because in development `client/proxy.conf.json` serves client and API from one origin.
- Real values belong in `appsettings.Development.json` (gitignored) or production environment variables.
- The client authenticates with a `Bearer` header, not cookies, so **do not** enable `AllowCredentials` — and never pair it with `AllowAnyOrigin`.

---

## Auth: two independent throttles

- **Per-IP rate limit** (`"auth"` policy in `Program.cs`) — coarse anti-flooding only, deliberately generous (20/min). Every teacher in a school sits behind one NAT, so a tight per-IP limit punishes the whole building for one person's typo.
- **Per-account lockout** (`User.RegisterFailedLogin`) — the precise control: `User.MaxFailedLoginAttempts` consecutive failures lock the account for `User.LockoutDuration`. It cannot affect anyone else's account.
- ⚠️ `LoginHandler` returns **one message for every failure path** — unknown username, wrong password, locked account. Splitting it would confirm that an account exists. The lockout is explained by constant text that appears on every failure, so it leaks nothing.
- `app.UseRateLimiter()` must stay **after** `app.UseAuthentication()`. Before it, `httpContext.User` has no claims and the `"ai"` policy — which partitions per user — silently degrades to per-IP.

---

## Password recovery by email

Teachers and admins recover their own accounts: `POST /api/auth/forgot-password` emails a one-time link, `POST /api/auth/reset-password` consumes it. Both are `[AllowAnonymous]` **and** `[EnableRateLimiting("auth")]` — without the limiter, `forgot-password` is an open email-sending relay pointed at any address a caller names.

**Students are deliberately excluded.** A student has no email; she recovers through her teacher, exactly as a teacher recovers through the admin.

- ⚠️ **`forgot-password` returns an identical empty 200 on every path** — registered address, unknown address, student account, and SMTP failure alike. Any difference, *including a 500 from an unhandled send error*, turns the endpoint into a registered-account oracle. `ForgotPasswordHandler` therefore catches its own send failures.
- ⚠️ **`reset-password` returns one generic message** for a missing, expired, superseded, or already-used token — same reasoning as `LoginHandler`'s single login failure message.
- A new link **supersedes** any outstanding one (`InvalidateAllForUserAsync`), so a link that reached the wrong inbox stops working as soon as a new one is requested.
- Only the **SHA-256 hash** of the token is stored, never the token. It is deliberately *not* `IPasswordHasherService`: that hasher salts randomly, so the same token hashes differently every call and could never be looked up. A 256-bit random token needs no slow KDF — there is no guessable space to defend.
- An SMTP or `App:ClientBaseUrl` misconfiguration is written to the `Logs` table as `LogActionTypes.PasswordResetEmailFailed` with `Status=Error`. That row is the *only* trace: the caller always sees "if the address is registered, a link was sent", so an unlogged fault would be completely invisible.
- ⚠️ **Reset does not kill active sessions.** There is no JWT revocation here, so a session signed in with the old password stays valid until `Jwt:ExpiresHours` (8h) elapses. Acceptable for a forgotten password; do **not** assume reset covers a stolen session.

### Residual limitation: an admin row seeded before `User.Email` existed

The seeding block in `Program.cs` creates the admin **only when the username does not yet exist**, and writes `AdminUser:Email` only at that moment — for the same reason it does not overwrite the password: a config file must not silently revert a value the admin has since changed.

So an admin row created **before** the `AddUserEmail` migration has `Email = NULL` and cannot recover through the flow above, because `GetByEmailAsync` can never match `NULL`. Fixing that row is a one-time manual `UPDATE`, or a second admin username. Every admin seeded from here on recovers like everyone else.

## Teacher accounts: who creates whom

Self-registration is closed. There is no `POST /api/auth/register-teacher` — it was `[AllowAnonymous]`, so anyone who reached the URL created a full Teacher account with nobody approving it.

The permission chain is now closed end to end: **admin → teachers → students.** The admin creates teacher accounts through `TeachersController` (`[Authorize(Roles = "Admin")]`), a teacher creates student accounts through `AuthController`, and the admin herself is seeded from configuration. Nobody enters without someone above them creating the account.

- `User.Email` is nullable in the schema (students have none, and every pre-migration row has none) but **required for teachers at the validator level**. Teacher rows created before the migration show a "חסר מייל" tag on the teachers screen until the admin fills them in — they are the rows that would fail to recover a password later.
- `DeleteTeacherHandler` refuses to delete a teacher who owns lessons or courses, with a message that counts them. The FKs on `Lesson.TeacherId` / `Course.TeacherId` are `Restrict`, so without the guard the same case surfaces as an opaque 500 instead of an explanation.

### ⚠️ Known gap: deleting a teacher orphans four audit columns

Only `Student.UserId` (`SetNull`), `Lesson.TeacherId` and `Course.TeacherId` (both `Restrict`) are real FKs to `Users`. These four are **plain `int?` columns with no FK**, so nothing stops a delete and nothing nulls them out:

| Column | Written by |
| --- | --- |
| `Log.UserId` | every logged action |
| `LessonResult.FinalScoreOverriddenByUserId` | `LessonResult.OverrideFinalScore` |
| `Submission.ScoreOverriddenByUserId` | `Submission.OverrideScore` |
| `Submission.ExtraAttemptGrantedByUserId` | `Submission.GrantExtraAttempt` |

After a teacher is deleted these read "user 7 did X" where user 7 no longer exists — a degraded audit trail, not a crash or a broken query.

**In practice the exposure is small but not zero.** A teacher can only be deleted with 0 lessons and 0 courses, and grade overrides are reachable only through a lesson she owns — so the three override/attempt columns are nearly always empty for a deletable teacher. `Log.UserId` is the realistic one: any teacher who ever signed in left log rows behind.

The delete guard deliberately counts **only** lessons and courses, per `plan-adminTeacherManagement`. Widening it to these four (or backfilling them to `NULL` on delete) was left open rather than decided silently. Related and still open: there is no way to transfer lessons/courses between teachers, which is the intended way past the guard.

## Teacher notifications: aggregates, not submissions

The bell used to list individual graded submissions. It now carries four **class signals**, each an aggregate over one assignment: a structural requirement that failed for many, a test case that failed for many, an assignment nobody passed, and an assignment most students could not compile. The first two say what to reteach; the last two say the exercise itself is wrong.

**There is still no `Notification` entity and no notifications table — do not add one.** Every signal is computed on demand from `Submission` rows inside a date window, and the digest covers a fixed calendar day, so it is idempotent by date. An "already sent" table would only record what the date range already determines.

- **One aggregation, two deliveries.** `ClassSignalDetector` produces the `ClassSignalDto` list; the bell (`GET /api/notifications/class-signals`) and the daily digest email render the same records over the same window (`ClassSignalPeriod.PreviousDay`). The Hebrew sentence is built **server-side** so the two can never drift into sounding like two different alert systems.
- ⚠️ **The window is cut on `LastSubmittedAt`, not `GradedAt`.** `GradedAt` is written only by `MarkDone`/`OverrideScore`, so a submission that failed to compile or missed a blocking requirement carries `NULL` — filtering on it would silently drop exactly the broken-exercise signals.
- ⚠️ **`ClassSignalDto` is Teacher/Admin only**, enforced by `[Authorize(Roles = "Teacher,Admin")]` on the action. It says "8 of 12 failed requirement X" — worthless to a student and not hers to know. The student's bell stays on `graded-submissions`; there is deliberately no student path through `GetClassSignalsQuery`.
- ⚠️ A **hidden** test case's input is never put into a signal's text, only its position ("בדיקה 3"). The same sentence goes out by email, and email gets forwarded. Sample inputs are included because the student already sees them.
- "Passed" means **`ScoreBreakdown.AllCorePassed`**, not a score threshold. An assignment with a low `TestsAllocation` can reach a passing score on requirement points alone while every test failed — which is the case the signal exists to catch.
- `NobodyPassed` is **suppressed** when `CompilationFailedForMost` already fired on the same assignment. Both say "the exercise is broken", but the compilation signal already names the reason.
- `Advisory` requirements never raise a signal — they do not affect the grade.
- One `Submission` row per (student, assignment) is enforced by a unique index, so counting rows in a group *is* counting students. No `Distinct` needed.

### The digest job

`TeacherDigestJob` (Hangfire recurring, `Notifications:DigestHourLocal`, default 06:00 Israel time) sends one email per teacher who has both an address and something to report.

- 🔴 **A day with nothing to report sends no email at all.** Never a "no news today". That rule is what separates a digest people read from one they filter away.
- ⚠️ Each teacher's send is wrapped individually — one bad address must not abort the rest of the run.
- ⚠️ A failed or unconfigured send is written to `Logs` as `LogActionTypes.TeacherDigestEmailFailed` with `Status=Error`, which also emails the admin. `SmtpEmailSender` returns `false` silently when SMTP is unconfigured, so without that row a broken digest is indistinguishable from a quiet day.
- A teacher whose `Email` is `NULL` is skipped silently — that is a real case, not a fault (see the nullable-`Email` note above).
- The day boundary is **Israel time**, not UTC (`ClassSignalPeriod`). Submissions are stored in UTC, and cutting on UTC midnight would split an evening lesson across two digests. The Hangfire cron is UTC, so the configured local hour is converted at startup — meaning the job drifts by an hour across a DST change until the next restart. Acceptable for a daily digest; do not copy this into anything hour-sensitive.
- ⚠️ Thresholds live in **one** place (`ClassSignalThresholds`, bound from `Notifications:ClassSignals`) — at least 3 affected students **and** at least 50% of those who submitted; `NobodyPassed` additionally requires 3 submissions. The absolute minimum is what stops a class of four from firing on every hiccup: without it, 2 of 3 is 67% and clears any ratio.
