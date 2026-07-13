# Lessons — Jobs-to-be-Done

**Persona**: Teacher only (admin/content-management surface — see [personas.md](./personas.md)).

## Job Statement

When I'm prepping for a class or reorganizing my curriculum, I want to create, update, and remove
lesson records quickly and with confidence that I entered everything correctly, so I can move on to
building assignments without second-guessing myself.

## Context

- **Situation**: Either quiet prep time (creating several lessons ahead of a term) or a quick edit
  between classes (fixing a typo in a lesson name/date).
- **Motivation**: Wants the lesson list to be an accurate, trustworthy source of truth she can hand off
  to (or be seen by) other teachers/admins.
- **Outcome**: A correct `LessonResponseDto` exists, and she can immediately continue to
  `/lessons/:id/assignments` to build content for it.

## Current Solution & Pain Points (grounded in current code)

Current implementation: [lessons-list.component.ts](../../client/src/app/pages/lessons/lessons-list.component.ts)

- [lesson-form.component.ts](../../client/src/app/pages/lessons/lesson-form.component.ts).

* **Gendered Hebrew copy bug**: the delete-confirmation dialog hardcodes feminine grammar —
  `"בטוחה שברצונך למחוק..."` and `acceptLabel: "מחקי"` — which is incorrect/exclusionary if the
  logged-in teacher identifies as male. This is a real correctness bug, not just a style nit.
* **Inconsistent language**: the list page's error/success toasts are in Hebrew ("שגיאה", "השיעור נמחק
  בהצלחה"), but the form page's toasts are hardcoded in **English** ("Error", "Failed to load lesson",
  "Lesson updated successfully"). A teacher using a Hebrew-only UI will suddenly see English error text
  if a save fails — confusing and looks unfinished.
* **No inline field validation feedback**: the form only disables the Save button when invalid
  (`[disabled]="form.invalid"`); there is no per-field error message (e.g. "שדה חובה") telling her
  _which_ field is missing or why Save is greyed out.
* **No unsaved-changes protection**: `onCancel()` navigates away immediately with no confirmation,
  risking silent loss of typed content.
* **Legacy duplicate component**: [lessons.component.ts](../../client/src/app/pages/lessons/lessons.component.ts)
  is an old, unused, inline-template version still sitting in the same folder — dead code that could
  confuse future maintainers (not a user-facing issue, but a codebase-hygiene one to flag).
* **Positive pattern already present** (keep): the `assignmentsCount` badge-button on each row is a
  good shortcut into the child resource and should be preserved/reused as a pattern for other
  entities with nested children.

## Consequence of the Gaps

- The gendered/mixed-language copy erodes the "production ready" trust described in the Teacher
  persona's pain points.
- Lack of inline validation means she may retry saving several times without understanding why, an
  avoidable friction that isn't necessary for a 4-field form.
