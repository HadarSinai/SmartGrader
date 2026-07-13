# Assignments — Flow Specification

**Scope**: Localization (English → Hebrew) + dead-column removal + validation-consistency fixes for the
existing Assignments feature, within
[assignments-list.component.ts](../../client/src/app/pages/assignments/assignments-list.component.ts)
and
[assignment-form.component.ts](../../client/src/app/pages/assignments/assignment-form.component.ts).
No backend changes required.

## Priority Fix: Remove Dead Columns / Localize Fully

**Problem**: `AssignmentExtended` fields (`difficulty`, `status`, `dueDate`, `maxScore`,
`completionRate`, `averageScore`) are never populated by the real `AssignmentResponseDto`, so 5 of 9
table columns are always blank/dashed; separately, the entire feature is in English.

**Proposed flow**:

1. Drop the `AssignmentExtended` cast in `loadAssignments()`; type `assignments` as
   `AssignmentResponseDto[]` (the real DTO) until/unless the backend actually adds those fields.
2. Remove the "רמה" (difficulty), "סטטוס" (status), "דדליין" (deadline), "נקודות" (maxScore/points),
   "השלמה" (completion), and "ממוצע" (average) columns from both the desktop table and mobile card.
3. Keep: title/description, bonus chip + `bonusValue` tag (both real, from `isBonus`/`bonusValue`),
   test-case count (`tests.length`), submission count (`submissionsCount`) — all backed by the real DTO.
4. Translate every remaining string in both files to Hebrew, matching the tone already used in
   Lessons/Students (e.g. "אין תרגילים להצגה", "תרגיל נמחק בהצלחה", "טעינת התרגילים נכשלה").

## User Flow: Create Assignment

**Entry Point**: `/lessons/:lessonId/assignments` → "תרגיל חדש" → `/lessons/:lessonId/assignments/new`.

**Flow Steps**:

1. Form renders empty: title (required), description (optional), method name (required, **already has**
   inline error — keep as-is), bonus checkbox + value, dynamic test-case rows.
2. **[Fix]** Add the same inline-error pattern already used for `methodName` to the `title` field too,
   for consistency within this same form: `<small class="p-error" *ngIf="form.get('title')?.invalid &&
form.get('title')?.touched">כותרת היא שדה חובה</small>`.
3. Clicks "יצירה" → `POST /api/lessons/{lessonId}/assignments`.
4. **[Fix]** Hebrew toast on success ("התרגיל נוצר בהצלחה") / failure ("יצירת התרגיל נכשלה") — currently
   hardcoded English.

**Exit Points**:

- Success → `/lessons/:lessonId/assignments`.
- Cancel → **[Fix]** confirm via `ConfirmationService` if the form is dirty (test-case rows in
  particular represent real authoring effort worth protecting from accidental loss).

## User Flow: Delete Assignment

**Entry Point**: Trash icon on an assignments-list row.

**Flow Steps**:

1. Click trash icon → `ConfirmationService.confirm()`.
2. **[Fix]** Fully localize and align with the gender-neutral standard set for Lessons/Students:
   - `message`: `"Are you sure you want to delete \"{title}\"?"` →
     `"האם למחוק את התרגיל \"{{assignment.title}}\"?  לא ניתן לשחזר פעולה זו."`
   - `header`: `"Confirm Delete"` → `"אישור מחיקה"`
   - Add explicit `acceptLabel: "מחיקה"`, `rejectLabel: "ביטול"` (currently relies on PrimeNG's English
     defaults since neither is set).
3. On accept → `DELETE /api/lessons/{lessonId}/assignments/{assignmentId}` → Hebrew success toast
   ("התרגיל נמחק בהצלחה") → reload list.
4. On error → Hebrew error toast ("מחיקת התרגיל נכשלה").

## Design Principles for This Flow

1. **Full localization parity**: this feature must reach the same Hebrew-only bar already met by
   Lessons' and Students' _list_ pages — currently it is the only feature area with zero Hebrew.
2. **Never render a column that is always empty**: drop `AssignmentExtended` fields from the UI until
   the backend actually supports them — a blank/dashed column across every row is worse than no column.
3. **Propagate the good pattern, don't just keep it local**: the `methodName` inline-validation
   approach found here is the best-practice reference for Lessons/Students/Submissions form fixes too
   (see [README.md](./README.md) consolidated recommendations).
4. **Explicit accept/reject labels on every `ConfirmationService.confirm()` call** — never rely on
   PrimeNG defaults, so the button text is always Hebrew and gender-neutral.

## Accessibility

See [accessibility-checklist.md](./accessibility-checklist.md). Specific to this flow: once the
confirm dialog is localized, verify screen readers announce the new Hebrew `aria-label`s/labels, not
cached English defaults.
