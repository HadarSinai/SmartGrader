# Area: Teacher — Content

> SmartGrader · Version 1.0 · Last updated 2026-08-27 · Status: as-built

| Version | Date | Change |
|---|---|---|
| 1.0 | 2026-08-27 | First edition. |

## Purpose

Everything a teacher authors **before** a student touches it: courses, the lessons inside them, and the
assignments inside those — including the test cases and structural requirements that decide every
grade that follows.

This is the area where a mistake is silent and expensive. A wrong expected output does not raise an
error; it produces wrong grades for a whole class, weeks later, with nothing to point at.

## Who Uses This

A teacher preparing a lesson, usually in a block of prep time rather than in front of a class. She
writes C# comfortably but is not a software engineer. She authors an assignment once and reuses the
shape of it constantly.

## Screens & Routes

<!-- gen:arearoutes teacher-content -->

| `path` | Full route | Screen |
|---|---|---|
| `lessons` | `/lessons` | Lessons list |
| `courses` | `/courses` | Courses list |
| `courses/new` | `/courses/new` | Course form — create |
| `courses/:id/edit` | `/courses/:id/edit` | Course form — edit |
| `lessons/new` | `/lessons/new` | Lesson form — create |
| `lessons/:id/edit` | `/lessons/:id/edit` | Lesson form — edit |
| `lessons/:lessonId/assignments` | `/lessons/:lessonId/assignments` | Assignments list |
| `lessons/:lessonId/assignments/new` | `/lessons/:lessonId/assignments/new` | Assignment form — create |
| `lessons/:lessonId/assignments/:assignmentId/edit` | `/lessons/…/edit` | Assignment form — edit |
| `assignments` | `/assignments` → redirects to `lessons` | ⚠️ a stub, and the topbar links to it |

<!-- /gen -->

⚠️ **`/assignments` is a `redirectTo` stub and the topbar links to it.** Clicking «תרגילים» lands on
Lessons and highlights the wrong navigation item. There is no standalone assignments screen — an
assignment exists only inside a lesson. Tracked as Plan B's B4.

## Functional Requirements

**TC-1** A teacher shall see only her own courses and lessons; another teacher's shall return 404, not
403, so ids cannot be probed.

**TC-2** A course shall belong to exactly one teacher, and a teacher shall not have two courses with
the same name.

**TC-3** A lesson shall belong to exactly one course and shall be assignable to one or more classes.

**TC-4** A lesson's date shall be entered and displayed as a Hebrew date and stored as a date-time.

**TC-5** The lessons list shall page at 10 rows with options 10/25/50, shall sort by course, subject or
date when the teacher clicks those headers, shall match the search box against **course name and
subject**, and shall additionally filter by class.

**TC-6** The assignments list shall page at 10 rows with options 10/25/50, shall sort by title or
submission count, and shall match the search box against **title and description**.

**TC-7** The courses list shall page at 10 rows with options 10/25/50, shall sort by name or lesson
count, and shall match the search box against **name only**.

**TC-8** Search text and filters shall be held in component state only, so navigating away and back
shall clear them.

**TC-9** The assignments list shall show, per assignment, its number of test cases and its number of
submissions.

**TC-10** An assignment shall declare a grading mode — whole program, single method, or multi-file —
and a method name where the mode requires one.

**TC-11** The system shall reject an assignment that has neither a test case nor a structural
requirement (`G-15`).

**TC-12** The system shall reject an assignment whose rubric does not sum to exactly its ceiling
(`G-14`), and shall state the required total in the message.

**TC-13** The system shall reject a scored requirement carrying fewer than one point (`G-16`).

**TC-14** The reference solution shall never be sent to a student on any path.

**TC-15** The system shall offer to generate candidate test cases with a language model, shall execute
each against the teacher's reference solution, and shall show the executed result where the two
disagree — the model's proposal shall never be saved unverified.

**TC-16** A test case shall default to core and to hidden, and the teacher shall change those
deliberately (`B-46`).

**TC-17** A required form field shall show its error inline beneath the field, only after the field has
been touched.

**TC-18** Deletion of a course, lesson or assignment shall be confirmed through a dialog that names
what is being deleted and states that it cannot be undone.

## Applicable Rules

| Rule | Why it reaches this area |
|---|---|
| `G-14`, `G-15`, `G-16`, `G-17` | the rubric the assignment form enforces |
| `G-1`, `G-10`, `G-11` | what a Blocking / Scored / Advisory requirement will do to a grade |
| `B-37` … `B-45` | **what the analyzer can and cannot see** — the limits a requirement is written against |
| `B-46` | the two test-case defaults, and why they fail closed |
| `D-1` … `D-15` | the form is the densest screen in the system and the hardest to keep accessible |

⚠️ **`B-37`…`B-44` belong in front of the teacher while she writes a requirement, not in a document she
reads afterwards.** "Must use inheritance" also passes on an interface implementation; "must use LINQ"
fails on method chaining; "at most 2 `if`" counts `if / else if / else` as two. These are not bugs to
file — they are the price of an analyzer that runs in milliseconds on code that may not even compile.

## Acceptance Criteria

**AC-1 (TC-1)** Given teacher A owns lesson 7, when teacher B requests `/api/lessons/7`, then the
server returns 404.

**AC-2 (TC-11)** Given an assignment with no test cases and no structural requirements, when she saves,
then the save is rejected with «לתרגיל חייב להיות לפחות מקרה בדיקה אחד או לפחות דרישה מבנית אחת…».

**AC-3 (TC-12)** Given tests allocated 60 and two scored requirements worth 25 and 20, when she saves,
then the save is rejected and the message names 100 as the required total.

**AC-4 (TC-12)** Given a bonus assignment with a bonus value of 20, when she saves a rubric summing to
120, then the save succeeds.

**AC-5 (TC-14)** Given an assignment with a reference solution, when a student requests it, then the
response's reference solution is an empty list.

**AC-6 (TC-15)** Given a generated test case whose expected output differs from what the reference
solution actually produces, when the verification runs, then the executed result is shown and marked as
the disagreement.

**AC-7 (TC-5)** Given 30 lessons across three courses, when she types a course name, then only that
course's lessons remain; when she navigates to a lesson and returns, then the search box is empty.

**AC-8 (TC-17)** Given the assignment form with an empty title, when she moves focus out of the title
field, then an error appears beneath it and not as a toast.

## Screen Composition

*Filled in phase A5.* The suspected problem is recorded now:

**The assignment form is the most crowded screen in the system** — 1,929 lines before its template was
extracted, with roughly ten regions in one form: general fields, grading mode, bonus, expected files,
the reference solution, test cases with two flags each, structural requirements with kind / construct /
threshold / severity / points, the rubric total, and the AI test-case generator.

Its template and styles were moved out in Plan B's B6; **the class was deliberately not split**,
because the structural-rules block shares `FormArray` state with the rubric getters and splitting it
blind is the line-count theatre the plan warns against. A5 decides what comes off the screen before A6
decides what moves in the code.

## Explicitly Not Supported

- **An assignment cannot exist outside a lesson.** There is no flat assignments screen and no flat API
  route; `/assignments` is a redirect.
- **A lesson has no topic entity** — `Subject` is free text, so two lessons about loops are two
  unrelated strings and nothing groups them.
- **A course belongs to one teacher.** Two teachers teaching C# keep two separate courses; there is no
  sharing and no transfer.
- **Courses and lessons carry no academic year**, so they accumulate indefinitely.
- **A lesson or assignment with submissions cannot be deleted** — the guard refuses rather than
  cascading.
- **There is no template or duplicate action** for an assignment; each is authored from scratch.
- **The analyzer cannot see types.** Any requirement that depends on knowing a variable's type, or on
  telling inheritance from interface implementation, cannot be expressed (`B-37`, `B-40`, `B-43`).
- **A language model never decides whether a requirement was met** — it only proposes test cases, which
  are then executed (`TC-15`).
