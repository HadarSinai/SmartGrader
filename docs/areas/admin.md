# Area: Admin

> SmartGrader · Version 1.0 · Last updated 2026-08-27 · Status: as-built

| Version | Date | Change |
|---|---|---|
| 1.0 | 2026-08-27 | First edition. |

## Purpose

The two things only an administrator does: **create the teacher accounts** that everything else hangs
off, and **read the system log** when something has gone quietly wrong.

Small in surface and large in consequence. This is the top of the account chain — there is nobody above
it — and it is the only place where an operational failure that produced no user-visible error can be
seen at all.

## Who Uses This

One person, rarely. She is not a developer. She opens the teachers screen at the start of a year and
the log only when someone reports that something did not arrive.

## Screens & Routes

<!-- gen:arearoutes admin -->

| `path` | Full route | Screen |
|---|---|---|
| `logs` | `/logs` | System log |
| `teachers` | `/teachers` | Teachers list |
| `teachers/new` | `/teachers/new` | Teacher form — create |
| `teachers/:id/edit` | `/teachers/:id/edit` | Teacher form — edit |

<!-- /gen -->

All four carry `adminGuard`, sit in the teacher shell, and are backed by controllers marked
`[Authorize(Roles = "Admin")]` at the class level. **Three layers agree**: the endpoint, the route
guard, and the topbar item, which renders only for an admin.

## Functional Requirements

**AD-1** Only an administrator shall reach any screen or endpoint in this area.

**AD-2** A teacher account shall be created only by an administrator; there shall be no
self-registration (`B-22`).

**AD-3** A teacher's email shall be required when her account is created or edited, even though the
column is nullable, because a teacher without one can never recover her password (`B-24`).

**AD-4** A teacher row created before the email column existed shall be shown with a «חסר מייל» marker
until an address is filled in.

**AD-5** An administrator shall be able to set a new password for a teacher directly.

**AD-6** The system shall refuse to delete a teacher who owns lessons or courses, and the refusal shall
count them (`B-47`).

**AD-7** The system log shall page at 25 rows with options 25/50/100, shall sort by timestamp, shall
match the search box against the **message text**, and shall additionally filter by action type and by
status.

**AD-8** Each log row shall display the context ids it carries — user, lesson and assignment — so a
failure can be traced to the record it concerns.

**AD-9** The system shall write a log row for grading-pipeline events and for unhandled errors, and
shall write a row for a failed password-reset email and a failed teacher digest.

**AD-10** The system shall permit deleting log rows older than a given age.

**AD-11** Ending the school year shall be reachable only by an administrator (`TK-3` in
[teacher-classroom.md](teacher-classroom.md)).

## Applicable Rules

| Rule | Why it reaches this area |
|---|---|
| `B-22` | the creation chain — admin → teachers → students |
| `B-24` | a teacher without an email cannot recover her password |
| `B-47`, `B-48` | the delete guard, and the four audit columns it does not cover |
| `B-16`, `B-35` | the two failures whose **only** trace is a log row |
| `B-50` | ⚠️ the admin's own recovery hole |

## Acceptance Criteria

**AC-1 (AD-1)** Given a teacher who is not an admin, when she requests `/api/logs`, then the server
returns 403; when she navigates to `/logs`, then the guard redirects her; and the topbar shows no
«לוגים» item.

**AC-2 (AD-3)** Given a new teacher with no email, when the admin saves, then the save is refused.

**AC-3 (AD-6)** Given a teacher who owns two lessons, when the admin deletes her, then the request is
refused with a message naming the count.

**AC-4 (AD-8)** Given a failed password-reset email logged with a user id, when the admin opens the
log, then that row shows the user id in its context column.

**AC-5 (AD-7)** Given 200 log rows, when the screen loads, then 25 are shown with a 25/50/100 selector;
when she filters by status «Error», then only error rows remain.

## Screen Composition

*Filled in phase A5.* No problem is suspected upfront for either screen — both are narrow, and the log
screen's density is appropriate to its job.

## Explicitly Not Supported

- **There is no role above Admin.** An admin who forgets her password recovers through
  `forgot-password` like anyone else, which means her row must carry an email.
- ⚠️ **`B-50`: an admin row created before the email column existed holds none and can never recover.**
  The seeder writes the address only at creation and never overwrites — correctly, because overwriting
  would silently revert a changed value on every restart. The live database has no such row; **a fresh
  environment or a restore from an older backup reproduces it silently**, and it surfaces on the worst
  possible day. A startup warning was specified in Plan B's B1 and is **not built**.
- **The log records what the system did, not what people read.** There is no audit trail of views, and
  a delete of a teacher leaves her id behind in rows that no longer resolve (`B-48`).
- **There is no admin-visible list of students across the school** except through the students screen,
  where an admin's scope is simply unfiltered.
- **The log cannot be exported.**
- **Log retention is a manual action.** Old rows are deleted when someone asks for it, not on a
  schedule.
