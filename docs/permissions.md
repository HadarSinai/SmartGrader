# Permissions

> SmartGrader · Version 1.0 · Last updated 2026-08-26 · Status: as-built

| Version | Date | Change |
|---|---|---|
| 1.0 | 2026-08-26 | First edition. Reflects the B1 fix that moved `finish-year` to Admin. |

**What this document answers:** may this person do this, to whose data.

It is separate from [domain-model.md](domain-model.md) because it answers *"may she?"* rather than
*"what is it?"*, and it changes on a different trigger — a new endpoint changes this file and not that
one.

---

## The three mechanisms

Every authorization decision in this system is one of exactly three things. **Naming them is the
point:** a fourth ad-hoc mechanism is how a system stops being explainable.

| # | Mechanism | Question it answers | Where it lives |
|---|---|---|---|
| 1 | **Endpoint gate** | may this *role* call this route at all? | `[Authorize(Roles = …)]` on the controller or action |
| 2 | **Row scope** | which *rows* may this caller see? | `LessonAccess`, `StudentScope`, `OwnerScopeTeacherId` |
| 3 | **Field redaction** | which *fields* of a row may this caller see? | `TestVisibility`, `SubmissionLock` |

A rule enforced only in the Angular client is **not** a mechanism. The payload already reached the
browser and is readable in full in DevTools. Client-side hiding is presentation; the three above are
control.

### 1 — Endpoint gate

Roles come from the token. The class-level attribute applies unless the action carries its own, and
`[AllowAnonymous]` on an action wins over everything.

Three endpoints are anonymous — `login`, `forgot-password`, `reset-password` — because whoever needs
them is by definition someone who cannot authenticate. **Every other endpoint returns 401 to an
anonymous caller**, which is why the tables below have no "anonymous" column: it would read the same
in sixty rows out of sixty-three.

### 2 — Row scope

The scope is decided **at the controller boundary** and threaded into the query. No handler inspects
`User`.

| Property | Value | Consumed by |
|---|---|---|
| `OwnerScopeTeacherId` | `null` for an admin, otherwise the caller's user id | every teacher-owned query |
| `TeacherIdForSharedRead` | `null` for a **student** as well as for an admin | endpoints a student shares with a teacher |
| `StudentIdClaim` | the `studentId` claim, or `null` | a student's own scoping |

⚠️ **`TeacherIdForSharedRead is null` does not identify a student.** It is also null for an admin. The
distinguishing signal is `StudentIdClaim`. `LessonAccess.GetAccessibleOrThrowAsync` exists precisely
because of this: for a student the teacher filter does not run, so without a student id the check would
not run at all and every lesson in the school would be readable by anyone logged in.

**A student's id is read from the token claim, never from the route or the body.** `IsAllowedForStudent`
compares the claim to the route id and rejects a mismatch.

**Ownership is inherited, not stored per row.** An assignment has no `TeacherId`; it belongs to whoever
owns its lesson. A student has no `TeacherId` either — the teacher's set of students is derived through
her lessons' classes by `StudentScope`.

### 3 — Field redaction

Two redactions, both applied **after** mapping, at the end of the handler, immediately before the DTO
leaves it:

- **`TestVisibility`** — a student receives only sample test cases, and an empty reference solution.
  For a hidden test's *result*, `Input`, `Expected`, `Actual` and `Error` are all blanked and
  `IsHidden` is set. `Actual` and `Error` are blanked too, and not only the inputs: the student
  controls the code that runs against a hidden input, so printing it to stdout or stderr would hand it
  back to her through those fields.
- **`SubmissionLock`** — a finalised lesson or an archived class turns `CanResubmit` off and fills
  `LockReason`. It cannot be computed during mapping because it needs a `LessonResult` query and
  AutoMapper is not asynchronous.

**The lock overrides a teacher's grant.** `Submission.GrantExtraAttempt` beats the retry threshold; it
does not beat a lock. That is why the DTO carries a separate `LockReason` rather than only turning the
flag off — the client must be able to tell the two blocks apart, because a teacher can act on one and
not the other.

### The 404-not-403 rule

**"Missing" and "not yours" return the same 404, deliberately.** A 403 would confirm that the id exists,
which makes ids probeable: a teacher could walk `/api/lessons/1..500` and learn exactly how many lessons
the school has and which ids are taken.

Role mismatch is a different question and does return 403 — that leaks nothing, because the role
boundary is public knowledge.

### The creation chain

**Admin → Teachers → Students.** There is no self-registration: no `register` route in the client and
no endpoint on the server. A student's login is created for her by a teacher or an admin.

---

## The matrix

`Method`, `Route` and `Roles` are machine-checked against `server/Api/Controllers/*.cs`. The five
judgement columns are not — they record what the row scope and redaction mechanisms produce, which no
attribute states.

**Legend:** ✅ full access · 🔒 access with fields redacted · 404 exists but is invisible ·
403 wrong role · ❌ not applicable.
**Roles:** `(any)` = any authenticated user · `(anonymous)` = no token required.

<!-- gen:endpoints -->

### Auth — `api/auth`

| Method | Route | Roles | Admin | Owning teacher | Other teacher | Her own student | Other student |
|---|---|---|---|---|---|---|---|
| POST | `api/auth/login` | (anonymous) | ✅ | ✅ | ✅ | ✅ | ✅ |
| POST | `api/auth/forgot-password` | (anonymous) | ✅ | ✅ | ✅ | ✅ | ✅ |
| POST | `api/auth/reset-password` | (anonymous) | ✅ | ✅ | ✅ | ✅ | ✅ |
| POST | `api/auth/students` | Teacher,Admin | ✅ | ✅ | ✅ | 403 | 403 |
| POST | `api/auth/students/{studentId:int}/account` | Teacher,Admin | ✅ | ✅ | ✅ | 403 | 403 |
| GET | `api/auth/me` | (any) | ✅ | ✅ | ✅ | ✅ | ✅ |
| GET | `api/auth/me/profile` | (any) | ✅ | ✅ | ✅ | ✅ | ✅ |
| PUT | `api/auth/me` | Teacher,Admin | ✅ | ✅ | ✅ | 403 | 403 |
| POST | `api/auth/me/password` | (any) | ✅ | ✅ | ✅ | ✅ | ✅ |

`me` and `me/password` are self-scoped by construction — the id comes from the token, so there is no
"other person's" case. `PUT api/auth/me` is teacher-and-admin only because a student may not change her
own name or email; changing her password is the one account action she has.

### Classes — `api/classes`

| Method | Route | Roles | Admin | Owning teacher | Other teacher | Her own student | Other student |
|---|---|---|---|---|---|---|---|
| GET | `api/classes` | Teacher,Admin | ✅ | ✅ | ✅ | 403 | 403 |
| GET | `api/classes/{id:int}` | Teacher,Admin | ✅ | ✅ | ✅ | 403 | 403 |
| POST | `api/classes` | Teacher,Admin | ✅ | ✅ | ✅ | 403 | 403 |
| PUT | `api/classes/{id:int}` | Teacher,Admin | ✅ | ✅ | ✅ | 403 | 403 |
| DELETE | `api/classes/{id:int}` | Teacher,Admin | ✅ | ✅ | ✅ | 403 | 403 |
| POST | `api/classes/finish-year` | Admin | ✅ | 403 | 403 | 403 | 403 |

⚠️ **"Owning teacher" and "other teacher" are identical for every class row, and that is not an
oversight.** `SchoolClass` has no owner column, so there is no ownership to enforce — every teacher may
rename or delete every class. See *Known Modeling Gaps* in [domain-model.md](domain-model.md).

`finish-year` archives **every active class in the school** in one `ExecuteUpdate`, with no undo, and
locks every student's submissions with it. That is why it is the one class endpoint restricted to an
admin.

### Courses — `api/courses`

| Method | Route | Roles | Admin | Owning teacher | Other teacher | Her own student | Other student |
|---|---|---|---|---|---|---|---|
| GET | `api/courses` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |
| GET | `api/courses/{id:int}` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |
| POST | `api/courses` | Teacher,Admin | ✅ | ✅ | ✅ | 403 | 403 |
| PUT | `api/courses/{id:int}` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |
| DELETE | `api/courses/{id:int}` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |

### Lessons and assignments — `api/lessons`

| Method | Route | Roles | Admin | Owning teacher | Other teacher | Her own student | Other student |
|---|---|---|---|---|---|---|---|
| GET | `api/lessons` | (any) | ✅ | ✅ | 404 | ✅ | ✅ |
| GET | `api/lessons/{id:int}` | (any) | ✅ | ✅ | 404 | ✅ | 404 |
| POST | `api/lessons` | Teacher,Admin | ✅ | ✅ | ✅ | 403 | 403 |
| PUT | `api/lessons/{id:int}` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |
| DELETE | `api/lessons/{id:int}` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |
| GET | `api/lessons/{lessonId:int}/assignments` | (any) | ✅ | ✅ | 404 | 🔒 | 404 |
| GET | `api/lessons/{lessonId:int}/assignments/{assignmentId:int}` | (any) | ✅ | ✅ | 404 | 🔒 | 404 |
| POST | `api/lessons/{lessonId:int}/assignments` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |
| PUT | `api/lessons/{lessonId:int}/assignments/{assignmentId:int}` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |
| POST | `api/lessons/{lessonId:int}/assignments/verify-tests` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |
| POST | `api/lessons/{lessonId:int}/assignments/suggest-tests` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |
| DELETE | `api/lessons/{lessonId:int}/assignments/{assignmentId:int}` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |

**"Her own student"** means a student in a class the lesson was assigned to. A student outside that
class gets 404 for the lesson and everything under it.

🔒 on the two assignment reads is `TestVisibility`: hidden test cases are removed and the reference
solution is emptied. **This is the single most important redaction in the system** — the reference
solution is the complete answer to the exercise.

### Lesson results — `api/lesson-results`

| Method | Route | Roles | Admin | Owning teacher | Other teacher | Her own student | Other student |
|---|---|---|---|---|---|---|---|
| GET | `api/lesson-results/{studentId:int}/{lessonId:int}` | (any) | ✅ | ✅ | 404 | ✅ | 404 |
| GET | `api/lesson-results/lesson/{lessonId:int}/export` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |
| GET | `api/lesson-results/{studentId:int}/{lessonId:int}/suggestion` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |
| POST | `api/lesson-results/{studentId:int}/{lessonId:int}/reopen` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |
| POST | `api/lesson-results/complete` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |
| GET | `api/lesson-results/export-report` | Teacher,Admin | ✅ | ✅ | ✅ | 403 | 403 |
| GET | `api/lesson-results/student/{studentId:int}/summary` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |

`export-report` is scoped rather than owned — each teacher's export contains only her own students, via
`StudentScope`. A teacher with no lessons exports an empty file, **not the whole school**.

### Students and submissions — `api/students`

| Method | Route | Roles | Admin | Owning teacher | Other teacher | Her own student | Other student |
|---|---|---|---|---|---|---|---|
| GET | `api/students` | Teacher,Admin | ✅ | ✅ | ✅ scoped | 403 | 403 |
| GET | `api/students/export` | Teacher,Admin | ✅ | ✅ | ✅ scoped | 403 | 403 |
| POST | `api/students/import` | Teacher,Admin | ✅ | ✅ | ✅ | 403 | 403 |
| GET | `api/students/{id:int}` | (any) | ✅ | ✅ | 404 | ✅ | 404 |
| POST | `api/students` | Teacher,Admin | ✅ | ✅ | ✅ | 403 | 403 |
| PUT | `api/students/{id:int}` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |
| DELETE | `api/students/{id:int}` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |
| GET | `api/students/{studentId:int}/submissions` | (any) | ✅ | ✅ | 404 | 🔒 | 404 |
| GET | `api/students/{studentId:int}/submissions/{submissionId:int}` | (any) | ✅ | ✅ | 404 | 🔒 | 404 |
| POST | `api/students/{studentId:int}/submissions` | (any) | ✅ | ✅ | 404 | ✅ | 404 |
| PUT | `api/students/{studentId:int}/submissions/{submissionId:int}` | (any) | ✅ | ✅ | 404 | ✅ | 404 |
| POST | `api/students/{studentId:int}/submissions/{submissionId:int}/extra-attempt` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |
| PUT | `api/students/{studentId:int}/submissions/{submissionId:int}/score` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |
| DELETE | `api/students/{studentId:int}/submissions/{submissionId:int}` | Teacher,Admin | ✅ | ✅ | 404 | 403 | 403 |

"✅ scoped" means the endpoint succeeds but returns only the students reachable through that teacher's
own lessons. The distinction matters: `GET api/students` is not owned by anyone, it is **filtered per
caller**, and the filter is `StudentScope` — the same definition the period report uses, not a second
one.

🔒 on the two submission reads is `TestVisibility.RedactTestResults` **plus** `SubmissionLock`.

### Notifications — `api/notifications`

| Method | Route | Roles | Admin | Owning teacher | Other teacher | Her own student | Other student |
|---|---|---|---|---|---|---|---|
| GET | `api/notifications/graded-submissions` | Teacher,Admin,Student | ✅ | ✅ | ✅ scoped | ✅ own | ✅ own |
| GET | `api/notifications/class-signals` | Teacher,Admin | ✅ | ✅ | ✅ scoped | 403 | 403 |

The same route serves two different feeds by role: a teacher sees her own students' recently graded
submissions, a student sees her own.

### Logs — `api/logs`

| Method | Route | Roles | Admin | Owning teacher | Other teacher | Her own student | Other student |
|---|---|---|---|---|---|---|---|
| GET | `api/logs` | Admin | ✅ | 403 | 403 | 403 | 403 |
| DELETE | `api/logs/old` | Admin | ✅ | 403 | 403 | 403 | 403 |

### Teachers — `api/teachers`

| Method | Route | Roles | Admin | Owning teacher | Other teacher | Her own student | Other student |
|---|---|---|---|---|---|---|---|
| GET | `api/teachers` | Admin | ✅ | 403 | 403 | 403 | 403 |
| GET | `api/teachers/{id:int}` | Admin | ✅ | 403 | 403 | 403 | 403 |
| POST | `api/teachers` | Admin | ✅ | 403 | 403 | 403 | 403 |
| PUT | `api/teachers/{id:int}` | Admin | ✅ | 403 | 403 | 403 | 403 |
| POST | `api/teachers/{id:int}/password` | Admin | ✅ | 403 | 403 | 403 | 403 |
| DELETE | `api/teachers/{id:int}` | Admin | ✅ | 403 | 403 | 403 | 403 |

<!-- /gen -->

---

## Client routes

The Angular guards mirror the endpoint gate. They are **not** a control — they decide what is drawn,
not what is served — but a route whose guard disagrees with its endpoints is a defect either way, so
they are checked too.

The first column is the `path` literal exactly as written in `app.routes.ts`, **in file order** — that
is what the test compares, so a route added, removed or moved shows up here. The full route is the
human column; it is what nesting produces.

<!-- gen:routes -->

| `path` | Full route | Guard | Shell |
|---|---|---|---|
| `login` | `/login` | — | none |
| `forgot-password` | `/forgot-password` | — | none |
| `reset-password` | `/reset-password` | — | none |
| `` | `/` — the shell itself | `authGuard` | app |
| `` | `/` — the dashboard | `teacherGuard` | app |
| `lessons` | `/lessons` | `teacherGuard` | app |
| `students` | `/students` | `teacherGuard` | app |
| `classes` | `/classes` | `teacherGuard` | app |
| `classes/new` | `/classes/new` | `teacherGuard` | app |
| `classes/:id/edit` | `/classes/:id/edit` | `teacherGuard` | app |
| `courses` | `/courses` | `teacherGuard` | app |
| `courses/new` | `/courses/new` | `teacherGuard` | app |
| `courses/:id/edit` | `/courses/:id/edit` | `teacherGuard` | app |
| `students/new` | `/students/new` | `teacherGuard` | app |
| `students/:id/edit` | `/students/:id/edit` | `teacherGuard` | app |
| `lessons/new` | `/lessons/new` | `teacherGuard` | app |
| `lessons/:id/edit` | `/lessons/:id/edit` | `teacherGuard` | app |
| `lessons/:lessonId/assignments` | `/lessons/:lessonId/assignments` | `teacherGuard` | app |
| `lessons/:lessonId/assignments/new` | `/lessons/:lessonId/assignments/new` | `teacherGuard` | app |
| `lessons/:lessonId/assignments/:assignmentId/edit` | same | `teacherGuard` | app |
| `lessons/:lessonId/results` | `/lessons/:lessonId/results` | `teacherGuard` | app |
| `students/:studentId/submissions` | same | `teacherGuard` | app |
| `students/:studentId/submissions/:submissionId` | same | `teacherGuard` | app |
| `students/:studentId/submissions/:submissionId/edit` | same | `teacherGuard` | app |
| `logs` | `/logs` | `adminGuard` | app |
| `teachers` | `/teachers` | `adminGuard` | app |
| `teachers/new` | `/teachers/new` | `adminGuard` | app |
| `teachers/:id/edit` | `/teachers/:id/edit` | `adminGuard` | app |
| `profile` | `/profile` | `teacherGuard` | app |
| `my` | `/my` — the shell itself | `studentGuard` | student |
| `` | `/my` → redirects to `lessons` | inherited | student |
| `lessons` | `/my/lessons` | inherited | student |
| `lessons/:lessonId/assignments` | `/my/lessons/:lessonId/assignments` | inherited | student |
| `lessons/:lessonId/assignments/:assignmentId/submit` | `/my/…/submit` | inherited | student |
| `submissions/:submissionId` | `/my/submissions/:submissionId` | inherited | student |
| `submissions/:submissionId/edit` | `/my/submissions/:submissionId/edit` | inherited | student |
| `grades` | `/my/grades` | inherited | student |
| `profile` | `/my/profile` | inherited | student |

<!-- /gen -->

"inherited" means the child carries no guard of its own — `studentGuard` on the `my` parent covers it.
The teacher-area children each repeat `teacherGuard` even though `authGuard` already sits on the
parent, because `authGuard` alone would let a student through.

`profile` and `my/profile` are the **same component** in two shells. A student sees only the password
section; the name and email fields are hidden from her, which matches `PUT api/auth/me` being
teacher-and-admin only.

**`assignments` and `submissions` no longer appear here.** They were `redirectTo` stubs the topbar
linked to, so "תרגילים" landed on Lessons and highlighted the wrong item; Plan B's B4 removed the
items and then the routes. Neither resource is reachable at the top level, which is correct — every
route to one is nested under the lesson or the student that owns it, and that nesting is where the
guard applies.

---

## Explicitly not supported

- **No self-registration.** No `register` route, no endpoint.
- **No per-class ownership.** Any teacher may edit or delete any class, and nothing records who did.
- **No role above Admin.** An admin who forgets her password has nobody to reset it; recovery runs
  through `forgot-password` and therefore requires her row to have an email.
- **No field-level permission on lesson results.** A student who may read a result reads all of it.
- **No audit trail on reads.** The log records grading-pipeline events and unhandled errors, not who
  looked at what.
