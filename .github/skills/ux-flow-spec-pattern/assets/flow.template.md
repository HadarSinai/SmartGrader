# {Feature} — Flow Specification

**Scope**: Create/Delete UX improvements for the {existing | brand-new} {Feature} feature. {No new
routes, no backend changes — all fixes are copy/validation/consistency improvements within
[{feature}-list.component.ts](../../../client/src/app/pages/{feature}/{feature}-list.component.ts) and
[{feature}-form.component.ts](../../../client/src/app/pages/{feature}/{feature}-form.component.ts). |
New screens/components proposed below, following the existing nested-route convention.}

## User Flow: Create {Feature Singular}

**Entry Point**: `/{feature}` → "{button label}" button → `/{feature}/new`.

**Flow Steps**:

1. {step}
2. **[Fix]** {step, with exact replacement copy/behavior}
3. Clicks "{submit label}" → `POST /api/{feature}`.

**Exit Points**:

- Success → `/{feature}`.
- Cancel → {behavior, e.g. **[Fix]** confirm via `ConfirmationService` if the form is dirty}.

## User Flow: Delete {Feature Singular}

**Entry Point**: Trash icon on a {feature}-list row.

**Flow Steps**:

1. Click trash icon → `ConfirmationService.confirm()`.
2. **[Fix]** {copy fix, exact before → after strings}
3. On accept → `DELETE /api/{feature}/{id}` → success toast → reload list.

**Exit Points**:

- Success: row removed from list, list reloaded.
- Error: dialog closes, list unchanged, error toast shown.

## Design Principles for This Flow

1. {principle}
2. {principle}
3. {principle}

## Codebase Hygiene Note (non-UX, flag only)

{Optional: legacy/dead component or similar finding — flag only, don't fix here.}

## Accessibility

See [accessibility-checklist.md](../../accessibility-checklist.md). Specific to this flow:
{feature-specific deltas only}.
