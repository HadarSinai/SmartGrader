# Area: Student — "המסע שלי"

> SmartGrader · Version 1.0 · Last updated 2026-08-27 · Status: as-built

| Version | Date | Change |
|---|---|---|
| 1.0 | 2026-08-27 | First edition. |

## Purpose

The only part of the system a student ever sees. It answers three questions and deliberately nothing
else: **what am I supposed to do**, **what happened to what I submitted**, and **what is my grade**.

Without it a student's only channel to her own work is her teacher's screen read aloud. With it she can
see a failure, understand it, and fix it herself — which is the entire pedagogical point of automated
grading.

## Who Uses This

A student, 15–18, in one class. She logs in with a username and password her teacher created; she has
no email and cannot recover her own password. She uses the system a few times a week, usually right
after submitting, and usually to find out whether her code passed.

## Screens & Routes

All under the `my` parent, which carries `studentGuard` and the student shell. Children inherit the
guard.

<!-- gen:arearoutes student -->

| `path` | Full route | Screen |
|---|---|---|
| `my` | `/my` | the student shell |
| `` | `/my` → redirects to `lessons` | — |
| `lessons` | `/my/lessons` | My lessons |
| `lessons/:lessonId/assignments` | `/my/lessons/:lessonId/assignments` | The assignments in one lesson |
| `lessons/:lessonId/assignments/:assignmentId/submit` | `/my/…/submit` | Submit code |
| `submissions/:submissionId` | `/my/submissions/:submissionId` | My feedback |
| `submissions/:submissionId/edit` | `/my/submissions/:submissionId/edit` | Fix and resubmit — the same editor in edit mode |
| `grades` | `/my/grades` | My grades |
| `profile` | `/my/profile` | My account |

<!-- /gen -->

**`studentGuard` rejects a Student login that has no linked student record**, logs it out and shows an
error. A student without a `studentId` claim has no scope at all, and reading unscoped would be worse
than refusing.

`/my/profile` is the **same component** as the teacher's `/profile`, in the student shell. The name and
email fields are hidden from her; changing her password is the one account action she has, which
matches `PUT api/auth/me` being teacher-and-admin only.

## Functional Requirements

**S-1** The system shall take the student's identity **only** from the `studentId` claim in her token,
never from a route parameter or a request body.

**S-2** When a Student login has no linked student record, the system shall log the session out rather
than render any screen.

**S-3** The "my lessons" screen shall list every lesson assigned to the student's class, each with her
personal status and her final score for that lesson.

**S-4** When a lesson has no final result for the student, the system shall display «בתהליך» rather
than an error or a blank.

**S-5** The "my lessons" list shall page at 10 rows, shall show the paginator only when there are more
than 10, shall apply no default sort, and shall offer no search.

**S-6** The assignments screen shall show, per assignment in the lesson, the student's own submission
status with its Hebrew label, its semantic colour **and** its icon.

**S-7** A student shall see only sample test cases, and shall never receive the reference solution
(`B-46`, and the redaction in [permissions.md](../permissions.md)).

**S-8** For a hidden test case's result, the system shall show only whether it passed, blanking its
input, expected output, actual output and error.

**S-9** While a submission is `PendingAi` or `ProcessingAi`, the feedback screen shall refresh it every
5 seconds and shall stop when it reaches any other status.

**S-10** The feedback screen shall offer "fix and resubmit" **if and only if** the server says the
submission can be resubmitted — the client shall not compute that rule.

**S-11** When a submission is locked, the system shall show the lock sentence instead of the resubmit
action, and shall not present an action that would fail on click (`B-7`).

**S-12** The system shall not offer fix-and-resubmit when the failure was `JudgeUnavailable`, because
the fault is not the student's and there is nothing for her to change (`B-5`).

**S-13** A multi-file submission shall render every file with its name, not only the first.

**S-14** The "my grades" screen shall show her lessons with their final scores, and her submissions
with their per-assignment scores, and shall filter the second by the lesson selected in the first.

**S-15** A missing lesson result shall be tolerated: a 404 shall render as «בתהליך», not as an error
toast.

**S-16** Every list in this area shall show a Hebrew empty state rather than an empty table.

## Applicable Rules

| Rule | Why it reaches this area |
|---|---|
| `G-1` | a blocking requirement produces **no grade**, and the screen must not read as a low one |
| `G-20` | no graded assignment means no lesson score — «בתהליך», not 0 |
| `G-25` | only her latest attempt counts; earlier attempts are history |
| `B-2`, `B-3` | unlimited attempts, at least a minute apart |
| `B-4` | what makes a submission resubmittable |
| `B-5` | `JudgeUnavailable` offers her nothing to fix |
| `B-6` | a teacher's grant beats the retry threshold |
| `B-7`, `B-8` | the lock, and the empty-database premise its two conditions rest on |
| `B-21` | she cannot recover her own password — her teacher does it |
| `B-46` | hidden and core default in her disfavour, on purpose |
| `B-51` | her own submitted code is rendered as escaped text |
| `D-7`, `D-9` | a status change during polling must be announced, and never colour-only |

## Acceptance Criteria

**AC-1 (S-1)** Given a student is logged in, when she requests
`/api/students/{someone else}/submissions`, then the server returns 404.

**AC-2 (S-2)** Given a Student login with no linked student record, when she reaches `/my`, then she is
logged out and sees an error message.

**AC-3 (S-4)** Given a lesson with no `LessonResult` row for her, when "my lessons" loads, then that row
shows «בתהליך» and no error toast appears.

**AC-4 (S-7, S-8)** Given an assignment with three hidden test cases and one sample, when she opens her
feedback, then she sees one test's input and expected output, and three rows showing only pass or fail
with every other field empty.

**AC-5 (S-9)** Given a submission in `ProcessingAi`, when 5 seconds pass, then the screen re-requests
it; when it becomes `Done`, then the polling stops.

**AC-6 (S-11)** Given her teacher has finalised the lesson, when she opens a submission scored below
the retry threshold, then she sees «לא ניתן להגיש שוב — השיעור כבר סוכם או שהכיתה נמצאת בארכיון.» and
**no** resubmit button.

**AC-7 (S-12)** Given a submission that failed with `JudgeUnavailable`, when she opens it, then no
fix-and-resubmit action is offered.

**AC-8 (S-13)** Given a multi-file submission of three files, when she opens it, then all three render,
each under its file name.

**AC-9 (S-5)** Given 11 lessons, when "my lessons" loads, then 10 rows and a paginator are shown; given
9, then no paginator is shown.

## Screen Composition

Four questions per screen: **what comes off · what does the eye hit first · what is the reading order ·
how much per row.** Written before the code changes, so it is specification rather than taste.

### My lessons — `/my/lessons`

Six columns: מקצוע · נושא · תאריך · סטטוס · ציון סופי · actions.

| | |
|---|---|
| **Comes off** | **Nothing.** Every column answers "which lesson do I open next": what it was about, when, whether it is graded, and what I got. |
| **Eye hits first** | **The status column.** She is here to find what is not finished. |
| **Reading order** | Subject → date → status → score. Identity, then time, then state, then result. |
| **Per row** | Six columns is right for a decision this simple. |

**No change, and the reason is worth recording.** A5 flagged «ציון סופי» as a heading promising a
number that may not exist, on the reading that the cell falls back to «בתהליך». **Checking the code
before changing it, it does not** — the cell renders «—» and «בתהליך» lives in the «סטטוס» column
beside it, where it belongs (`S-4`). The heading and the column agree, so the copy stays.

### The assignments in one lesson — `/my/lessons/:lessonId/assignments`

Four columns: תרגיל · בונוס · סטטוס · ציון.

| | |
|---|---|
| **Comes off** | Nothing. Four columns. |
| **Eye hits first** | **Status.** Same reason as above — she is looking for what she has not done. |
| **Reading order** | Assignment → bonus → status → score. |
| **Per row** | Four is already minimal. |

The «בונוס» column earns its place: a bonus assignment is optional, and skipping it is never a penalty
(`G-26`) — it adds to her lesson score and never enters the average that her required work sets.
Without the marker she cannot tell an optional exercise from one she missed.

### My feedback — `/my/submissions/:submissionId`

The panel stacks four regions: score tiles · requirements table · the model's prose · test results.
See [shared-ui.md](shared-ui.md) for the panel itself; the **student's default state** is decided here.

| | |
|---|---|
| **Comes off** | Nothing is removed. The requirements table and the test results are **collapsed by default** and open on click. |
| **Eye hits first** | **The score, and the one sentence explaining it.** She arrived to understand one number. |
| **Reading order** | Score → what it means → *(optional)* which requirement failed → *(optional)* which test failed. |
| **Per row** | Test results: pass/fail plus the case name. Hidden cases show only the verdict (`S-8`). |

⚠️ **Collapsed, never removed.** When a blocking requirement fails there is **no grade at all**
(`G-1`), and the requirements table is the only thing on the screen that says *why*. Hiding it would
leave a student looking at "no grade" with no reason given — the exact failure this area exists to
prevent.

### My grades — `/my/grades`

**The two tables are already linked.** The lessons table is `selectionMode="single"`; clicking a lesson
filters the submissions table beneath it to that lesson, hides its now-redundant «שיעור» column, and
shows a hint naming the selection. That is the behaviour the screen was asked for, and it is what it
does.

| | |
|---|---|
| **Comes off** | Nothing. **The problem is not density, it is discoverability.** |
| **Eye hits first** | The lessons table — the final grades are what she came for. |
| **Reading order** | Final grades → pick one → its submissions. |
| **Per row** | Lessons 4 columns, submissions 5 when a lesson is selected and 6 when it is not. Correct. |

**The change is to make the link visible rather than to restructure.** Today a student learns the
tables are connected by happening to click a row. The selected row needs an unmistakable selected
state, the lower table needs a heading that names the selected lesson rather than a hint below it, and
there must be a visible way back to "all submissions". A relationship the user has to discover by
accident is a relationship most users never find.

### Submit code — `/my/…/submit`

| | |
|---|---|
| **Comes off** | Nothing yet — this screen is not crowded. |
| **Eye hits first** | The editor. |
| **Reading order** | What the exercise asks → sample cases → the editor → submit. |
| **Per row** | — |

The sample test cases must sit **above** the editor, not below it. They are the specification she is
coding against, and specification below the work is specification read second.

## Explicitly Not Supported

- **A student cannot recover her own password.** She has no email; her teacher resets it (`B-21`).
- **She cannot change her own name or email** — `PUT api/auth/me` is teacher-and-admin only.
- **She cannot see another student's anything**, including class averages or how many others failed the
  same test — class signals are teacher-only (`B-36`).
- **She cannot see a hidden test's input, expected output, or the reference solution**, ever, on any
  path — not after grading and not after the lesson is finalised.
- **She cannot see her attempt history.** Only the current attempt is shown; earlier ones exist in the
  database but no screen renders them.
- **She cannot resubmit after the lesson is finalised**, and her teacher cannot grant her one — that
  needs the lesson reopened (`B-7`).
- **There is no search and no sorting** on any list in this area.
- **There is no notification bell badge count persisted across devices** — read state is per browser.
