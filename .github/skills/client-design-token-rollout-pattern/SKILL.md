---
name: client-design-token-rollout-pattern
description: "Use when styling a new or existing SmartGrader list/form page against the design system in docs/design-system.md: the --radius-*/--shadow-*/--space-*/--text-* tokens and sg-* classes, what to check (RTL, 360/768/1280px breakpoints, no parallel colours, no var(--token, fallback)) and what NOT to do (no ad-hoc classes or colours, no rule a component redefines from styles.css). USE FOR: 'style a new page', 'make this page match the rest of the app', 'review a PR that touches page styles', 'apply the design tokens'. NOT for functional/copy fixes (see client-flow-fix-implementation-pattern) or for introducing a new token (that is one commit touching docs/design-system.md and styles.css together, not a component)."
---

# Client Design Token Rollout Pattern

Styles a screen against the one design system, without introducing a second, parallel one. The
contract is [docs/design-system.md](../../../docs/design-system.md); the tokens themselves are defined
once in `client/src/styles.css`.

⚠️ **This is no longer a rollout.** Phase A6 converted every file, and `DesignTokenTests` now fails on
a literal colour anywhere under `client/src/app` — `#hex`, `rgb()`, `rgba()`, `hsl()` alike — on a
`var(--token, fallback)` pair, and on a component that redefines a base class selector the global
sheet already defines. What follows is how to satisfy those guards, not how to migrate towards them.

## When to Use

- Styling a new page, or a page that still uses ad-hoc spacing/colours/shadows instead of the `sg-*`
  classes and the `--radius-*`/`--shadow-*`/`--space-*`/`--text-*` tokens.
- Reviewing a PR that touches page-level styles, to confirm it didn't introduce new parallel tokens.
- Working out where a rule belongs: `styles.css` if two screens need it, the component if one does.

## Workflow

1. **Re-read the contract**: [docs/design-system.md](../../../docs/design-system.md) — the three
   mother templates, the token table, the seven status presentations, and the `D-N` accessibility
   requirements. Constraints are locked: PrimeNG + PrimeFlex only, no feature/flow changes, RTL/Hebrew
   must stay correct, don't touch services/API.
2. **Inventory existing tokens** in `client/src/styles.css`: `--radius-sm/md/lg`, `--shadow-sm/md`,
   `--space-1/2/3/4/6`, `--text-sm/base/lg/xl`, and the `sg-*` classes (`sg-page`, `sg-card`,
   `sg-form-card`, `sg-title`, `sg-h1`, `sg-h2`, etc.). If a token you need doesn't exist yet, check
   whether it can be **derived** from an existing `--app-*`/theme variable before adding anything new
   — and a genuinely new token is one commit touching `docs/design-system.md` and `styles.css`
   together, never a value written into a component.
3. **Diff against a page that already conforms**: open one and put it beside the target. Identify every
   place the target uses a raw pixel value, a literal colour, or a bespoke class where the conforming
   page uses a token or an `sg-*` class.
4. **Apply, don't invent**: replace ad-hoc styles with the existing token/class. Examples already seen
   in the codebase: `sg-page` wrapping the section, `p-card` with `styleClass="sg-card sg-form-card"`,
   title blocks using `sg-title` / `sg-h1` / `sg-h2` (see
   [assignment-form.component.html](../../../client/src/app/pages/assignments/assignment-form.component.html)
   header template for a reference implementation already following this convention).
5. **Component-level styles only when the rule is genuinely local.** A rule two screens need belongs in
   `src/styles.css` — and `DesignTokenTests` fails a component that redefines a base class selector the
   global sheet already defines, so this is enforced rather than advised.
6. **Verify on the touched page:**
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
- Do not change any feature behavior, route, or API call while doing a styling pass — a visual change
  and a behavioural change in one commit are two changes nobody can review.
- Do not replace PrimeNG components with custom markup — keep using `p-card`, `p-table`, `p-button`,
  etc.; only the classes/tokens applied to them change.
- Do not skip the RTL check — a token that looks right in an LTR mental model can still break icon/
  padding alignment in this app's RTL layout.

## Real Examples

Reference usage to copy from:
[assignment-form.component.html](../../../client/src/app/pages/assignments/assignment-form.component.html) —
`class="sg-page"` wrapper, `p-card styleClass="sg-card sg-form-card"`, and `sg-title`/`sg-h1`/`sg-h2`
inside the `pTemplate="header"` block.

**Every component is three files** — `x.component.ts`, `x.component.html`, `x.component.css` — and a
test enforces it. Styling work therefore lands in the `.css` file, never in a string inside the `.ts`.

## See Also

- [docs/design-system.md](../../../docs/design-system.md) — the authoritative source for tokens,
  the three mother templates, the `D-N` accessibility requirements, and what is machine-verified.
- [client-flow-fix-implementation-pattern](../client-flow-fix-implementation-pattern/SKILL.md) — the
  sibling skill for copy/validation fixes (functional, not visual).
