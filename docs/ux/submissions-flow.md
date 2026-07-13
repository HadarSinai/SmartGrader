# Submissions — Flow Specification

**Scope**: Add missing Delete UX, fix status-label localization, and add lightweight async-grading
feedback, within
[submissions-list.component.ts](../../client/src/app/pages/submissions/submissions-list.component.ts)
/ [.html](../../client/src/app/pages/submissions/submissions-list.component.html) and
[submission-detail.component.ts](../../client/src/app/pages/submissions/submission-detail.component.ts).
Backend already supports `DELETE /api/students/{studentId}/submissions/{submissionId}` — no backend
change needed for the Delete flow.

## Priority Fix: Add Delete (currently entirely missing)

**Problem**: No delete action exists anywhere in the Submissions feature, unlike every other feature
area, despite the backend endpoint already existing.

**Proposed flow** (mirrors the established Lessons/Students/Assignments pattern):

1. Import `ConfirmationService` + `ConfirmDialogModule` into `submissions-list.component.ts`
   (`providers: [ConfirmationService]`), add a trash-icon `p-button` per row (desktop table + mobile
   card), and a `<p-confirmDialog>` in the template — same shape as
   `lessons-list.component.ts`/`students-list.component.ts`.
2. `confirmDelete(submission)`:
   - `message`: `"האם למחוק את ההגשה עבור \"{{submission.assignmentName}}\"?  לא ניתן לשחזר פעולה זו."`
   - `header`: `"אישור מחיקה"`
   - `acceptLabel: "מחיקה"`, `rejectLabel: "ביטול"` (gender-neutral, consistent with other features).
3. `deleteSubmission(id)` → `DELETE /api/students/{studentId}/submissions/{submissionId}` → Hebrew
   success toast ("ההגשה נמחקה בהצלחה") → reload list; Hebrew error toast on failure
   ("מחיקת ההגשה נכשלה").
4. Optionally surface the same delete action on `submission-detail.component.ts`'s header actions row
   (next to "עריכה"), for a student/teacher already viewing a specific submission.

## Priority Fix: Human-Readable, Fully Hebrew Status Labels

**Problem**: `getStatusLabel()` returns raw enum strings (`PendingAi`, `ProcessingAi`, `AiFailed`,
`Done`) verbatim for the list; the detail page hardcodes English tag values.

**Proposed flow**:

1. Centralize one Hebrew status-label map (usable by both list and detail):
   ```ts
   const STATUS_LABELS_HE: Record<string, string> = {
     PendingAi: "ממתין לבדיקה",
     ProcessingAi: "בבדיקה...",
     Done: "נבדק",
     AiFailed: "שגיאת בדיקה",
     CompilationFailed: "שגיאת קומפילציה",
   };
   ```
2. Replace `getStatusLabel()`'s pass-through with a lookup into this map (fallback: `"לא ידוע"`).
3. Replace the hardcoded English tag `value`s in `submission-detail.component.ts`
   (`"Done"`, `"Pending AI"`, `"Processing AI"`, `"AI Failed"`, `"Compile Error"`, `"Unknown"`) with the
   same map's Hebrew values.
4. Add a CSS spin animation class to the `pi-spinner` icon used for `ProcessingAi` so it visually
   communicates "in progress," not just a static clock-like icon.

## User Flow: Async Grading Feedback (light-touch, no backend change)

**Entry Point**: Detail page for a submission still `PendingAi`/`ProcessingAi`.

**Flow Steps**:

1. On load, if `status` is `PendingAi` or `ProcessingAi`, start a lightweight polling interval
   (e.g. every 7s) re-fetching `GET /api/students/{studentId}/submissions/{submissionId}`.
2. Stop polling once status becomes `Done`/`AiFailed`/`CompilationFailed`, or when the component is
   destroyed (`ngOnDestroy` clears the interval — avoid leaking timers).
3. Show a small "מתעדכן אוטומטית..." (auto-updating) hint near the status tag while polling is active,
   so the behavior is discoverable rather than silent.

**Exit Points**:

- Status resolves to `Done`/`AiFailed`/`CompilationFailed` → polling stops, final result renders.
- User navigates away → polling interval is cleared.

## User Flow: Resubmit Call-to-Action on Failure

**Entry Point**: Detail page showing `CompilationFailed` or `AiFailed`.

**Flow Steps**:

1. Inside the existing compile-error/AI-error result block, add a prominent button:
   "ערכי והגישי מחדש" (edit and resubmit) → `navigateToEdit()` (already exists in the header, just
   duplicated contextually where the user's attention already is).

## Design Principles for This Flow

1. **Feature parity for destructive actions**: every entity with a backend delete endpoint must expose
   it in the client, using the same `ConfirmationService` + gender-neutral-copy pattern everywhere.
2. **Never expose raw enum/technical values in the UI** — always map backend enums to a human label
   before rendering, in the user's language.
3. **Async states need a visible "it's working" signal** — a status label alone is not enough; pair it
   with either polling or, at minimum, a clearly discoverable manual refresh action.
4. **Guide the user to the next action from a failure state**, don't just report the failure.

## Accessibility

See [accessibility-checklist.md](./accessibility-checklist.md). Specific to this flow: polling-driven
status changes must be announced via `aria-live` (already flagged generically in the checklist,
called out here because Submissions is the one feature area where this actually matters in practice).
