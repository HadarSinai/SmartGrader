# Students — User Journey Map

**Persona**: Teacher (see [personas.md](./personas.md)).
**Scope**: Full lifecycle — List → Create → Edit → Delete — grounded in the current
`students-list`/`student-form` components (the Figma-pilot design-token screen per `client/spec.md`).

## Stage 1: List / Discover

**Doing**: Opens `/students`, scans the table showing name, class, "ממוצע ציון" (grade avg), "נוכחות"
(attendance), "הוגש בזמן" (on-time), points/stars, and a status icon; can search/filter by class and a
grade-minimum slider.
**Thinking**: "This student has a 70% average and full attendance — should I check in with them?"
**Feeling**: 🙂 Confident at first glance (the visual polish is the design-system pilot) — but this
confidence is **misplaced**, since those numbers are fabricated demo values, not this student's real
data.
**Pain points**: The most-prominent columns (grade average, attendance, on-time, points, status) are
100% fake/static; the two real, useful fields (`submissionsCount`, `lessonResultsCount`) aren't shown
anywhere in the table.
**Opportunity**: Replace the fake metric columns with the real `submissionsCount` /
`lessonResultsCount` fields (or clearly remove the metric columns entirely until the backend actually
supports them) — do not ship fabricated per-student data.

## Stage 2: Create

**Doing**: Clicks "סטודנט חדש" (via `navigateToCreate()`), fills full name + class, saves.
**Thinking**: "Only two fields — good, this should be quick." / "Why won't Save enable?"
**Feeling**: 🙂 Mostly fine — the form itself is simple and appropriately short.
**Pain points**: Same as Lessons — no inline required-field messages; English-language toasts on
failure break Hebrew-UI consistency.
**Opportunity**: Inline validation messages + Hebrew-only toasts (see Flow spec).

## Stage 3: Edit

**Doing**: Clicks pencil icon, edits full name/class, saves.
**Thinking**: "Did this save? Is my class assignment now correct?"
**Feeling**: 🙂 Fine, low friction given only 2 fields.
**Pain points**: Same validation/English-toast gaps as Create.
**Opportunity**: Same fixes as Create stage.

## Stage 4: Delete

**Doing**: Clicks trash icon → confirm dialog → confirms.
**Thinking**: "The message assumes I'm a woman ('בטוחה'/'מחקי') — but the success toast right after
uses gender-neutral phrasing. Inconsistent."
**Feeling**: 😕 Mildly jarring inconsistency within the same screen.
**Pain points**: `confirmDelete()` copy not yet aligned with the already-fixed gender-neutral toast
copy in `deleteStudent()`.
**Opportunity**: Align confirm-dialog copy to the same gender-neutral standard already used elsewhere
on this page.

## Success Metric

Teacher can trust every number shown in the students table as real, and complete create/edit/delete in
under 30 seconds with consistent, correct Hebrew copy throughout.
