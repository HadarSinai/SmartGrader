---
description: "Build the system Logs feature: Admin role, log writing (AI pipeline + unhandled errors), error emails, retention cleanup, and an admin-only logs screen"
---

# Build the Logs Feature (SmartGrader)

The `Logs` table already exists in the DB (entity `server/Domain/Entities/Log.cs`, `DbSet<Log> Logs` in `GradeSheetContext`, created in the Init migration) but nothing reads or writes it. Wire it up end-to-end per the phases below. Follow `.github/instructions/server.instructions.md` and `client.instructions.md`, and use the existing skills: backend-repository-query-pattern, backend-mediatr-query-handler-pattern, backend-controller-endpoint-pattern, backend-automapper-profile-pattern, client-list-table-pattern.

## Phase 0 — Admin role

1. Add `Admin` to the `UserRole` enum in `server/Domain/Entities/User.cs`. No migration needed (Role uses `HasConversion<string>()`).
2. Seed an admin user at startup from an `AdminUser` appsettings section (`Email`, `Password`, `FullName`): create it if it doesn't exist, hashing the password with the same hasher used in `RegisterTeacherHandler`.
3. Change every `[Authorize(Roles = "Teacher")]` to `[Authorize(Roles = "Teacher,Admin")]` — 18 occurrences across `AuthController`, `LessonResultController`, `LessonsController`, `StudentsController` — so Admin can do everything a teacher can.
4. Client: add an `isAdmin` computed signal in `client/src/app/services/auth.service.ts` (mirroring `isTeacher`), add an `adminGuard` in `client/src/app/core/guards/auth.guards.ts`, and make `teacherGuard` accept Admin too.

## Phase 1 — Logs infrastructure

5. Fix the `Log` entity: it has a private ctor and private setters, so it cannot be instantiated. Add a static factory `Log.Create(actionType, message, status, systemSource, userId?, lessonId?, assignmentId?)` (keep the private ctor for EF). Add string constants: ActionType = AiGradingStarted / AiGradingCompleted / CompilationFailed / AiFailed / UnhandledError; Status = Success / Error; SystemSource = AiWorker / Api.
6. Create `ILogRepository` in `server/Domain/Abstractions` (`AddAsync`, `GetAllAsync` ordered by Timestamp desc with AsNoTracking, `DeleteOlderThanAsync(DateTime cutoff, ct)` using ExecuteDeleteAsync) modeled after `ILessonRepository`, implement in `server/Infrastructure/Repositories/LogRepository.cs`, register scoped in `server/Infrastructure/DependencyInjection.cs`.
7. Create an `ILogWriter` application service: writes a log + SaveChanges wrapped in try/catch — a failed log write must NEVER fail or delay the main operation (log the failure to `ILogger` only). When the written log has Status=Error, it also triggers the error email (fire-and-forget, best-effort).

## Phase 2 — Email infrastructure (none exists today)

8. Add an `IEmailSender` abstraction (respect Clean Architecture layering) with an SMTP implementation in `server/Infrastructure/Services`, configured via an `Smtp` appsettings section (Host, Port, User, Password, From). Recipient = `AdminUser:Email`. If SMTP is not configured, it must no-op with an `ILogger` warning — the app must run fine without it. Register in Infrastructure DI.
9. Error email: Hebrew subject "SmartGrader – שגיאה במערכת", body containing ActionType, Message, Timestamp.

## Phase 3 — Write logs

10. `server/Api/BackgroundServices/AiWorker.cs`: within the existing per-submission DI scope, write logs (via `ILogWriter`) at: grading started, grading completed (include score), compilation failed, AI failed.
11. `server/Api/Middlewares/GlobalExceptionMiddleware.cs`: only in the generic 500 branch (not 400/404/409), write an UnhandledError log (UserId from claims when available) resolving `ILogWriter` from `context.RequestServices`; wrap in try/catch.

## Phase 4 — Read + delete API

12. `LogResponseDto` + `LogProfile` (AutoMapper) + `GetLogsQuery`/Handler under `server/Application/UseCases/Logs` returning the latest 500 logs desc.
13. `DeleteOldLogsCommand`/Handler taking `olderThanDays`.
14. `LogsController` with `[Authorize(Roles = "Admin")]` (Admin ONLY — teachers must get 403): `GET /api/logs`, `DELETE /api/logs/old?days=30`.

## Phase 5 — Automatic retention

15. Hangfire is already configured (in-memory). Register a recurring daily job at startup that deletes logs older than `Logs:RetentionDays` (appsettings, default 30).

## Phase 6 — Client screen

16. `client/src/app/models/log.model.ts` + `client/src/app/services/logs.service.ts` (`getAll`, `deleteOld`) following the existing service pattern.
17. `client/src/app/pages/logs/logs-list.component.ts` — "יומן מערכת": read-only table (NO row actions/multi-select): date+time (unified format), actionType as tag, status with semantic colors, source, message; filters by actionType/status; PrimeNG client-side paginator; RTL; empty state. Toolbar button "מחיקת לוגים ישנים" → `ConfirmationService.confirm()` → deleteOld(30) → Hebrew toast + reload. All copy in Hebrew, gender-neutral.
18. Route `path: 'logs'` under `AppLayoutComponent` with `[authGuard, adminGuard]`; nav item "יומן מערכת" (pi-history icon) in `client/src/app/components/layout/topbar.component.ts`, visible only when `auth.isAdmin()`.

## Verification

- `dotnet build` on `server/SmartGrader.sln`; run the API; confirm the admin user is seeded and can log in and use all teacher endpoints.
- Create a submission → AI pipeline log rows appear via `GET /api/logs` with an admin token; a teacher token gets 403.
- Force a 500 → UnhandledError log row + email attempt (with SMTP unset: warning only, no crash).
- `DELETE /api/logs/old?days=0` removes rows; the recurring job appears in the Hangfire dashboard (dev).
- Client build passes; admin sees the nav item and screen incl. the delete-old flow; a teacher doesn't see the nav item and `/logs` redirects.

## Out of scope

- No CRUD/login event logging, no daily digest email, no server-side paging (cap at latest 500), no manual email sending from the screen.
