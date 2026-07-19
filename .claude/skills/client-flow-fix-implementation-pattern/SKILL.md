---
name: client-flow-fix-implementation-pattern
description: "Use when turning a **[Fix]** step from a docs/ux/{feature}-flow.md spec into real Angular code in the SmartGrader client: Hebrew-only gender-neutral copy replacement, ConfirmationService.confirm() config, or replicating the inline-validation pattern (from assignment-form.component.ts's methodName field) onto other required form fields. USE FOR: 'implement the [Fix] steps for X', 'apply the flow spec to the list/form component', 'add inline validation like methodName does'. NOT for writing the flow spec itself (see ux-flow-spec-pattern) or design-token/visual rollout (see client-design-token-rollout-pattern)."
---

# Client Flow-Fix Implementation Pattern

This is the client-side counterpart to `backend-mediatr-query-handler-pattern` /
`backend-repository-query-pattern`: a repeatable recipe for turning a documented UX fix into an actual
code change, instead of a repository/handler.

## When to Use

- A `docs/ux/{feature}-flow.md` file has one or more `**[Fix]**` markers (e.g. Lessons, Students,
  Assignments, Submissions) and you need to apply them to the real `{feature}-list.component.ts` /
  `{feature}-form.component.ts`.
- Replacing hardcoded English or gendered Hebrew toast/dialog copy with gender-neutral Hebrew.
- Adding an inline "שדה חובה"-style validation message under a required field that currently only
  disables the Save button silently.
- Adding a "confirm before discarding unsaved changes" guard on Cancel.

## Workflow

1. **Read the flow spec**: open `docs/ux/{feature}-flow.md` and list every `**[Fix]**` bullet — each one
   is a discrete, independently-verifiable change. Do not touch anything not marked `[Fix]`.
2. **Locate the real files**: `client/src/app/pages/{feature}/{feature}-list.component.ts` and
   `{feature}-form.component.ts` (check both the `.ts` inline template and any sibling `.html`/`.css` —
   this codebase mixes both styles).
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
   [assignment-form.component.ts](../../../client/src/app/pages/assignments/assignment-form.component.ts)
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
7. **Mark off `[Fix]` bullets** mentally (or in your summary) as you complete each one — a flow file is
   only "done" when every `[Fix]` marker in it has a corresponding code change.

## Real Examples

Gender-neutral delete confirm (from [lessons-flow.md](../../../docs/ux/lessons-flow.md)):

```
message: "בטוחה שברצונך למחוק את..."  →  "האם למחוק את \"{{lesson.name}}\"? לא ניתן לשחזר פעולה זו."
acceptLabel: "מחקי"  →  "מחיקה"
rejectLabel: "ביטול"  (already neutral, keep)
```

Inline validation reference implementation —
[assignment-form.component.ts](../../../client/src/app/pages/assignments/assignment-form.component.ts)
lines ~112–131 (the `methodName` field): label with `*`, `pInputText` bound via `formControlName`, and a
`<small class="p-error" *ngIf="...invalid && ...touched">` message directly beneath the input.

## Pitfalls

- Don't fix things that aren't marked `[Fix]` in the flow doc — scope creep makes the change harder to
  review against the spec.
- Don't invent new Hebrew copy from scratch when equivalent phrasing already exists elsewhere in the
  same feature — reuse it for consistency.
- Don't replace `ConfirmationService`/`MessageService` calls with `alert`/`window.confirm` even
  temporarily.
- Don't change validator logic (`Validators.required`, etc.) when the `[Fix]` is only about showing the
  existing error visibly — the bug is UX visibility, not validation correctness.
- Don't forget `touched` in the `*ngIf` — showing the error before the user has interacted with the
  field is itself a UX regression.

## See Also

- [ux-flow-spec-pattern](../ux-flow-spec-pattern/SKILL.md) — how the `{feature}-flow.md` this skill
  consumes was written.
- [client-design-token-rollout-pattern](../client-design-token-rollout-pattern/SKILL.md) — the sibling
  skill for visual/design-token changes (as opposed to copy/validation fixes).
