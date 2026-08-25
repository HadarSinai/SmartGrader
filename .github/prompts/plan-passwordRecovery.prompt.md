# Plan: Password Recovery by Email (teachers and admins)

## TL;DR

The system has **no account-recovery path at all.** A teacher who forgets her password depends entirely on the
admin being available; an admin who forgets hers cannot get back in at all — the seeding block only creates the
admin when the username does not already exist, so changing `AdminUser:Password` after the first run does
nothing. Once [plan-adminTeacherManagement](plan-adminTeacherManagement.prompt.md) closes self-registration,
every lockout becomes a manual task for one person.

This adds the standard flow: request a link by email, follow a one-time link, choose a new password.

**Requires first:** [plan-adminTeacherManagement](plan-adminTeacherManagement.prompt.md) — it adds the
`User.Email` column this reads, and moves the `passwordsMatch` validator that the reset screen reuses.

**Scope:** teachers and admins only. A student has no email; she recovers through her teacher, exactly as a
teacher recovers through the admin.

---

## ⚠️ MANDATORY — load the relevant skill before writing code

| Work | Skill |
|---|---|
| The two handlers and their validators | `backend-mediatr-query-handler-pattern` |
| `IPasswordResetTokenRepository`, `GetByEmailAsync` | `backend-repository-query-pattern` |
| The two anonymous, rate-limited endpoints | `backend-controller-endpoint-pattern` |
| Never returning a token or hash in a response | `backend-role-based-field-redaction` |
| Inline errors and Hebrew copy on the two screens | `client-flow-fix-implementation-pattern` |
| Styling both screens — existing tokens only | `client-design-token-rollout-pattern` |

Hebrew copy, feminine form. Follow [server/CLAUDE.md](../../server/CLAUDE.md) and
[client/CLAUDE.md](../../client/CLAUDE.md).

## What already exists — reuse it, do not rebuild it

| Component | Where |
|---|---|
| SMTP transport, credentials, `Smtp` config section | `SmtpEmailSender` |
| Rate limiting on auth routes | the `"auth"` policy in `Program.cs:74-85` — 5 req/min per IP |
| Password hashing (PBKDF2, per-password random salt embedded in the stored string) | `IPasswordHasher` / `PasswordHasherService` |
| Password strength rules | `PasswordRuleExtensions` (server), `password.validator.ts` (client) |
| Password checklist UI | `PasswordChecklistComponent` |
| Anti-enumeration precedent | `LoginHandler` returns one generic error rather than confirming a username |

## What exists only partly — extend it

⚠️ **`IEmailSender` can only send to the admin.** Its sole method is
`SendToAdminAsync(subject, body)`, and `SmtpEmailSender` hardcodes the recipient from `AdminUser:Email`.
Add `SendAsync(string to, string subject, string body, CancellationToken ct = default)` to the interface and
implement it; keep `SendToAdminAsync` as it is so the existing error-alert path is untouched.

⚠️ **`SmtpEmailSender` no-ops silently when SMTP is unconfigured** — it logs a warning and returns, which is
correct for error alerts but dangerous here. Combined with the always-200 response, a teacher would see
"a link has been sent" while nothing was ever sent, and the only trace would be a log line. Therefore:

- `ForgotPasswordHandler` must distinguish "no such account" (say nothing, return 200) from "SMTP is not
  configured" (a real operational fault). Write the latter to the `Logs` table through the existing logging
  path, at a level the admin's log screen surfaces — do not let it vanish into the console.
- Do **not** leak the difference to the caller. The response stays identical.

⚠️ **The seeded admin has an email in configuration but not on her row.** `AdminUser:Email` already exists and
is where error alerts go. Extend the seeding block at `Program.cs:107-132` to also write it into the admin
`User`'s new `Email` column, so the admin can recover her own password through the same flow as everyone else.
This is the one account that currently has no way back in at all.

---

## Steps

### 1. Domain + migration

New entity `server/Domain/Entities/PasswordResetToken.cs`:

```csharp
public int Id;  public int UserId;  public string TokenHash;
public DateTime ExpiresAt;  public DateTime? UsedAt;  public DateTime CreatedAt;
```

Protected constructor + a `Create` factory, like every other entity. Register the `DbSet` and an index on
`TokenHash` in `GradeSheetContext`, then `dotnet ef migrations add AddPasswordResetTokens`.

⚠️ **Store a hash of the token, never the token itself** — same reasoning as passwords. The raw token exists
only in the emailed link. Reuse `IPasswordHasher` rather than introducing a second hashing mechanism.

### 2. Repositories — skill: `backend-repository-query-pattern`

New `IPasswordResetTokenRepository`: `GetByTokenHashAsync`, `AddAsync`, `InvalidateAllForUserAsync`.

On `IUserRepository`, add `GetByEmailAsync(string email, CancellationToken ct = default)`, normalizing with
`Trim().ToLowerInvariant()` exactly as `GetByUsernameAsync` already does.

### 3. Configuration

The server builds a link into the client, which it cannot infer. Add `App:ClientBaseUrl` to `appsettings.json`
with a placeholder; the real value goes in `appsettings.Development.json`, which is gitignored — see the
Secrets section of `server/CLAUDE.md`.

### 4. Use cases — skill: `backend-mediatr-query-handler-pattern`

`server/Application/UseCases/Auth/ForgotPassword/`:

1. `GetByEmailAsync`. If there is no user, or the user is a `Student`, or the user has no email — **return
   successfully anyway**. Never reveal whether an address is registered.
2. `InvalidateAllForUserAsync` — a newly requested link supersedes any outstanding one.
3. Generate a cryptographically random token (`RandomNumberGenerator`, URL-safe), store its hash with
   `ExpiresAt = UtcNow + 1 hour`.
4. Send through `IEmailSender`, linking to `{ClientBaseUrl}/reset-password?token={rawToken}`. Hebrew body,
   feminine form, stating the one-hour expiry.

`server/Application/UseCases/Auth/ResetPassword/`: look the token hash up; reject when missing, expired, or
already used — `BusinessRuleException` with **one generic Hebrew message for all three**, never explaining
which. On success, `SetPasswordHash`, stamp `UsedAt`, and save in a single `SaveChangesAsync`.

⚠️ **Known limitation, to state plainly rather than hide:** there is no JWT revocation in this system, so a
session already signed in with the old password stays valid until its 8-hour expiry. Acceptable here — the
threat model is a forgotten password, not a stolen session — but write it in a comment so nobody later assumes
reset kills active sessions.

### 5. Endpoints — skill: `backend-controller-endpoint-pattern`

On `AuthController`, both `[AllowAnonymous]` **and** `[EnableRateLimiting("auth")]`:

```
POST /api/auth/forgot-password    (string Email)                  → always 200
POST /api/auth/reset-password     (string Token, string NewPassword)
```

The rate limiter is not optional here: without it, `forgot-password` is an open email-sending relay.

### 6. Client — two public screens styled like `login.component.ts`

- `/forgot-password` — one email field. On submit, always renders the same confirmation
  ("אם הכתובת רשומה במערכת, נשלח אליה קישור") regardless of the server's outcome.
- `/reset-password` — reads `token` from the query string, takes a new password + confirmation, and reuses
  `PasswordChecklistComponent`, `passwordStrengthValidator` and the `passwordsMatch` validator that
  [plan-adminTeacherManagement](plan-adminTeacherManagement.prompt.md) moved into `core/validators/`. On
  success, redirect to `/login` with a success toast.
- Add both routes to `app.routes.ts` next to `login`, unguarded.
- Add a "שכחתי סיסמה" link to `login.component.ts`.
- Add both paths to the `api-error.interceptor.ts` inline-error list, the same way `/api/auth/login` is
  exempted from the global toast.

---

## Verification

`dotnet build server/SmartGrader.sln`, `cd client && npm run build`, then:

| # | Scenario | Expected |
|---|---|---|
| 1 | `forgot-password` with a registered teacher email | 200, email arrives, link works once |
| 2 | `forgot-password` with an address that does not exist | **200 with the identical response** — no enumeration |
| 3 | `forgot-password` for a **student's** account | 200, no email sent |
| 4 | `forgot-password` for a teacher whose email is still `NULL` | 200, no email, no crash |
| 5 | Reuse the same reset link a second time | 400 |
| 6 | Tamper with the token; separately, use a link past its expiry | 400, **same generic message** for both |
| 7 | Request a second link, then try the first one | 400 — the newer link supersedes it |
| 8 | Six `forgot-password` calls in a minute | 429 on the sixth |
| 9 | Full round trip in the browser: link → new password → sign in | Works |
| 10 | RTL on both screens at 360px / 768px / 1280px | Correct |

## Out of scope

- **Student self-recovery.** She asks her teacher.
- **JWT revocation on reset** — documented as a known limitation in step 4.
- **Email notifications to teachers** — [plan-teacherNotifications](plan-teacherNotifications.prompt.md).
