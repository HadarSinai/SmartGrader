# Students — Jobs-to-be-Done

**Persona**: Teacher only (roster/admin management — see [personas.md](./personas.md)).

## Job Statement

When I'm setting up my class roster or checking in on a student's overall progress, I want an accurate,
trustworthy view of who's in my class and how they're doing, so I can decide who needs attention without
being misled by numbers that aren't real.

## Context

- **Situation**: Either roster setup (adding/removing students at the start of a term) or a quick glance
  at the students table to gauge overall class performance.
- **Motivation**: Wants the students list to reflect real backend data (`submissionsCount`,
  `lessonResultsCount`) so decisions ("who needs help?") are grounded in fact.
- **Outcome**: An accurate `StudentResponseDto` record exists per student, and the list view shows only
  real, backend-sourced metrics.

## Current Solution & Pain Points (grounded in current code)

Current implementation: [students-list.component.ts](../../client/src/app/pages/students/students-list.component.ts)

- [students-list.component.html](../../client/src/app/pages/students/students-list.component.html)
- [student-form.component.ts](../../client/src/app/pages/students/student-form.component.ts).

* **Critical finding — fabricated data presented as real**: `toVm()` in `students-list.component.ts`
  hardcodes `gradeAvg`, `attendance`, `onTimeFraction`, and `stars` from static demo arrays (explicitly
  commented `// Demo values (until backend provides real metrics)`), e.g.
  `[0, 70, 0, 100, 0][idx % 5]`. The table then renders these as `p-knob` gauges ("ממוצע ציון",
  "נוכחות"), an "on-time" fraction, and a star rating — **none of which exist on the real
  `StudentResponseDto`** (which only has `id`, `fullName`, `className`, `createdAt`,
  `submissionsCount`, `lessonResultsCount`). This is exactly the "unnecessary/misleading things" the
  teacher persona would distrust once noticed — every student shows the _same_ small set of
  demo-cycled values regardless of who they actually are.
* **Real available fields are hidden instead**: `submissionsCount` and `lessonResultsCount` — both
  real, backend-provided, and directly useful for "how is this student doing?" — are not shown in the
  table at all today, while fake metrics occupy the prime columns.
* **Static, non-functional status icon**: the "סטטוס" column always renders the same
  `pi-check-circle` success icon and a hardcoded "0" points value for every row — decorative, not
  data-driven.
* **Same gendered-copy bug as Lessons**: `confirmDelete()` still hardcodes `"בטוחה שברצונך למחוק..."`
  and `acceptLabel: "מחקי"`, even though the delete-success toast on this same page was already fixed
  to be gender-neutral (`"הסטודנט/ית נמחק/ה בהצלחה"`) — an inconsistency within the same file.
* **Same English-toast bug as Lessons**: `student-form.component.ts` hardcodes `"Error"`/`"Success"`/
  `"Failed to load student"` in English, inconsistent with the Hebrew list page.
* **No inline field validation feedback**: identical gap to Lessons — Save button disables silently.
* **Legacy duplicate component**: `students.component.ts` (unused, inline-template) sits alongside the
  real pilot component — dead code to flag for cleanup.

## Consequence of the Gaps

- Showing fake per-student performance data is the most serious finding across the whole audit: a
  teacher could genuinely believe a student has 70% attendance or a 100% grade average when the number
  is a demo placeholder cycling by row index, not tied to that student at all. This must be prioritized
  above the cosmetic fixes shared with other features.
