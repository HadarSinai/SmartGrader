# Plan: Admin-Managed Teacher Accounts + Closing Self-Registration

## TL;DR

`/register` is open to anyone who reaches the URL: they create a full **Teacher** account with nobody approving
it. `POST /api/auth/register-teacher` is `[AllowAnonymous]` (`AuthController.cs:39-48`), so hiding the screen in
the client closes nothing — the endpoint answers a direct request just fine.

Closing it means someone must be able to create teacher accounts instead. The system already has the exact
pattern one level down: a teacher creates a student's account from `student-form.component.ts`. This plan lifts
that pattern one level up — **the admin creates a teacher's account** — and then deletes self-registration.

The result is a closed permission chain: admin → teachers → students. Nobody enters without someone above them
creating the account.

**Then:** [plan-passwordRecovery](plan-passwordRecovery.prompt.md) — ship it close behind. Once registration is
closed, every teacher who forgets her password becomes a manual task for the admin.

---

## ⚠️ MANDATORY — load the relevant skill before writing code

| Work | Skill |
|---|---|
| Commands, queries, handlers, validators | `backend-mediatr-query-handler-pattern` |
| Repository methods (`GetByRoleAsync`, `CountByTeacherIdAsync`) | `backend-repository-query-pattern` |
| `TeachersController` and route auth | `backend-controller-endpoint-pattern` |
| `TeacherProfile` mapping | `backend-automapper-profile-pattern` |
| The rule that `PasswordHash` never reaches a DTO | `backend-role-based-field-redaction` |
| Teachers list page | `client-list-table-pattern` |
| Confirm dialogs, inline validation, Hebrew copy | `client-flow-fix-implementation-pattern` |
| Styling the new pages — existing tokens only, no new colors | `client-design-token-rollout-pattern` |

All user-facing copy is Hebrew and addresses the user in the **feminine** form — this is a girls' school.
Follow [server/CLAUDE.md](../../server/CLAUDE.md) and [client/CLAUDE.md](../../client/CLAUDE.md).

---

## Background facts that constrain the design

- **There is no `Teacher` entity.** One `Users` table with a `Role` column (`User.cs:3-8` —
  `Teacher=0, Student=1, Admin=2`). A teacher is a `User` with `Role = Teacher`.
- **The admin is seeded from configuration** (`Program.cs:107-132`, `AdminUser:Username/Password`). Closing
  self-registration therefore locks nobody out — an admin always exists.
- ⚠️ **`Lesson.TeacherId` and `Course.TeacherId` are plain `int` columns with no FK to `Users`.**
  `GradeSheetContext.cs:109-138` configures no `HasOne` for them. Deleting a teacher will **not** be blocked by
  the database — it will silently leave lessons and courses pointing at an id that no longer exists. This is why
  the delete guard in the handler is a requirement, not a nicety.
- `adminGuard` and one admin screen (`/logs`) already exist, hosted inside the teacher shell. That is the
  pattern to copy for placement and nav.
- `User` exposes only `SetPasswordHash`; every property has a `private set` and `User.Create` is the sole
  factory. Renaming and setting an email each need a new domain method.

## Decisions already made

| Topic | Decision |
|---|---|
| Who can reach the teachers screen | Admin only |
| Scope of actions | Full CRUD including delete |
| Deleting a teacher who owns content | **Blocked**, with a message that counts what exists |
| The public registration endpoint | Delete the old code; write `CreateTeacher` fresh |
| Teacher email | **Required.** It is the recovery identifier that [plan-passwordRecovery](plan-passwordRecovery.prompt.md) looks the account up by |
| Student email | **None.** A locked-out student asks her teacher, exactly as a teacher asks the admin. The student form and the Excel import are untouched |

---

## Phase 1 — The admin's teachers screen

### 1.1 Domain (`server/Domain/Entities/User.cs`)

- Add `public string? Email { get; private set; }`.
- Add `SetFullName(string fullName)` and `SetEmail(string? email)` — trim, and normalize the email to
  lowercase the way `User.Create` already normalizes `Username`.
- Extend the `User.Create` factory with an optional email parameter.
- Do **not** add `SetUsername` — the login username is deliberately immutable.

### 1.2 EF configuration + migration (`server/Infrastructure/Data/GradeSheetContext.cs:109-138`)

`Email` is nullable in the database — students have none, and every row that exists today has none. It is
required for teachers at the *validator* level, not the schema level.

Add a unique index on `Email`. SQLite treats `NULL`s as distinct, so any number of students and legacy rows
coexist fine, while `forgot-password` can still resolve an email to exactly one account.

Then `dotnet ef migrations add AddUserEmail`.

⚠️ **Teachers created before this migration will have `Email = NULL`.** The teachers list must flag them
visibly (a "חסר מייל" tag on the row), and the admin fills them in through the edit form. Do not silently leave
them looking healthy — they are the rows that will fail to recover a password later.

### 1.3 Repositories — skill: `backend-repository-query-pattern`

In `server/Domain/Abstractions/IUserRepository.cs` + `server/Infrastructure/Repositories/UserRepository.cs`:

```csharp
Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken ct = default);
Task<bool> ExistsByEmailAsync(string email, int? excludingUserId, CancellationToken ct = default);
Task UpdateAsync(User user, CancellationToken ct = default);
```

`ExistsByEmailAsync` normalizes with `Trim().ToLowerInvariant()`, exactly as `ExistsByUsernameAsync` already
does, and takes `excludingUserId` so that editing a teacher without changing her email does not collide with
herself.

`UpdateAsync` follows `StudentRepository.cs:77-81` (`_context.Users.Update(user)`). It is required because the
existing `GetByIdAsync` uses `AsNoTracking()`, so the entity comes back detached and change tracking will not
pick up the edit on its own.

In `ILessonRepository` / `ICourseRepository` and their implementations, following
`SubmissionRepository.cs:86-87` exactly:

```csharp
Task<int> CountByTeacherIdAsync(int teacherId, CancellationToken ct = default);
```

### 1.4 DTOs — new file `server/Application/Dtos/Teacher/TeacherDtos.cs`

```csharp
public record TeacherResponseDto(int Id, string FullName, string Username, string? Email,
                                 DateTime CreatedAt, int LessonsCount, int CoursesCount);
public record CreateTeacherRequestDto(string FullName, string Username, string Email, string Password);
public record UpdateTeacherRequestDto(string FullName, string Email);
public record ResetTeacherPasswordRequestDto(string NewPassword);
```

`Email` is nullable on the *response* (legacy rows) and non-nullable on the *requests* (new writes must supply
it). ⚠️ `PasswordHash` appears in no DTO and is never mapped — see `backend-role-based-field-redaction`.

### 1.5 Use cases — new folder `server/Application/UseCases/Teachers/`

Skill: `backend-mediatr-query-handler-pattern`.

| Use case | Logic |
|---|---|
| `GetTeachers` | `GetByRoleAsync(UserRole.Teacher)`, plus lesson/course counts per teacher |
| `CreateTeacher` | Mirrors `RegisterTeacherHandler.cs`: `ExistsByUsernameAsync` and `ExistsByEmailAsync` → `UniqueConstraintException` (409), then `User.Create(..., UserRole.Teacher)`. **Difference: it issues no token** — the admin stays signed in as the admin |
| `UpdateTeacher` | `SetFullName` + `SetEmail`, with the same email-uniqueness check |
| `ResetTeacherPassword` | `SetPasswordHash(_passwordHasher.Hash(dto.NewPassword))` |
| `DeleteTeacher` | Guard, then delete — see below |

`DeleteTeacherHandler` mirrors `DeleteStudentHandler.cs:40-60`, including a `DescribeWork` helper that builds
the Hebrew message:

```csharp
var lessons = await _lessonRepository.CountByTeacherIdAsync(id, ct);
var courses = await _courseRepository.CountByTeacherIdAsync(id, ct);
if (lessons > 0 || courses > 0)
    throw new BusinessRuleException(
        $"לא ניתן למחוק את {user.FullName} — יש לה {DescribeWork(lessons, courses)} " +
        "שיישארו בלי בעלים. יש להעביר או למחוק אותם קודם.");
```

Plus two guards that have no equivalent on the student side:

- `user.Role != UserRole.Teacher` → `BusinessRuleException`. This screen does not delete students, and it does
  not delete the admin.
- `user.Id == CurrentUserId` → blocked. An admin does not delete herself.

Every command gets a FluentValidation validator, reusing `.Username()` and `.Password()` from
`UsernameRuleExtensions.cs` and `PasswordRuleExtensions.cs`, plus `.EmailAddress()` and `NotEmpty()` on the
email. Do not re-implement any of these inline — `ImportStudentsHandler` already did that and lost the
Hebrew-characters check as a result.

### 1.6 AutoMapper

`server/Application/Common/Mapping/TeacherProfile.cs`, skill: `backend-automapper-profile-pattern`. Maps
`User → TeacherResponseDto`; the counts are supplied by the handler, not computed in the profile.

### 1.7 Controller

`server/Api/Controllers/TeachersController.cs`, skill: `backend-controller-endpoint-pattern`.

Extends `ControllerBase`, **not** `ApiControllerBase` — there is no ownership scoping here.
`[Authorize(Roles = "Admin")]` at the **class** level, exactly like `LogsController.cs:12`.

```
GET    /api/teachers
POST   /api/teachers               → CreatedAtAction
PUT    /api/teachers/{id:int}
POST   /api/teachers/{id:int}/password
DELETE /api/teachers/{id:int}
```

### 1.8 Client

Skills: `client-list-table-pattern`, `client-flow-fix-implementation-pattern`,
`client-design-token-rollout-pattern`.

- `client/src/app/models/teacher.model.ts` — mirrors the DTOs 1:1.
- `client/src/app/services/teachers.service.ts` — in the style of `students.service.ts`, through `ApiClient`.
- `client/src/app/pages/teachers/teachers-list.component.{ts,html,css}` — mirrors
  `students-list.component.html`: search, `⋯` overflow menu with edit/delete, `ConfirmationService` for delete,
  RTL paginator. Columns: name, username, email, lessons, courses. A row whose email is empty shows a
  **"חסר מייל"** warning tag. **No** import/export and **no** multi-select — neither belongs on this screen.
- `client/src/app/pages/teachers/teacher-form.component.ts` — mirrors `student-form.component.ts`, reusing
  `PasswordChecklistComponent` and the `sgNoHebrew` directive. Three modes:
  - **Create**: full name + username + email + password, all required.
  - **Edit**: full name + email; the username renders as read-only text.
  - **Reset password**: a separate button on the edit form, in the style of the "יצירת חשבון התחברות" flow at
    `student-form.component.ts:474-508`.
- `client/src/app/app.routes.ts` — `teachers`, `teachers/new`, `teachers/:id/edit` inside
  `AppLayoutComponent`, each with `canActivate: [adminGuard]`, like `logs` at `app.routes.ts:145-149`.
- `client/src/app/components/layout/topbar.component.ts:70-75` — add a "מורות" link inside the existing
  `@if (auth.isAdmin())` block, next to "יומן מערכת".

---

## Phase 2 — Close self-registration

Only after Phase 1 works end to end.

**Delete on the server:**

- The whole `server/Application/UseCases/Auth/RegisterTeacher/` folder (command, validator, handler).
- `RegisterTeacherRequestDto` from `server/Application/Dtos/Auth/AuthDtos.cs:5`.
- The `RegisterTeacher` action from `AuthController.cs:39-48`.

**Delete on the client:**

- `client/src/app/pages/auth/register.component.ts` — but ⚠️ **first move the `passwordsMatch` cross-field
  validator** (`register.component.ts:21-27`) into `client/src/app/core/validators/password.validator.ts`
  alongside `passwordStrengthValidator`. [plan-passwordRecovery](plan-passwordRecovery.prompt.md) needs it, and
  deleting the file outright would lose it.
- The `{ path: "register", ... }` route at `app.routes.ts:37`.
- `registerTeacher()` from `auth.service.ts:49-53` and `RegisterTeacherRequestDto` from `auth.model.ts`.
- The `/api/auth/register-teacher` entry in `api-error.interceptor.ts` — the list that suppresses the toast so
  the screen can render an inline error. The screen is gone; the exemption goes with it.

**Copy change:** at `login.component.ts:92-95`, replace `מורה חדשה? הרשמה` with plain text and no link:
`אין לך חשבון? יש לפנות למנהלת המערכת`. This answers the question the user would otherwise ask, instead of
leaving a dead end. The next plan adds the "שכחתי סיסמה" link beside it.

---

## Verification

**Server** — `dotnet build server/SmartGrader.sln`, then against the running API:

| # | Scenario | Expected |
|---|---|---|
| 1 | Sign in as admin → `POST /api/teachers` → sign in with the new credentials | Works |
| 2 | Same `POST` with an existing username, then with an existing email | 409 both times |
| 3 | `POST /api/teachers` with no email, or a malformed one | 400 |
| 4 | Sign in as a **teacher** → `GET /api/teachers` | **403** |
| 5 | `POST /api/teachers` with no token at all | **401** |
| 6 | `POST /api/auth/register-teacher` after Phase 2 | **404** |
| 7 | Delete a teacher who owns a lesson | 400, message counts what exists |
| 8 | Delete a teacher who owns nothing | 204 |
| 9 | Admin deletes her own account | 400 |
| 10 | `DELETE /api/teachers/{id}` where the id belongs to a **student** user | 400, not a deleted student |

**Client** — `cd client && npm run build`, then:

1. Signed in as admin → "מורות" appears in the nav → create, rename, change email, reset password, delete.
2. A teacher row created before the migration shows the "חסר מייל" tag until the admin fills it in.
3. Signed in as a teacher → "מורות" is absent, and typing `/teachers` manually redirects away.
4. `/register` no longer routes, and the link is gone from the login screen.
5. RTL renders correctly at 360px / 768px / 1280px.

## Out of scope

- **Password recovery** — [plan-passwordRecovery](plan-passwordRecovery.prompt.md).
- **A personal area for changing your own details** — [plan-personalArea](plan-personalArea.prompt.md).
- **Transferring lessons and courses between teachers** — the way to get past the delete guard. Build it if and
  when it turns out to be needed.
- **A real FK from `Lesson.TeacherId` / `Course.TeacherId` to `Users`.** It would fix the dangling-reference
  problem at the root, but needs a migration and a check of existing data. The handler guard covers the case in
  practice.
