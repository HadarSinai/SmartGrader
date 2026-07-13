---
name: client-design-token-rollout-pattern
description: "Use when extending the Students-list pilot's design tokens (--radius-*, --shadow-*, --space-*, --text-*, sg-* classes) from client/spec.md to another SmartGrader list/form page: what to check (RTL, 360/768/1280px breakpoints, no new parallel colors) and what NOT to do (no new ad-hoc classes/colors). USE FOR: 'roll out the design tokens to X page', 'make the Assignments/Lessons/Submissions page match the Students pilot styling', 'apply spec.md tokens'. NOT for functional/copy fixes (see client-flow-fix-implementation-pattern) or defining the tokens themselves (already defined in src/styles.css per the pilot)."
---

# Client Design Token Rollout Pattern

Extends the design-token work already piloted on the Students list (per
[client/spec.md](../../../client/spec.md)) to another screen, without introducing a second, parallel
design system.

## When to Use

- A list or form page (Lessons, Assignments, Submissions, or a new page) still uses ad-hoc
  spacing/colors/shadows instead of the `sg-*` classes and `--radius-*`/`--shadow-*`/`--space-*`/
  `--text-*` tokens already standardized on `students-list.component`.
- Reviewing a PR that touches page-level styles, to confirm it didn't introduce new parallel tokens.
- Rolling out the pilot's card/button/table/toolbar/toast conventions one screen at a time.

## Workflow

1. **Re-read the pilot spec**: [client/spec.md](../../../client/spec.md) — constraints are locked
   (PrimeNG + PrimeFlex only, no feature/flow changes, RTL/Hebrew must stay correct, don't touch
   services/API).
2. **Inventory existing tokens** in `client/src/styles.css`: `--radius-sm/md/lg`, `--shadow-sm/md`,
   `--space-1/2/3/4/6`, `--text-sm/base/lg/xl`, and the `sg-*` classes (`sg-page`, `sg-card`,
   `sg-form-card`, `sg-title`, `sg-h1`, `sg-h2`, etc.) — these must already exist from the Students
   pilot. If a token you need doesn't exist yet, check whether it can be **derived** from an existing
   `--app-*`/theme variable before adding anything new (per the mapping rule in `spec.md`).
3. **Diff against the pilot**: open `students-list.component` (list) and/or its form counterpart side by
   side with the target page. Identify every place the target page uses a raw pixel value, an inline
   color, or a bespoke class where the pilot uses a token/`sg-*` class instead.
4. **Apply, don't invent**: replace ad-hoc styles with the existing token/class. Examples already seen
   in the codebase: `sg-page` wrapping the section, `p-card` with `styleClass="sg-card sg-form-card"`,
   title blocks using `sg-title` / `sg-h1` / `sg-h2` (see
   [assignment-form.component.ts](../../../client/src/app/pages/assignments/assignment-form.component.ts)
   header template for a reference implementation already following this convention).
5. **Component-level styles only if needed**: per `spec.md`, prefer fixing things globally in
   `src/styles.css`; only add scoped component-level CSS for a layout issue that's genuinely specific to
   that one page.
6. **Verify the checklist from spec.md** on the touched page:
   - RTL sanity: icons, paddings, and action columns still align correctly (this is a Hebrew RTL app).
   - Responsive sanity at 360px / 768px / 1280px — no broken layout.
   - Empty/loading states for tables follow the same pattern as the pilot (`emptymessage`, `[loading]`).
   - No console errors introduced.
   - `ng build` succeeds.

## What NOT to Do

- Do not introduce new ad-hoc CSS classes or hardcoded colors/spacing that duplicate what a token
  already covers — this creates a second, parallel design system.
- Do not add a new `--radius-*`/`--shadow-*`/`--space-*`/`--text-*` token unless it truly cannot be
  derived from an existing token or `--app-*`/theme variable.
- Do not change any feature behavior, route, or API call while doing a visual rollout — this is a
  styling-only pass (per the locked constraints in `spec.md`).
- Do not replace PrimeNG components with custom markup — keep using `p-card`, `p-table`, `p-button`,
  etc.; only the classes/tokens applied to them change.
- Do not skip the RTL check — a token that looks right in an LTR mental model can still break icon/
  padding alignment in this app's RTL layout.

## Real Examples

Reference "already-correct" usage to copy from (not students-list, since that's the pilot itself, but a
page that has already adopted the same header/card convention):
[assignment-form.component.ts](../../../client/src/app/pages/assignments/assignment-form.component.ts) —
`class="sg-page"` wrapper, `p-card styleClass="sg-card sg-form-card"`, and `sg-title`/`sg-h1`/`sg-h2`
inside the `pTemplate="header"` block.

## See Also

- [client/spec.md](../../../client/spec.md) — the authoritative source for tokens, constraints, and the
  verification checklist.
- [client-flow-fix-implementation-pattern](../client-flow-fix-implementation-pattern/SKILL.md) — the
  sibling skill for copy/validation fixes (functional, not visual).
