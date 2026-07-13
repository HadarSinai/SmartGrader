# Lessons — Flow Specification

**Scope**: Create/Delete UX improvements for the existing Lessons feature. No new routes, no backend
changes — all fixes are copy/validation/consistency improvements within
[lessons-list.component.ts](../../client/src/app/pages/lessons/lessons-list.component.ts) and
[lesson-form.component.ts](../../client/src/app/pages/lessons/lesson-form.component.ts).

## User Flow: Create Lesson

**Entry Point**: `/lessons` → "שיעור חדש" button → `/lessons/new`.

**Flow Steps**:

1. Form renders empty, all 4 fields required (name, subject, lessonDate, teacherName).
2. **[Fix]** As the teacher types/blurs each field, show an inline error message under any invalid
   field (e.g. "שדה חובה") instead of only disabling the Save button silently.
3. Clicks "יצירה" → `POST /api/lessons`.
4. **[Fix]** On success: Hebrew toast "השיעור נוצר בהצלחה" (currently English "Lesson created
   successfully") → navigate to `/lessons`.
5. **[Fix]** On error: Hebrew toast "יצירת השיעור נכשלה" (currently English) — routed through the
   existing `ApiErrorInterceptor` mapping, not a hardcoded string.

**Exit Points**:

- Success → `/lessons` (unchanged).
- Cancel → **[Fix]** if the form is dirty, confirm via `ConfirmationService` before discarding
  ("יש לך שינויים שלא נשמרו. לצאת בכל זאת?").

## User Flow: Delete Lesson

**Entry Point**: Trash icon on a lessons-list row (desktop table or mobile card).

**Flow Steps**:

1. Click trash icon → `ConfirmationService.confirm()`.
2. **[Fix]** Replace hardcoded feminine-gendered copy with gender-neutral phrasing:
   - `message`: `"בטוחה שברצונך למחוק את..."` → `"האם למחוק את "{{lesson.name}}"?  לא ניתן לשחזר פעולה זו."`
   - `acceptLabel`: `"מחקי"` → `"מחיקה"`
   - `rejectLabel`: `"ביטול"` (already neutral, keep).
3. On accept → `DELETE /api/lessons/{id}` → Hebrew success toast (already correct on this page) →
   reload list.
4. On error → Hebrew error toast (already correct on this page).

**Exit Points**:

- Success: row removed from list, list reloaded.
- Error: dialog closes, list unchanged, error toast shown.

## Design Principles for This Flow

1. **Language consistency**: every toast/dialog string across list + form pages must be Hebrew —
   audit and fix the form component's hardcoded English strings first (`Error`, `Success`,
   `Failed to load lesson`, `Lesson {op} successfully`).
2. **Gender-neutral Hebrew copy**: avoid gendered verb/adjective forms in system-generated copy
   (confirm dialogs, toasts) since the teacher's gender is unknown to the app.
3. **Explain, don't just disable**: a disabled Save button must always be paired with a visible reason
   (inline field error), never silent.
4. **Confirm destructive/discarding actions consistently**: deletion already uses
   `ConfirmationService`; extend the same discipline to "cancel with unsaved changes."

## Codebase Hygiene Note (non-UX, flag only)

`lessons.component.ts` (inline-template, unused) is a legacy duplicate of `lessons-list.component.ts`.
Recommend removing it in a follow-up cleanup — not part of this UX flow change.

## Accessibility

See [accessibility-checklist.md](./accessibility-checklist.md). Specific to this flow: the delete
confirm dialog's `aria-label`s must be updated in lockstep with the gender-neutral copy fix above so
screen readers announce the corrected text.
