# Work Plan — SmartGrader: Authentication & User Management (Auth)

> Version 3 · 14.07.2026 · End-to-end: backend (.NET 8) + login screen (Angular)

## Decisions Made (agreed with Hadar)

| Topic                 | Decision                                                                                                                                                   |
| --------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Login method          | **Username (email) + password** — Identity + JWT. Microsoft account — maybe in the future, not now                                                         |
| Who logs in           | Both teachers and students                                                                                                                                 |
| Registration          | **No self-registration for students**. A student account is created only by a teacher, or via data import                                                  |
| Student login         | With the email + password the teacher created for her                                                                                                      |
| DB model              | **Two tables**: `User` (authentication only) + existing `Student` (academic), linked via `Student.UserId`                                                  |
| Teacher registration  | **Teachers can self-register** — a registration screen + `POST /api/auth/register-teacher`                                                                 |
| Data import           | **Deferred** — CSV/Excel import of students is a follow-up phase (Phase 5), not in scope now                                                               |
| Student password      | Teacher sets the student's password at account creation; **no forced password change on first login**                                                      |
| Scope                 | End-to-end: full backend + Angular login screen + role-based redirect                                                                                      |
| Client infrastructure | **In scope**: token (Bearer) interceptor, route guards, protected routes — replacing today's open-routes client (`ApiClient` + `ApiErrorInterceptor` only) |
| Performance           | Verified — JOIN on an indexed FK is negligible; JWT avoids a DB hit per request; no concern                                                                |

---

## Current State (verified in code)

- **No auth at all** — the entire API is open; no Identity, no JWT, no users table.
- **No teacher entity** — `Lesson.TeacherName` is just a string.
- **`Student` exists** (`FullName`, `ClassName`) with relations to `Submission` and `LessonResult` — it stays, only gains a `UserId`.
- DB: **SQLite** + EF Core Migrations; DI in `server/Infrastructure/DependencyInjection.cs`.
- Client: simple `ApiClient` + `ApiErrorInterceptor`; no token interceptor; no guards; routes are open — **all of these gaps are closed in Phase 3** (auth interceptor, guards, protected routes).

---

## Target Architecture

### Data Model

```
Users
├── Id (PK)
├── Email        (unique, index)
├── PasswordHash (bcrypt/Identity hasher — never a plain-text password)
├── FullName
├── Role         ("Teacher" | "Student")
└── CreatedAt

Students (existing — addition only)
└── UserId (FK → Users.Id, nullable at first, unique index)
```

- Teacher = a `User` record only (`Role=Teacher`).
- Student = a `User` (`Role=Student`) + a linked `Student`. Creating a student via the teacher creates both in a single transaction.
- **Deleting a student** (existing delete flow) also deletes the linked `User` (cascade) — so she can no longer log in.
- Existing `Student` rows keep `UserId = null` — they simply can't log in until the teacher creates an account for them (Phase 4).
- `Lesson.TeacherName` — stays for now; a `Lesson.TeacherUserId` link = future improvement, out of scope.

### Authentication Flow

1. `POST /api/auth/login` (anonymous) → verify email+password → return a **JWT** with claims: `sub` (UserId), `role`, `studentId` (for a student), `name` + expiry (e.g. 8 hours).
2. The client stores the token (localStorage) and attaches `Authorization: Bearer` to every request (interceptor).
3. All controllers get `[Authorize]`; management endpoints (creating students, lessons, assignments) — `[Authorize(Roles="Teacher")]`; a student can access only her own data (validate the `studentId` claim against the route).

---

## Skills to Use (existing — load before each phase)

| Phase | Skill                                    | Used for                                                                                                           |
| ----- | ---------------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| 1     | `backend-repository-query-pattern`       | `IUserRepository` in Domain/Abstractions + `UserRepository` in Infrastructure/Repositories                         |
| 2     | `backend-mediatr-query-handler-pattern`  | `LoginCommand` / `RegisterTeacherCommand` / `CreateStudentAccountCommand` + Handlers + FluentValidation validators |
| 2     | `backend-controller-endpoint-pattern`    | `AuthController` actions, route templates, status codes                                                            |
| 2     | `backend-automapper-profile-pattern`     | `AuthResponse` mapping profile (if mapping is needed)                                                              |
| 3–4   | `client-flow-fix-implementation-pattern` | Inline validation on login/register/student forms, Hebrew gender-neutral copy                                      |
| 3–4   | `client-design-token-rollout-pattern`    | Styling login/register pages with the existing spec.md tokens (RTL, breakpoints)                                   |

No new skills/agents are created for this task (decided 14.07): auth is a one-time feature, not a recurring pattern; this plan file is the execution script. JWT setup, rate limiting, guards, and interceptor details are fully specified in the phases below.

---

## Work Phases

### Phase 1 — Backend: Identity Infrastructure

1. New `User` entity in `server/Domain/Entities/User.cs` + `IUserRepository` in `Domain/Abstractions` + implementation in `Infrastructure/Repositories` (per backend-repository-query-pattern).
2. Add `UserId` (nullable, unique) to `Student` + EF configuration in `GradeSheetContext`.
3. New migration (`AddUsersAndStudentUserLink`) on SQLite.
4. Password hashing: `Microsoft.AspNetCore.Identity.PasswordHasher<User>` (without the full Identity package — lightweight and sufficient).
5. JWT configuration: `Microsoft.AspNetCore.Authentication.JwtBearer` package; `Jwt:Key/Issuer/Audience/ExpiresHours` in `appsettings.json` (the Key in user-secrets for development, not in code!).

### Phase 2 — Backend: Use Cases + Controller (following the existing CQRS patterns)

6. `LoginCommand` + Handler + Validator — returns `AuthResponse { Token, FullName, Role, StudentId? }`; a generic error "Invalid email or password" (without revealing which one).
7. `CreateStudentAccountCommand` (Teacher only) — creates `User(Role=Student)` + a linked `Student` in one transaction; duplicate email → 409.
8. `RegisterTeacherCommand` + Handler + Validator — teacher self-registration: creates `User(Role=Teacher)`; duplicate email → 409; returns `AuthResponse` (logged in immediately after registering).
9. **Password policy** in the validators: minimum 8 characters (email + password validators shared by Register/CreateStudent); emails normalized to lowercase before save/lookup.
10. `AuthController`: `POST /api/auth/login` (AllowAnonymous), `POST /api/auth/register-teacher` (AllowAnonymous), `POST /api/auth/students` (Roles=Teacher), `GET /api/auth/me`.
11. Enable `UseAuthentication()` + `UseAuthorization()` in `Program.cs` + Swagger with an Authorize button.
12. **Rate limiting on auth endpoints** — .NET 8 built-in rate limiter in `Program.cs`: e.g. max 5 login attempts per minute per IP on `POST /api/auth/login` (brute-force protection).
13. **Swagger restricted to Development** — wrap `UseSwagger()`/`UseSwaggerUI()` in `if (app.Environment.IsDevelopment())` (currently unconditional — exposed in production).
14. **Protect the Hangfire dashboard** — `/hangfire` is currently open to everyone; restrict it to development only (or Teacher role) once auth exists.
15. `[Authorize]` on all existing controllers: `StudentsController`, `LessonsController`, `SubmissionController`, `LessonResultController` — writes: Teacher; reading own data: Student with `studentId` claim enforcement. **Exception**: `POST` submission (submitting code) is allowed for a Student — with her own `studentId` only. Rationale: submitting code is a write operation; a blanket Teacher-only write rule would break the student area.

### Phase 3 — Client: Login Screen + Infrastructure

16. `AuthService` (`client/src/app/services/auth.service.ts`): `login()`, `logout()`, `token`, `role`, `isLoggedIn` (signals).
17. `authInterceptor`: attaches Bearer to every request; on 401 → clear token + navigate to `/login` (integrates with the existing `ApiErrorInterceptor`).
18. `login` page (`pages/auth/login.component.ts`) per the existing spec in `docs/ux/master-spec.md` (Hebrew UI, RTL, Warm Minimal, inline validation per the assignment-form pattern).
19. `register` page for teachers (`pages/auth/register.component.ts`): full name, email, password + confirmation; on success → logged in and routed to `/dashboard`; link between login ↔ register.
20. Guards: `authGuard` (not logged in → `/login`), `teacherGuard`, `studentGuard`; update `app.routes.ts` — the teacher area under `teacherGuard`.
21. Topbar: real user name + logout button.
22. Post-login routing: Teacher → `/dashboard`; Student → student area (at this stage: a minimal "My Journey" page or her submissions — per master-spec). **Student routes take `studentId` from the token claims, never from the URL** — a student cannot browse another student's data by changing the address (enforced on the server too).

### Phase 4 — "Create Student Account" Screen for the Teacher

23. Extend the existing student form (or a separate action in the list): email + password fields → calls `POST /api/auth/students`. The password the teacher sets is the student's permanent password (no forced change on first login).
24. Handle scenarios: email taken, existing student without an account ("Create account" action in the list).

### Phase 5 — Data Import (deferred — decided: follow-up phase, not in scope now)

25. `POST /api/auth/students/import` — CSV file (name, class, email) → creates User+Student per row + error report; file-upload UI.

### Phase 6 — Verification & Cleanup

26. Clean `dotnet build` + migrations run; clean `ng build`.
27. Manual tests: teacher registration → dashboard; teacher login → dashboard; student login → student area; direct access to a teacher URL as a student → blocked; 401 → redirect to login; Hebrew toasts.
28. Security: no passwords/tokens in logs; JWT Key not in git; CORS configured correctly; `/hangfire` and Swagger not publicly accessible.
29. Deployment config: `docker-compose.yml` currently runs only the Judge0 infrastructure — the API is not containerized. When the API is deployed (container/host), pass the key as the `Jwt__Key` environment variable; until then the dev key lives in `appsettings.Development.json` only.

---

## Key Files

| Area           | Files                                                                                                                                                                                        |
| -------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Domain         | `server/Domain/Entities/User.cs` (new), `Student.cs` (add UserId), `Abstractions/IUserRepository.cs`                                                                                         |
| Infrastructure | `Data/GradeSheetContext.cs`, `Repositories/UserRepository.cs`, `DependencyInjection.cs`, new migration                                                                                       |
| Application    | `UseCases/Auth/**` (Login, RegisterTeacher, CreateStudentAccount), DTOs, Validators                                                                                                          |
| Api            | `Controllers/AuthController.cs` (new), `Program.cs` (JWT), `appsettings.json`, `[Authorize]` on existing ones                                                                                |
| Client         | `services/auth.service.ts`, `core/http/auth.interceptor.ts`, `core/guards/**`, `pages/auth/login.component.ts`, `pages/auth/register.component.ts`, `app.routes.ts`, `app.config.ts`, topbar |

## Mandatory Security Principles

- Passwords stored only as hashes (PasswordHasher / bcrypt) — never plain text.
- JWT Secret Key: user-secrets in development, env var in production — not in the repo.
- Generic login error message (don't reveal whether the email exists).
- Authorization enforced **on the server** (claims), not just hidden UI on the client.
- Limited token lifetime; 401 handled by the interceptor.
- Rate limiting on login (brute-force protection); Swagger and `/hangfire` not exposed in production.
- JWT in localStorage is acceptable **only with** strict no-`[innerHTML]` discipline for user content; student-submitted source code must always be rendered as escaped text (Angular interpolation), never as HTML.
- Any API key that ever appeared in git history is compromised — rotate it at the provider (OpenAI / RapidAPI); removing it from the file does not remove it from history. (Manual action, not code.)

## Out of Scope (explicitly deferred)

- **Student-area screens ("My Journey")** — my lessons, submit-code form, AI feedback view — separate follow-up task; this task delivers only the login + a minimal student landing page.
- **Microsoft / Google SSO** — can be added later on top of the same `User` table (`ExternalProvider` + `ExternalId` columns).
- **Refresh tokens / "remember me"** — for now, token expiry means logging in again.
- **Forgot password / password reset by email** — no email-sending infrastructure yet; the teacher can set a new password for a student manually (future feature).
- **Forced password change on first login** — decided against: the password the teacher sets is permanent.
- **CSV/Excel data import** — Phase 5, follow-up task.
- **`Lesson.TeacherUserId` link** — replacing the free-text `TeacherName`; separate future task.
