# SmartGrader — Accessibility Requirements Checklist (Hebrew/RTL, PrimeNG)

Reusable accessibility checklist for every SmartGrader client screen. Each per-feature flow spec
(`docs/ux/{feature}-flow.md`) must reference this file and call out any deviations or feature-specific
additions instead of repeating the whole list.

Client context: Angular 17 standalone components, PrimeNG, **RTL/Hebrew UI** (per
`client.instructions.md` — RTL must remain correct in every change).

## Keyboard Navigation

- [ ] All interactive elements (buttons, table row actions, form inputs) reachable via Tab key.
- [ ] Logical tab order in RTL: visually right-to-left, top to bottom (verify PrimeNG components don't
      default to LTR tab order).
- [ ] Visible focus indicator on every focusable element (not just the browser default outline, which
      can be invisible on custom PrimeNG themes).
- [ ] Enter/Space activate buttons and icon-buttons (e.g., row edit/delete icons).
- [ ] Escape closes `p-dialog` (forms-in-dialog) and `p-confirmDialog` (delete confirmations).

## Screen Reader Support

- [ ] Every icon-only button (edit/delete row actions) has an `aria-label` in Hebrew describing the
      action and the row it acts on (e.g. "מחק שיעור: מבוא לפייתון", not just "מחק").
- [ ] Form inputs have associated `<label>` elements — never rely on `placeholder` alone (this is
      already a client.instructions.md rule for forms; call it out explicitly here too).
- [ ] `MessageService` toast content is announced (PrimeNG toasts use `aria-live` by default — verify,
      don't assume).
- [ ] `ConfirmationService` dialog reads its message and both action button labels before the user acts.
- [ ] Loading states (`p-skeleton`, spinners) are announced as "טוען..." for screen readers, not just
      visually shown.
- [ ] AI-grading async states (`PendingAi`/`ProcessingAi`/`Done`/`AiFailed`/`CompilationFailed` on
      Submissions) announce state changes when polling updates the status — a silent visual-only status
      badge change is not sufficient.

## Visual Accessibility

- [ ] Text contrast minimum 4.5:1 (WCAG AA) — verify against the design tokens in `client/spec.md`
      (`--app-text-strong`, etc.), not just default PrimeNG theme colors.
- [ ] Interactive elements (table action icons, buttons) minimum 24x24px touch target; prefer 44px
      height for primary actions per WCAG target-size guidance.
- [ ] Status is never color-only: `SubmissionStatus` badges and any pass/fail indicators must pair color
      with an icon and/or text label (e.g. not just a green/red dot).
- [ ] Layout does not break at 200% text zoom or at the three verification breakpoints already defined
      in `client/spec.md` (360px / 768px / 1280px).
- [ ] Focus and hover states are visually distinguishable from each other and from the default state.

## RTL-Specific Checks (in addition to generic WCAG)

- [ ] Icons that imply direction (e.g., arrows, chevrons for "next/back", expand/collapse) are mirrored
      correctly for RTL.
- [ ] Action columns in PrimeNG tables align to the visual-left (which is the RTL "end") consistently
      across all list screens.
- [ ] Numbers and dates (which remain LTR even in Hebrew text) do not get visually reversed inside RTL
      containers.

## Example (for a delete-confirmation flow)

- Trigger: icon-only delete button → `aria-label="מחק שיעור: {{lesson.name}}"`.
- `ConfirmationService.confirm()` dialog: message states exactly what will be deleted and that it
  cannot be undone; "אישור"/"ביטול" buttons are both reachable via Tab and activate via Enter/Space.
- On success: `MessageService` toast, `severity: 'success'`, announced via `aria-live`.
- On error: toast `severity: 'error'` with the mapped message from `ApiErrorInterceptor`, not a raw
  HTTP error.
