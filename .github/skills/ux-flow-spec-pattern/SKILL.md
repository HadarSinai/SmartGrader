---
name: ux-flow-spec-pattern
description: "Use when writing or reviewing docs/ux/{feature}-flow.md for the SmartGrader client: the improved screen flow (entry point, numbered steps, exit points) plus design principles and accessibility, compliant with client.instructions.md conventions (PrimeNG ConfirmationService/MessageService, ApiErrorInterceptor, routing pattern). USE FOR: 'write a flow spec for X', 'design the improved create/delete flow', 'figma-ready flow description'. NOT for the Job Statement/persona doc (see ux-jtbd-analysis-pattern) or the stage-by-stage journey map (see ux-journey-map-pattern)."
---

# UX Flow Spec Pattern

Every SmartGrader feature's flow spec lives at `docs/ux/{feature}-flow.md`. It turns each "Opportunity"
from the paired `{feature}-journey.md` into a concrete, implementable flow — but every fix must stay
inside the conventions already mandated by
[client.instructions.md](../../instructions/client.instructions.md): PrimeNG `ConfirmationService` for
deletes (never `window.confirm`), `MessageService` toasts, `ApiErrorInterceptor` for error mapping, and
the existing nested-route pattern. This is documentation only — no backend changes and no actual Angular
code are produced by this skill.

## When to Use

- Writing the flow spec for an existing screen once its jtbd + journey docs exist.
- Writing the flow spec for a brand-new screen (e.g. LessonResults), including new route/component
  names that follow the existing nesting convention.
- Reviewing an existing `{feature}-flow.md` for a proposed fix that violates a `client.instructions.md`
  rule (e.g. suggests `window.confirm`, a hardcoded URL, or a `Promise`-based service call).

## Workflow

1. **Read [client.instructions.md](../../instructions/client.instructions.md) first** — every proposed
   fix must stay within its rules (PrimeNG modules only, `ConfirmationService`/`MessageService`,
   `ApiClient.url(...)`, `Observable<T>` services, the `/parent/:id/children` routing convention).
2. **Read the paired `docs/ux/{feature}-journey.md`** — each `**[Fix]**` step in the flow spec should
   trace back to one of its "Opportunity" bullets; don't introduce fixes with no journey-map origin.
3. **Structure one `## User Flow: {Action}` section per action** (e.g. Create, Delete — add more only if
   the journey map identified issues there too):
   - **Entry Point**: the exact route/button that starts the flow.
   - **Flow Steps**: numbered, referencing the real component/API call (e.g. `POST /api/lessons`); mark
     every changed/new step with **`[Fix]`**, and describe the exact replacement copy/behavior.
   - **Exit Points**: success and cancel/error outcomes.
4. **Add "Design Principles for This Flow"**: 3-5 numbered, general principles distilled from the fixes
   (e.g. "language consistency", "explain, don't just disable", "confirm destructive actions
   consistently").
5. **Add "Accessibility"**: reference
   [docs/ux/accessibility-checklist.md](../../../docs/ux/accessibility-checklist.md) and call out only
   the feature-specific deltas (e.g. an `aria-label` that must change in lockstep with a copy fix) —
   don't repeat the full checklist.
6. **Flag backend dependencies explicitly** if a fix would require a backend change that's out of scope
   or not yet available (e.g. a commented-out endpoint) — mark it BLOCKED/TODO, don't silently design
   around it as if it were live.
7. **Optionally add a "Codebase Hygiene Note"** for non-UX findings (e.g. a legacy duplicate component)
   — flag only, don't fix it as part of this doc.

## Real Example

[`docs/ux/lessons-flow.md`](../../../docs/ux/lessons-flow.md):

```markdown
## User Flow: Delete Lesson

**Entry Point**: Trash icon on a lessons-list row (desktop table or mobile card).

**Flow Steps**:

1. Click trash icon → `ConfirmationService.confirm()`.
2. **[Fix]** Replace hardcoded feminine-gendered copy with gender-neutral phrasing:
   - `message`: `"בטוחה שברצונך למחוק את..."` → `"האם למחוק את "{{lesson.name}}"?  לא ניתן לשחזר פעולה זו."`
   - `acceptLabel`: `"מחקי"` → `"מחיקה"`
3. On accept → `DELETE /api/lessons/{id}` → Hebrew success toast → reload list.

**Exit Points**:

- Success: row removed from list, list reloaded.
- Error: dialog closes, list unchanged, error toast shown.
```

Note the flow still uses `ConfirmationService` (per `client.instructions.md`) — the fix only changes
copy, never the mechanism.

## Template

Copy-paste starting point: [assets/flow.template.md](./assets/flow.template.md)

## Pitfalls

- Don't propose anything that violates `client.instructions.md` — no `window.confirm`, no hardcoded API
  URLs, no `class`-based models, no `Promise`-based service methods.
- Don't design a flow around a backend endpoint that's disabled/commented out as if it were live — flag
  it as a dependency instead (see the LessonResults "Complete Lesson" example, blocked on
  `POST /api/lesson-results/complete`).
- Don't repeat the whole accessibility checklist — reference it and list only the deltas.
- Don't fix codebase hygiene issues (dead components) inline — note them separately and leave them for
  a follow-up cleanup task.
- Don't skip the journey-map trace — every `[Fix]` should map to a real "Opportunity" bullet, not a new
  idea invented at flow-spec time.

## See Also

- [ux-journey-map-pattern](../ux-journey-map-pattern/SKILL.md) — the "Opportunity" bullets this flow
  spec turns into concrete steps.
- [ux-jtbd-analysis-pattern](../ux-jtbd-analysis-pattern/SKILL.md) — the persona and pain points this
  whole artifact chain traces back to.
