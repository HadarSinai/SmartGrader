---
name: client-flow-fix-implementation-pattern
description: "Use when changing user-facing behaviour or copy in a SmartGrader Angular list/form component: Hebrew-only gender-neutral copy, the one ConfirmationService.confirm() shape used for every destructive action, the inline-validation pattern (a p-error message under a required field instead of a silently disabled button), and the unsaved-changes guard on Cancel. USE FOR: 'fix the copy on this screen', 'add a delete confirmation', 'add inline validation to this field', 'warn before discarding unsaved changes'. NOT for visual/token work (see client-design-token-rollout-pattern) or for deciding what a screen should show (that is docs/areas/ and docs/design-system.md)."
---

# Client Flow-Fix Implementation Pattern

This is the client-side counterpart to `backend-mediatr-query-handler-pattern` /
`backend-repository-query-pattern`: a repeatable recipe for turning a documented UX fix into an actual
code change, instead of a repository/handler.

## When to Use

- Applying a decision from [docs/areas/](../../../docs/areas/) or the **Shared patterns** section of
  [docs/design-system.md](../../../docs/design-system.md) to a real `{feature}-list.component` /
  `{feature}-form.component`.
- Replacing hardcoded English or gendered Hebrew toast/dialog copy with gender-neutral Hebrew.
- Adding an inline "שדה חובה"-style validation message under a required field that currently only
  disables the Save button silently.
- Adding a "confirm before discarding unsaved changes" guard on Cancel.

## Workflow

1. **Read the specification first**: the screen's entry in [docs/areas/](../../../docs/areas/) says what
   it is for and what it shows; **Shared patterns** in
   [docs/design-system.md](../../../docs/design-system.md) fixes the exact shape of toasts, delete
   confirmations, empty/loading states, dates and copy. List the discrete changes before touching code,
   and make only those.
2. **Locate the real files**: `client/src/app/pages/{feature}/{feature}-list.component.{ts,html,css}`
   and `{feature}-form.component.{ts,html,css}` — **every component is three files**, and
   `ComponentFileLayoutTests` fails a template or stylesheet written inline in the `.ts`.
3. **Copy replacement** — Hebrew-only, gender-neutral:
   - Replace any English toast string (`'Error'`, `'Success'`, `'Lesson created successfully'`) with
     Hebrew equivalents already used elsewhere in the same file/feature for consistency.
   - Replace gendered verb/adjective forms (e.g. `"בטוחה שברצונך למחוק..."`, `"מחקי"`) with neutral
     phrasing (e.g. `"האם למחוק את "..."? לא ניתן לשחזר פעולה זו."`, `"מחיקה"`). Never assume the
     user's gender in system-generated copy.
   - Keep already-neutral copy (e.g. `"ביטול"`) unchanged.
4. **`ConfirmationService.confirm()` shape** — every destructive or discard action uses this exact
   config shape (PrimeNG `ConfirmationService`, injected via constructor, never `window.confirm`):
   ```typescript
   this.confirmationService.confirm({
     message: `האם למחוק את "${item.name}"? לא ניתן לשחזר פעולה זו.`,
     header: "אישור מחיקה",
     acceptLabel: "מחיקה",
     rejectLabel: "ביטול",
     accept: () => {
       /* call the service, then reload/navigate, toast on success/error */
     },
   });
   ```
   For a "discard unsaved changes" guard on Cancel, use the same shape with a header like
   `"שינויים שלא נשמרו"` and only navigate away inside `accept`.
5. **Inline validation** — replicate the exact pattern already proven on `methodName` in
   [assignment-form.component.html](../../../client/src/app/pages/assignments/assignment-form.component.html)
   onto every other required field that's currently only silently disabling Save:

   ```html
   <input
     pInputText
     class="w-full"
     id="fieldName"
     formControlName="fieldName"
   />
   <small
     class="p-error"
     *ngIf="form.get('fieldName')?.invalid && form.get('fieldName')?.touched"
   >
     שם השדה הוא שדה חובה
   </small>
   ```

   - The error message only shows once the control is `touched` (blurred or submitted), never
     immediately on load.
   - Keep `Validators.required` (or the existing validator set) on the `FormControl` unchanged — this
     is purely a template/UX addition, not a validation-logic change.

6. **Verify against [client.instructions.md](../../../.github/instructions/client.instructions.md)**
   after every change:
   - Component stays `standalone: true` with explicit `imports: [...]`.
   - Services still return `Observable<T>` (never `Promise`), errors handled in
     `.subscribe({ error: (err) => ... })`.
   - Notifications go through `MessageService`/`ConfirmationService`, never `alert`/`console.error`/
     `window.confirm`.
   - Navigation via `Router.navigate([...])`, never `location.href`.
   - No new hardcoded English strings introduced.
7. **Account for every change you listed in step 1** — the work is done when each one has a
   corresponding code change, and nothing else was touched.

## Real Examples

Gender-neutral delete confirm — the one shape, from
[docs/design-system.md](../../../docs/design-system.md) § Toasts and delete confirmations:

```
message: "בטוחה שברצונך למחוק את..."  →  "האם למחוק את \"{{lesson.name}}\"? לא ניתן לשחזר פעולה זו."
acceptLabel: "מחקי"  →  "מחיקה"
rejectLabel: "ביטול"  (already neutral, keep)
```

Inline validation reference implementation —
[assignment-form.component.html](../../../client/src/app/pages/assignments/assignment-form.component.html)
(the `methodName` field): label with `*`, `pInputText` bound via `formControlName`, and a
`<small class="p-error" *ngIf="...invalid && ...touched">` message directly beneath the input.

## Pitfalls

- Don't fix things the specification did not ask for — scope creep makes the change impossible to
  review against the spec.
- Don't invent new Hebrew copy from scratch when equivalent phrasing already exists elsewhere in the
  same feature — reuse it for consistency.
- Don't replace `ConfirmationService`/`MessageService` calls with `alert`/`window.confirm` even
  temporarily.
- Don't change validator logic (`Validators.required`, etc.) when the task is only about showing the
  existing error visibly — the bug is UX visibility, not validation correctness.
- Don't forget `touched` in the `*ngIf` — showing the error before the user has interacted with the
  field is itself a UX regression.

## See Also

- [spec-feature-area-doc](../spec-feature-area-doc/SKILL.md) — how the specification this skill
  consumes is written.
- [client-design-token-rollout-pattern](../client-design-token-rollout-pattern/SKILL.md) — the sibling
  skill for visual/design-token changes (as opposed to copy/validation fixes).
