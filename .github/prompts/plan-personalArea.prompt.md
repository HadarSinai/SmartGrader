# Plan: Personal Area (change your own details and password)

## TL;DR

Every user's own details are currently changeable only by someone above them: a teacher's by the admin, a
student's by her teacher. Add one screen where a signed-in user maintains her own account.

One component, gated by role, so "a student changes only her password" needs no second screen:

| Role | Full name | Email | Password |
|---|---|---|---|
| Teacher / Admin | ✅ | ✅ | ✅ |
| Student | ❌ | — | ✅ |

A student cannot change her name because `User.FullName` and `Student.FullName` are two separate fields
(`Student.cs:6`). Changing one would desync what she sees from what her teacher sees. She has no email at all —
see [plan-adminTeacherManagement](plan-adminTeacherManagement.prompt.md).

**Requires first:** [plan-adminTeacherManagement](plan-adminTeacherManagement.prompt.md) for the `User.Email`
column and the `SetFullName` / `SetEmail` domain methods. Independent of
[plan-passwordRecovery](plan-passwordRecovery.prompt.md) — ship in either order.

Fully deferrable. Nothing else depends on it.

---

## ⚠️ MANDATORY — load the relevant skill before writing code

| Work | Skill |
|---|---|
| The two handlers and their validators | `backend-mediatr-query-handler-pattern` |
| The two `me` endpoints | `backend-controller-endpoint-pattern` |
| Inline validation and Hebrew copy | `client-flow-fix-implementation-pattern` |
| Styling the screen — existing tokens only | `client-design-token-rollout-pattern` |
| The student's half, under `/my` | `client-student-area-pattern` |

Hebrew copy, feminine form. Follow [server/CLAUDE.md](../../server/CLAUDE.md) and
[client/CLAUDE.md](../../client/CLAUDE.md).

---

## Steps

### 1. Server — two actions on `AuthController`

Both operate on `CurrentUserId` from the token and **never** on an id from the body or the route.

- `PUT /api/auth/me` — change full name and email. `[Authorize(Roles = "Teacher,Admin")]`. Reuses the same
  `ExistsByEmailAsync(email, excludingUserId)` check as `UpdateTeacher`.
- `POST /api/auth/me/password` — `(string CurrentPassword, string NewPassword)`. Verifies the current password
  through `IPasswordHasher` before changing it; a mismatch throws `BusinessRuleException`. Open to all roles.

Requiring the current password is deliberate: it is what stops someone at an unattended, still-signed-in
machine from taking the account over.

Validators reuse `.Password()` from `PasswordRuleExtensions` and `.EmailAddress()`.

### 2. Client — one component on two routes

`client/src/app/pages/profile/profile.component.ts`, registered at:

- `/profile` under `AppLayoutComponent`, with `authGuard`
- `/my/profile` under `StudentLayoutComponent`

The name and email fields are wrapped in `@if (!auth.isStudent())`. Reuses `PasswordChecklistComponent`, the
`sgNoHebrew` directive, and the `passwordsMatch` validator from `core/validators/`.

Add an entry point in both layouts near the sign-out button (`topbar.component.ts:95-105` and
`student-layout.component.ts`).

⚠️ After a successful name change, refresh the `sg_user` blob in localStorage through `AuthService` — otherwise
the name in the top bar stays stale until the next sign-in.

---

## Verification

`dotnet build server/SmartGrader.sln`, `cd client && npm run build`, then:

| # | Scenario | Expected |
|---|---|---|
| 1 | Teacher changes her name | Saved; the top bar updates without a re-login |
| 2 | Teacher changes her email to one another teacher already uses | 409 |
| 3 | Teacher changes her password with the wrong current password | 400, password unchanged |
| 4 | Teacher changes her password correctly, signs out, signs in with the new one | Works |
| 5 | Student opens `/my/profile` | Password only — no name and no email field |
| 6 | Student calls `PUT /api/auth/me` directly | **403** |
| 7 | `POST /api/auth/me/password` with no token | **401** |
| 8 | RTL at 360px / 768px / 1280px, in both layouts | Correct |

## Out of scope

- **Changing your own username.** Deliberately immutable; there is no `SetUsername` on `User`.
- **A student changing her own name.** It would desync from her `Student` record — her teacher maintains it.
- **Profile pictures, phone numbers, notification preferences.** No demand yet.
