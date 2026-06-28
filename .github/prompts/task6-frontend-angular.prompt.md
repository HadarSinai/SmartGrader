---
description: "Task 6 — Frontend: Add MethodName field to Assignment form and display CompilationFailed status in Angular"
agent: agent
tools:
  [
    read_file,
    replace_string_in_file,
    multi_replace_string_in_file,
    get_errors,
    file_search,
    semantic_search,
  ]
---

# Task 6 — Frontend: Angular — MethodName + CompilationFailed

Follow the rules in [client.instructions.md](../instructions/client.instructions.md).

**Depends on:** Task 1 (MethodName and CompilationFailed must exist in backend DTOs)

**Can run in parallel** with Tasks 2–5.

---

## Step 1 — Update Assignment model

File: `client/src/app/models/` — find the `Assignment` interface (search for `AssignmentResponseDto` or similar)

Add `methodName` field:

```typescript
export interface AssignmentResponseDto {
  // ...existing fields...
  methodName: string;
}
```

Also update `CreateAssignmentRequestDto` / `UpdateAssignmentRequestDto` interfaces:

```typescript
methodName: string;
```

---

## Step 2 — Update Submission model

File: `client/src/app/models/` — find the `Submission` interface

Add `compileError` field:

```typescript
export interface SubmissionResponseDto {
  // ...existing fields...
  compileError: string | null;
}
```

Also update the `SubmissionStatus` enum/type if it exists in the models:

```typescript
export type SubmissionStatus =
  | "PendingAi"
  | "ProcessingAi"
  | "Done"
  | "AiFailed"
  | "CompilationFailed"; // ← add this
```

---

## Step 3 — Add MethodName input to Assignment form

Find the Assignment create/edit component in `client/src/app/pages/`.
Search for the form that contains `title` or `description` fields for assignments.

Add a `MethodName` input field using PrimeNG `InputTextModule`:

```html
<div class="field">
  <label for="methodName">Method Name</label>
  <input
    pInputText
    id="methodName"
    [(ngModel)]="form.methodName"
    placeholder="e.g. Sum"
    required
  />
  <small class="p-error" *ngIf="submitted && !form.methodName">
    Method name is required
  </small>
</div>
```

Include validation — `methodName` is **required** (without it, Judge0 cannot run tests).

---

## Step 4 — Display CompilationFailed in submission view

Find the component that displays submission status (search for `PendingAi`, `Done`, `AiFailed` in templates).

Add handling for `CompilationFailed`:

### Status badge (PrimeNG Tag):

```html
<p-tag
  *ngSwitchCase="'CompilationFailed'"
  severity="danger"
  value="Compile Error"
  icon="pi pi-times-circle"
/>
```

### Compile error details (show below status when CompilationFailed):

```html
<div
  *ngIf="submission.status === 'CompilationFailed' && submission.compileError"
  class="compile-error-box"
>
  <strong>Compile Error:</strong>
  <pre>{{ submission.compileError }}</pre>
</div>
```

Style the `.compile-error-box` with a red border and monospace font to make errors readable.

---

## Step 5 — Include methodName in service calls

Find the Assignment service in `client/src/app/services/`.

Ensure `createAssignment()` and `updateAssignment()` include `methodName` in the request body.

---

## Rules

- All components use `standalone: true` — add `InputTextModule`, `NgIf`, `NgSwitch` to `imports: []` as needed
- Use `string | null` for nullable fields, never `string | undefined`
- Match backend DTO field names exactly (camelCase)
- Do not add `NgModule` — this project uses standalone components only

---

## Validation

- Assignment form shows `methodName` input with required validation
- Submitting assignment without `methodName` shows error
- Submission list/detail shows "Compile Error" badge for `CompilationFailed` status
- Compile error message is displayed in a readable format
- `ng build` passes with no errors
