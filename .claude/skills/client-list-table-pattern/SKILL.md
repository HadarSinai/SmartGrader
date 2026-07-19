---
name: client-list-table-pattern
description: "Use when applying the unified SmartGrader list/table pattern to any Angular list page (Students, Lessons, Assignments, Submissions, LessonResults): multi-select checkbox column + selection toolbar (design-only, no real bulk delete), actions overflow menu (⋯ with edit/delete) with view-submissions as a separate icon, UX-correct toolbar button placement, RTL-correct paginator, unified date format, and filter panel styling. USE FOR: 'add multi-select to the X table', 'apply the list table pattern', 'convert the actions column to an overflow menu', 'fix the paginator/toolbar layout on a list page'. NOT for defining design tokens (see client-design-token-rollout-pattern) or copy/validation fixes (see client-flow-fix-implementation-pattern)."
---

# Client List/Table Pattern

The unified table pattern for all SmartGrader list pages, per the "Warm Minimal" redesign plan
([docs/ux/redesign-plan.md](../../../docs/ux/redesign-plan.md), Phase 2). Every list screen
(Students, Lessons, Assignments, Submissions, LessonResults) must be an instance of this one
pattern — no per-page variations.

## When to Use

- Adding multi-select (checkbox column + selection toolbar) to a list page.
- Converting a dense actions column (3+ icon buttons) into an overflow menu (⋯).
- Fixing toolbar layout: primary action placement, search + filter row, export button.
- Fixing paginator arrows/styling in this RTL Hebrew app.
- Reviewing a list-page PR for consistency with the unified pattern.

## The Pattern (target anatomy of every list page)

```
┌─ sg-page ─────────────────────────────────────────────┐
│ Page header: title + subtitle | [+ חדש] (primary,     │
│   top-start = right side in RTL) | breadcrumb         │
│ Toolbar row: [search 🔍] [filters] ..... [ייצוא]      │
│ Selection toolbar (visible only when rows selected):  │
│   "נבחרו N" · [מחיקת נבחרים] · [ביטול בחירה]          │
│ p-table:                                              │
│   [☑] col | data cols | [👁 view] col | [⋯ menu] col  │
│ Paginator: RTL-correct first/prev/next/last           │
└───────────────────────────────────────────────────────┘
```

## Workflow

1. **Multi-select (DESIGN ONLY — no real deletion)**
   - Use PrimeNG selection: `[(selection)]="selectedItems"` + `<p-tableHeaderCheckbox>` in header and
     `<p-tableCheckbox [value]="row">` per row, `dataKey="id"`.
   - Add a selection toolbar above the table, shown only when `selectedItems.length > 0`:
     count label ("נבחרו {{selectedItems.length}}"), a "מחיקת נבחרים" button, and "ביטול בחירה".
   - **The bulk-delete button must NOT delete.** Per the plan decision, it shows a MessageService
     toast (severity `info`, Hebrew, e.g. "מחיקה מרובה תהיה זמינה בקרוב") — real logic is a future task.
2. **Actions column → overflow menu**
   - Replace inline edit/delete icon buttons with a single ⋯ trigger opening a `p-menu`
     (`[popup]="true"`, `appendTo="body"`): items עריכה (pencil) and מחיקה (trash, styled danger).
   - Keep the existing single-item delete behavior (ConfirmationService confirm + Hebrew copy) — it
     moves into the menu item's command, unchanged.
   - **"צפייה בהגשות" / view-details stays OUTSIDE the menu** as its own dedicated icon column
     (eye icon, with `pTooltip` in Hebrew and `aria-label`).
3. **Toolbar placement (UX-correct, consistent on every page)**
   - Primary action (+ חדש/חדשה) in the page header, top-start (visually right in RTL).
   - Search input + filter controls on one row directly above the table.
   - Export (ייצוא) as a secondary button near the table, not next to the primary action.
   - Back-navigation links (e.g. "חזרה לסטודנטים") in the breadcrumb slot, not mixed into the toolbar.
4. **Paginator (RTL)**
   - Style globally in `client/src/styles.css` (not per component).
   - Verify arrow semantics in RTL: "next" must point visually left, "first/last" chevrons flipped.
     Test by clicking through pages — position in RTL reading order must feel natural.
   - Rows-per-page dropdown aligned with the paginator, using existing tokens.
5. **Cells**
   - Dates: unified format via the Hebrew locale date pipe (`dd.MM.yy HH:mm`), no "AM/PM", no
     calendar icon in the cell.
   - Status tags: use the semantic status vars (`--status-success/warn/error/info`) + icon, never raw
     PrimeNG red/blue severities.
   - No inline code previews or multi-line content in rows — details belong on the detail page.
6. **Filter panel ("מיון לפי")** — consistent spacing with `--space-*` tokens, aligned labels above
   controls, styled reset button (link-style, start-aligned).

## Verification (per touched page)

- RTL sanity: checkbox column at the start (right), actions at the end (left), paginator arrows correct.
- 360 / 768 / 1280 px: mobile-card fallback still works; selection toolbar wraps gracefully.
- Keyboard: checkboxes and the ⋯ menu reachable by Tab, menu opens with Enter, closes with Escape.
- Screen reader: ⋯ trigger has Hebrew `aria-label` (e.g. "פעולות נוספות"); selection count announced.
- `ng build` succeeds; no console errors.

## What NOT to Do

- Do not wire real bulk deletion (no forkJoin of DELETEs, no server calls) — design only, per plan.
- Do not leave edit/delete as inline icons on some pages and a menu on others — all 5 list pages
  must match.
- Do not put "צפייה בהגשות"/view inside the ⋯ menu — it is the row's main drill-down and stays visible.
- Do not add per-component paginator CSS — fix once globally in `styles.css`.
- Do not introduce new colors/classes; use existing tokens (see
  [client-design-token-rollout-pattern](../client-design-token-rollout-pattern/SKILL.md)).

## See Also

- [docs/ux/redesign-plan.md](../../../docs/ux/redesign-plan.md) — Phase 2 definition + decisions.
- [client-design-token-rollout-pattern](../client-design-token-rollout-pattern/SKILL.md) — tokens/visual rollout.
- [client-flow-fix-implementation-pattern](../client-flow-fix-implementation-pattern/SKILL.md) — copy/validation fixes.
