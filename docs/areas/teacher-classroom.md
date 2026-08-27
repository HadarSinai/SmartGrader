# Area: Teacher — Classroom

> SmartGrader · Version 1.0 · Last updated 2026-08-27 · Status: as-built

| Version | Date | Change |
|---|---|---|
| 1.0 | 2026-08-27 | First edition. |

## Purpose

Everything that happens **after** students start working: the classes and students themselves, the
submissions that arrive, the grade each lesson ends with, and the dashboard that says what needs
attention today.

If [teacher-content.md](teacher-content.md) is where a teacher writes the exercise, this is where she
finds out whether it worked.

## Who Uses This

The same teacher, in a different mode — reacting rather than authoring, often between lessons and often
on the question "who is stuck". Also an admin, who sees everything unfiltered.

## Screens & Routes

<!-- gen:arearoutes teacher-classroom -->

| `path` | Full route | Screen |
|---|---|---|
| `` | `/` | Dashboard |
| `students` | `/students` | Students list |
| `classes` | `/classes` | Classes list |
| `classes/new` | `/classes/new` | Class form — create |
| `classes/:id/edit` | `/classes/:id/edit` | Class form — edit |
| `students/new` | `/students/new` | Student form — create |
| `students/:id/edit` | `/students/:id/edit` | Student form — edit |
| `lessons/:lessonId/results` | `/lessons/:lessonId/results` | Lesson results — the finalisation screen |
| `students/:studentId/lessons/:lessonId/result` | `/students/…/result` | ⚠️ orphaned — linked from nowhere |
| `students/:studentId/submissions` | `/students/:studentId/submissions` | One student's submissions |
| `students/:studentId/submissions/:submissionId` | `/students/…/:submissionId` | Submission detail |
| `students/:studentId/submissions/:submissionId/edit` | `/students/…/edit` | Submission edit |
| `submissions` | `/submissions` → redirects to `students` | ⚠️ a stub, and the topbar links to it |

<!-- /gen -->

⚠️ **Two defects are visible in this table, and hiding them would make it describe a system that does
not exist.**

`students/:studentId/lessons/:lessonId/result` is **linked from nowhere** — no screen navigates to it —
and its copy is written in the first person («התוצאה שלי») on a teacher-only route. Deletion is Plan
B's B4.

`/submissions` is a `redirectTo` stub that the topbar links to, so «הגשות» lands on Students and
highlights the wrong item. Also B4.

## Functional Requirements

**TK-1** A teacher shall see only the students reachable through her own lessons' classes; an admin
shall see all. A teacher with no lessons shall see an **empty list, not the whole school**.

**TK-2** A class shall have no owner: every teacher shall be able to see, create, edit and delete every
class.

**TK-3** Ending the school year shall archive every active class in one action, and shall be available
to an **admin only**.

**TK-4** An archived class shall lock every submission of every student in it (`B-7`).

**TK-5** The students list shall page at 10 rows with options 10/25/50, shall match the search box
against **full name**, and shall additionally filter by class.

**TK-6** The classes list shall page at 10 rows with options 10/25/50, shall sort by name or academic
year, and shall offer an archived/active filter.

**TK-7** The submissions list shall match the search box against **assignment name**, and shall
additionally filter by status.

**TK-8** Search text and filters shall be held in component state only, so navigating away and back
shall clear them. Where filters are active, the screen shall offer a single "clear filters" action.

**TK-9** The submissions list and detail shall show each submission's status with its Hebrew label,
semantic colour **and** icon.

**TK-10** While a submission is `PendingAi` or `ProcessingAi`, the detail screen shall refresh it every
5 seconds and shall stop at any other status.

**TK-11** A teacher shall be able to grant one extra attempt, with a written reason, overriding the
retry threshold but never a lock (`B-6`, `B-7`).

**TK-12** A teacher shall be able to override a submission's score, with a written reason, within the
assignment's ceiling (`G-23`).

**TK-13** The lesson results screen shall show the system's computed score as a suggestion, and shall
require a written reason for any final score that differs from it by more than 0.05 (`G-22`, `G-24`).

**TK-14** The system shall refuse to finalise a lesson while a submission in it is still being graded,
and shall permit finalising when a submission failed with `AiFailed`, so a manual grade is possible
when automatic grading did not work.

**TK-15** A finalised lesson shall be reopenable, which shall also release that student's submissions
in that lesson.

**TK-16** Student data shall be exportable to Excel, and students shall be importable from Excel with
per-row errors that never roll back the successful rows (`B-49`).

**TK-17** The dashboard shall show KPI cards and the five most recently graded submissions, scoped to
the calling teacher.

**TK-18** Deleting a class, student or submission shall be confirmed through a dialog that names the
subject and states that it cannot be undone; a submission that is being processed or is already graded
shall not be deletable.

## Applicable Rules

| Rule | Why it reaches this area |
|---|---|
| `G-18` … `G-25` | everything the lesson-results screen computes, suggests and refuses |
| `B-1` … `B-10` | one row per student per assignment, attempts, rate limit, locks |
| `B-25` … `B-36` | the notification bell and the daily digest, both fed from this area's data |
| `B-47`, `B-48` | the deletion guards, and the audit columns a delete orphans |
| `B-49` | Excel import is partial-success |
| `D-9` | status is never colour alone — and see the defect below |

## Acceptance Criteria

**AC-1 (TK-1)** Given a teacher with no lessons, when she opens `/students`, then the list is empty and
no error appears.

**AC-2 (TK-3)** Given a non-admin teacher, when she calls `POST /api/classes/finish-year`, then the
server returns 403; given an admin, then it succeeds and every active class becomes archived.

**AC-3 (TK-3)** Given a teacher is logged in, when the classes screen loads, then the «סיום שנה» button
is not rendered; given an admin, then it is.

**AC-4 (TK-4)** Given a student whose class was just archived, when she opens a submission scored below
the retry threshold, then no resubmit action is offered and the lock sentence is shown.

**AC-5 (TK-13)** Given a lesson whose computed score for a student is 85, when the teacher submits 85,
then no reason is required; when she submits 87, then the save is refused until she writes one.

**AC-6 (TK-14)** Given a lesson with one submission still in `ProcessingAi`, when she finalises, then
the request is refused; given the only ungraded submission is `AiFailed`, then finalising succeeds.

**AC-7 (TK-15)** Given a finalised lesson, when she reopens it, then the final score remains visible
and that student's submissions in that lesson become resubmittable again.

**AC-8 (TK-16)** Given an import file where row 4 has an empty class, when she imports, then the other
rows are created and the response names row 4 with its reason.

**AC-9 (TK-17)** Given a teacher with graded submissions, when she opens `/`, then four populated KPI
cards and a table with rows are shown, and **no error toast appears**.

**AC-10 (TK-9)** Given a submission with status `JudgeUnavailable`, when the submissions list renders
it, then it appears in the warning colour with `pi-exclamation-circle` — the same as every other screen.

⚠️ **AC-10 fails today.** `submissions-list` derives severity by substring match, and
`"judgeunavailable"` matches none of the tested substrings, so it renders as a neutral information
chip. Full analysis in [design-system.md](../design-system.md); the fix is one shared mapping, in A6.

## Screen Composition

Four questions per screen: **what comes off · what does the eye hit first · what is the reading order ·
how much per row.**

### Dashboard — `/`

Four KPI cards and a table of the five most recently graded submissions.

| | |
|---|---|
| **Comes off** | Nothing yet — but **every card must be re-justified**, because all four were blank for weeks and nobody noticed. A card nobody misses when it breaks is a card nobody reads. |
| **Eye hits first** | **What needs attention today**, not a total. A count of everything ever graded is a vanity number; a count of what is waiting is a decision. |
| **Reading order** | What is stuck → what just finished → act. |
| **Per row** | Five recent submissions: student · assignment · score · when. Four columns. |

**The copy was corrected in Plan B's B1** and the correction must survive A6: the endpoint returns
recently **graded** submissions, not recent ones, so the card reads «הגשות שנבדקו לאחרונה». Leaving the
old wording would have swapped a card that showed nothing for a card that quietly showed something else.

### Students — `/students`

Six columns: [☑] · שם התלמיד/ה · כיתה · פעילות · צפייה · פעולות.

| | |
|---|---|
| **Comes off** | Nothing. |
| **Eye hits first** | **«פעילות»** — the column that says who is working and who has stopped. |
| **Reading order** | Name → class → activity → act. |
| **Per row** | Six, of which three are controls. Three real columns. |

«צפייה» stays a separate icon rather than folding into ⋯: looking at a student's submissions is the
most frequent action on this screen, and burying the common action behind a menu to tidy the row makes
the row tidier and the work slower.

### Classes — `/classes`

Five columns: שם · שנה · תלמידים · סטטוס · פעולות.

| | |
|---|---|
| **Comes off** | Nothing. |
| **Eye hits first** | **The status** — archived or active decides whether anything on that class can still change (`TK-4`). |
| **Reading order** | Name → year → size → state → act. |
| **Per row** | Five. Correct. |

**«סיום שנה» is the most destructive action in the system** — one click archives every active class and
locks every submission behind it, with no undo. It is admin-only since Plan B's B1 (`TK-3`). It must
not sit beside «כיתה חדשה» as a peer; it is not a routine action and must not be positioned like one.

### One student's submissions — `/students/:studentId/submissions`

Six columns: תרגיל · נשלח · סטטוס · ציון · צפייה · פעולות.

| | |
|---|---|
| **Comes off** | Nothing. |
| **Eye hits first** | **Status.** This screen exists to find what went wrong. |
| **Reading order** | Assignment → when → state → score → act. |
| **Per row** | Six, of which two are controls. |

⚠️ **The status chip on this screen is wrong today** for `JudgeUnavailable` — it renders as a neutral
information chip because severity is derived by substring match, while every other screen shows it
amber. A6 replaces all five copies of that mapping with the one table in
[design-system.md](../design-system.md).

### Lesson results — `/lessons/:lessonId/results`

Four columns: שם התלמיד/ה · ציון סופי · סטטוס · פעולות. **The screen where a grade becomes final.**

| | |
|---|---|
| **Comes off** | Nothing — it is already minimal. |
| **Eye hits first** | **The computed score the system suggests**, because accepting it is the common case and departing from it is the exception that must be argued for (`G-22`). |
| **Reading order** | Student → suggested score → state → finalise. |
| **Per row** | Four. Correct. |

**The finalisation dialog is where the real density is, and it must show three things at once:** the
computed score, the ceiling it must fall under (`G-21`), and the reason field — which appears **only**
when the entered score departs from the suggestion by more than 0.05. A reason field that is always
visible teaches teachers to fill it in reflexively, and then it stops meaning anything.

### Class form, student form — `/classes/new`, `/students/new`

| | |
|---|---|
| **Comes off** | Nothing. Both are three or four fields. |
| **Eye hits first** | The first field. |
| **Reading order** | Top to bottom, actions at the end of the row. |
| **Per row** | — |

Both are among the **14 files carrying hardcoded colours** that A6 converts to tokens.

## Explicitly Not Supported

- **A class has no owner.** Any teacher can rename or delete any class, and **nothing records who did
  it.** This is the only resource outside the ownership model, and it is accepted rather than
  overlooked — see *Known Modeling Gaps* in [domain-model.md](../domain-model.md).
- **There is no real bulk delete.** The checkbox column and «מחיקת נבחרים» toolbar exist on four list
  screens and raise an information toast; there is no server endpoint. Plan B's B5.
- **A student cannot move between classes with history** — `Student.ClassId` is a single field with no
  record of where she was before.
- **A teacher cannot transfer a lesson or course to another teacher**, which is also the only way past
  the delete guard on a teacher account.
- **Deleting a teacher orphans four audit columns** that carry no foreign key (`B-48`).
- **A submission that is being processed or is already graded cannot be deleted.**
- **A teacher's grant cannot beat a lock.** Reopening the lesson result is the only route, and that is
  deliberate — a final grade already given must not move silently.
- **There is no per-class or per-assignment analytics screen.** The class signals in the bell are the
  only aggregate view, and they are computed on demand with no stored history (`B-25`).
