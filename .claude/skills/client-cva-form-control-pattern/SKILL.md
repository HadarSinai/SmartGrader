---
name: client-cva-form-control-pattern
description: "Use when creating or reviewing a custom ControlValueAccessor form control in the SmartGrader Angular client: a shared standalone component under src/app/components/ that plugs into Reactive Forms via formControlName (e.g. the hebrew-date-picker with three PrimeNG dropdowns). Covers NG_VALUE_ACCESSOR provider, writeValue/registerOnChange/registerOnTouched, null handling, dependent-dropdown resets, and RTL. USE FOR: 'create a custom form control', 'implement ControlValueAccessor', 'build the hebrew date picker component', 'wrap PrimeNG dropdowns as one form value'. NOT for plain forms using built-in controls (see client-flow-fix-implementation-pattern) or server-side date conversion (see backend-hebrew-calendar-pattern)."
---

# Client ControlValueAccessor Form Control Pattern

A shared custom form control is a **standalone** component in `client/src/app/components/<name>/` that implements `ControlValueAccessor`, so parent forms bind it with plain `formControlName="..."` — the parent never knows about the internal widgets.

## When to Use

- Building `hebrew-date-picker` (three `p-dropdown`s → one `{ hebrewYear, hebrewMonth, hebrewDay } | null` value).
- Wrapping any multi-widget input (range, composite ID, etc.) as a single form value.
- Reviewing an existing CVA that misbehaves with `patchValue`/`setValue` or disabled state.

## The Non-Negotiable Wiring

```typescript
import { Component, forwardRef } from "@angular/core";
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from "@angular/forms";

@Component({
  selector: "app-hebrew-date-picker",
  standalone: true,
  imports: [CommonModule, FormsModule, DropdownModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => HebrewDatePickerComponent),
      multi: true, // ← required; omitting it breaks ALL other CVAs on the page
    },
  ],
  template: `...`,
})
export class HebrewDatePickerComponent implements ControlValueAccessor {
  private onChange: (value: HebrewDateValue | null) => void = () => {};
  private onTouched: () => void = () => {};
  disabled = false;

  writeValue(value: HebrewDateValue | null): void {
    // Called by forms API (setValue/patchValue/init). NEVER call onChange from here —
    // that causes valueChanges loops. Just update internal state.
  }
  registerOnChange(fn: (value: HebrewDateValue | null) => void): void {
    this.onChange = fn;
  }
  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }
  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}
```

The value type is an exported interface (see model rules in client.instructions.md):

```typescript
export interface HebrewDateValue {
  hebrewYear: number;
  hebrewMonth: number;
  hebrewDay: number;
}
```

## Workflow

1. Create the component folder under `client/src/app/components/<kebab-name>/`, standalone, explicit `imports`.
2. Add the `NG_VALUE_ACCESSOR` provider with `forwardRef` + `multi: true` (exactly as above).
3. Implement the 4 CVA methods. `writeValue` must tolerate `null` (reset/clear) and a full value (edit-mode patch).
4. Internal widgets (PrimeNG `p-dropdown`s) use `[(ngModel)]` + `(onChange)` internally — that's fine inside a CVA; the _parent_ uses reactive `formControlName`.
5. On every internal change: recompute the composite value, call `this.onChange(valueOrNull)` and `this.onTouched()`. Emit `null` while incomplete, the full object when all parts are set.
6. **Dependent options:** when one dropdown invalidates another (e.g. year change removes אדר ב׳ in a non-leap year), reset the invalid part AND re-emit via `onChange` — otherwise the form keeps a stale value.
7. Parent usage — no extra glue:

```html
<app-hebrew-date-picker formControlName="lessonDate"></app-hebrew-date-picker>
```

```typescript
lessonDate: [initialValue as HebrewDateValue | null, Validators.required];
```

## Hebrew-Date-Picker Specifics

- Month options depend on the year: 13 months when `(7*y + 1) % 19 < 7` (אדר א׳=6, אדר ב׳=7), else 12 (אדר=6) — numbering must match .NET `HebrewCalendar` (see [backend-hebrew-calendar-pattern](../backend-hebrew-calendar-pattern/SKILL.md)).
- "Today" default: use `Intl.DateTimeFormat('he-u-ca-hebrew')` `.formatToParts()` and map the month **name** (not Intl's number) to the component's own arrays — Intl and .NET number months differently.
- Day options א׳–ל׳ (1–30) always; impossible combinations (day 30 in a 29-day month) are rejected server-side.
- RTL: the app is RTL globally; order the dropdowns day→month→year in the template and verify at 360px width.

## Pitfalls

- **Forgetting `multi: true`** — silently replaces the entire accessor collection; other inputs stop working.
- **Calling `onChange` inside `writeValue`** — infinite `valueChanges` loops. `writeValue` only sets internal state.
- **Emitting a partial object** — emit `null` until all parts are chosen so `Validators.required` behaves.
- **Not handling `null` in `writeValue`** — `form.reset()` will crash or leave stale dropdowns.
- **Skipping `onTouched`** — error styles that rely on `touched` never appear; call it on first user interaction.
- **`ChangeDetectionStrategy.OnPush` + external `setValue`** — if you use OnPush, inject `ChangeDetectorRef` and `markForCheck()` inside `writeValue`.

## See Also

- [backend-hebrew-calendar-pattern](../backend-hebrew-calendar-pattern/SKILL.md) — server-side conversion/validation this control's payload feeds into.
- [client-flow-fix-implementation-pattern](../client-flow-fix-implementation-pattern/SKILL.md) — inline validation styling on required fields.
