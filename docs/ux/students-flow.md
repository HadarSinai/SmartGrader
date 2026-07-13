# Students — Flow Specification

**Scope**: Create/Delete UX improvements + a data-integrity fix for the Students list, within
[students-list.component.ts](../../client/src/app/pages/students/students-list.component.ts),
[students-list.component.html](../../client/src/app/pages/students/students-list.component.html), and
[student-form.component.ts](../../client/src/app/pages/students/student-form.component.ts). No backend
changes required for the flows below (the fake-data removal is a client-only change).

## Priority Fix: Remove Fabricated Metrics from the List

**Problem**: `toVm()` invents `gradeAvg`, `attendance`, `onTimeFraction`, and `stars` from static demo
arrays, rendered as if real per-student data.

**Proposed flow**:

1. Remove the demo-value generation in `toVm()` and the `StudentRowVm` fake fields entirely.
2. Replace the "ממוצע ציון" / "נוכחות" / "הוגש בזמן" / points columns with two real, backend-sourced
   columns: **"הגשות"** (`submissionsCount`) and **"תוצאות שיעור"** (`lessonResultsCount`).
3. Keep the visual polish (knob/tag styling) only where it wraps a real number; a "status" column
   should be removed until there's a real status field to represent, rather than always rendering a
   static success icon.
4. Any of these counts being `0` should render as `0`, not as an empty/omitted cell — it's a real,
   meaningful value (a student with 0 submissions is worth flagging, not hiding).

## User Flow: Create Student

**Entry Point**: `/students` → "סטודנט חדש" → `/students/new`.

**Flow Steps**:

1. Form renders empty, 2 required fields (fullName, className).
2. **[Fix]** Inline error message under any invalid field on blur/submit-attempt (e.g. "שדה חובה"),
   not just a disabled Save button.
3. Clicks "יצירה" → `POST /api/students`.
4. **[Fix]** Hebrew-only toasts on success/error (currently hardcoded English "Success"/"Error" strings
   in `student-form.component.ts`), matching the tone already used on `students-list.component.ts`.

**Exit Points**:

- Success → `/students`.
- Cancel → **[Fix]** confirm via `ConfirmationService` if the form is dirty.

## User Flow: Delete Student

**Entry Point**: Trash icon on a students-list row.

**Flow Steps**:

1. Click trash icon → `ConfirmationService.confirm()`.
2. **[Fix]** Align confirm-dialog copy to the gender-neutral standard already used in this same file's
   success toast:
   - `message`: `"בטוחה שברצונך למחוק את..."` → `"האם למחוק את "{{student.fullName}}"?  לא ניתן לשחזר פעולה זו."`
   - `acceptLabel`: `"מחקי"` → `"מחיקה"`
3. On accept → `DELETE /api/students/{id}` → existing gender-neutral success toast
   (`"הסטודנט/ית נמחק/ה בהצלחה"`, already correct — keep) → reload list.
4. On error → existing gender-neutral error toast (already correct — keep).

## Design Principles for This Flow

1. **Never fabricate data**: a list screen must only render fields that exist on the real
   `StudentResponseDto`; if a metric isn't backed by the API yet, omit the column rather than inventing
   values.
2. **Consistency within a single file**: the gender-neutral copy standard already adopted in
   `deleteStudent()`'s toast must be applied everywhere else in the same component (starting with
   `confirmDelete()`).
3. **Language consistency**: Hebrew-only user-facing strings across both list and form pages.
4. **Explain, don't just disable**: same principle as Lessons — pair disabled Save with a visible
   reason.

## Codebase Hygiene Note (non-UX, flag only)

`students.component.ts` (inline-template, unused) is a legacy duplicate of the real pilot component —
flag for cleanup, not part of this flow change.

## Accessibility

See [accessibility-checklist.md](./accessibility-checklist.md). Specific to this flow: once fake
`p-knob` gauges are removed, re-verify `aria-label`s on the replacement count columns (e.g.
`aria-label="מספר הגשות"`).
