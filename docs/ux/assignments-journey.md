# Assignments — User Journey Map

**Persona**: Teacher (see [personas.md](./personas.md)).
**Scope**: Full lifecycle — List → Create → Edit → Delete — grounded in the current
`assignments-list`/`assignment-form` components.

## Stage 1: List / Discover

**Doing**: Navigates from a lesson row (`viewAssignments()`) into `/lessons/:lessonId/assignments`,
scans a 9-column table: title/description, difficulty, status, deadline, points, completion rate,
average score, tests/submissions count, actions.
**Thinking**: "This table looks rich — wait, why are difficulty, deadline, points and completion all
blank or dashes for literally every assignment?"
**Feeling**: 😕 Confused/distrustful — the table promises detail (progress bars, tags, sortable
columns) that never actually renders any value, because `AssignmentExtended`'s extra fields are never
populated by the real API response.
**Pain points**: 5 of 9 columns are dead weight for every row; only title/description, bonus flag,
test-case count, and submission count are ever real.
**Opportunity**: Remove or backend-support the extended columns (see Flow spec) so every visible column
is either real or absent.

## Stage 2: Create

**Doing**: Clicks "תרגיל חדש", fills title, description, method name, optional bonus + test cases,
saves.
**Thinking**: "The method-name error message is genuinely helpful — why don't the other fields (and
other forms in this app) work this way?"
**Feeling**: 🙂 Positive about the one field with inline validation; 😐 neutral/uncertain about the rest
(title has no equivalent feedback).
**Pain points**: Inconsistent validation feedback within the same form (methodName has it, title
doesn't); on save failure, an **English** toast appears — a jarring language switch if the teacher just
came from the (Hebrew) Lessons/Students screens.
**Opportunity**: Extend the existing inline-error pattern to every required field; translate all
toasts/dialogs on this feature to Hebrew.

## Stage 3: Edit

**Doing**: Clicks pencil icon, edits assignment details/tests, saves.
**Thinking**: Same as Create — appreciates the methodName validation, confused by the English toasts.
**Feeling**: 🙂/😐 mixed, consistent with Create stage.
**Pain points**: Same as Create.
**Opportunity**: Same fixes as Create.

## Stage 4: Delete

**Doing**: Clicks trash icon → sees an **entirely English** confirm dialog ("Are you sure you want to
delete...?", "Confirm Delete", default "Yes"/"No" buttons) → confirms.
**Thinking**: "Did I accidentally switch apps? This doesn't match the rest of the system at all."
**Feeling**: 😕 The most jarring moment in the whole Assignments journey — a completely untranslated
system dialog appears with no warning.
**Pain points**: 100% English confirm dialog with no localized accept/reject labels.
**Opportunity**: Translate to Hebrew and align with the gender-neutral standard already used for
Lessons/Students deletions (e.g. "האם למחוק את התרגיל...?" / "מחיקה" / "ביטול").

## Success Metric

Teacher experiences the Assignments feature as fully Hebrew and internally consistent with
Lessons/Students, and every visible table column shows a real, backend-sourced value for every row.
