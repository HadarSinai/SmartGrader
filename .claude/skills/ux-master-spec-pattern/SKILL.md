---
name: ux-master-spec-pattern
description: "Use when writing or reviewing the SmartGrader master UX spec (docs/ux/master-spec.md): the system-wide אפיון-על that defines the design vision (Warm Minimal), sitemap (teacher area / student area / login), the three mother-templates (list/form/detail), shared patterns (empty/loading/error states, toasts, delete confirmations, date format, semantic status colors), and future-feature specs (notifications bell, auth). USE FOR: 'write the master spec', 'create the אפיון-על', 'define the system-wide design language doc', 'spec the student area and login screen'. NOT for per-feature JTBD/journey/flow docs (see ux-jtbd-analysis-pattern / ux-journey-map-pattern / ux-flow-spec-pattern) or implementing styles (see client-design-token-rollout-pattern)."
---

# UX Master Spec Pattern

How to write `docs/ux/master-spec.md` — the single source of truth (אפיון-על) for the whole
SmartGrader client, sitting ABOVE the per-feature docs. Per-feature docs (jtbd/journey/flow) answer
"how does screen X work"; the master spec answers "what makes all screens one system".

## When to Use

- Creating or rewriting `docs/ux/master-spec.md` per the redesign plan
  ([docs/ux/redesign-plan.md](../../../docs/ux/redesign-plan.md), Phase 0).
- Adding a new system-wide pattern (e.g. a notifications spec, a new shared state) to the master spec.
- Reviewing whether a proposed screen/feature fits the existing mother-templates or genuinely needs
  a new pattern.

## Required Document Structure

Write in Hebrew (the product language), RTL-aware. Sections, in order:

1. **מטרת המוצר ופרסונות** — one paragraph on the product's job; link to
   [personas.md](../../../docs/ux/personas.md), don't duplicate it.
2. **החזון העיצובי — "מינימליזם חם"** — copy the locked principles from the redesign plan:
   existing warm palette (`#f3efe7` bg, `#fbfaf8` surfaces, `#8a6a54` accent, Rubik font) stays;
   3 elevation levels only (flat → card → overlay); 8pt grid; one typographic scale
   (display/title/body/caption).
3. **מפת מסכים (Sitemap)** — a mermaid or nested-list map of three areas:
   - **אזור מורה**: Dashboard → Students → Submissions; Lessons → Assignments → Submissions;
     LessonResults. Note breadcrumb chain per drill-down.
   - **אזור תלמידה — "המסע שלי"** (spec+design only, no auth implementation in this task):
     my lessons → assignment → submit code → AI feedback + grade; my submissions + grades.
   - **מסך כניסה** (spec+design only).
4. **שלוש תבניות-האם** — define each once; every screen must declare which template it instantiates:
   - **תבנית רשימה** — defer details to
     [client-list-table-pattern](../client-list-table-pattern/SKILL.md); the master spec only states
     the anatomy (header / toolbar / selection bar / table / paginator).
   - **תבנית טופס** — compact fields (~38px), thin `--app-border` borders, soft focus ring, inline
     validation, required-field indicators, Hebrew placeholders, שמירה/ביטול placement.
   - **תבנית פירוט** — page header + one unified status area (a single slot where
     success/error/AI-feedback/comments render consistently), key-value grid, code block styling.
5. **דפוסים משותפים** — one short subsection each, with exact copy where relevant:
   - מצבי ריק/טעינה/שגיאה (empty state icon + Hebrew CTA; skeleton loading; errors via ApiErrorInterceptor toast).
   - טוסטים (MessageService, Hebrew, gender-neutral copy) ואישורי מחיקה (ConfirmationService, Hebrew).
   - פורמט תאריך אחיד: `dd.MM.yy HH:mm` (e.g. `13.07.26 10:33`), Hebrew locale, no AM/PM, no calendar icon.
   - סמנטיקת סטטוסים: `--status-success` (מרווה), `--status-warn` (ענבר), `--status-error`
     (טרקוטה עמומה), `--status-info` — always icon + color, never color alone.
6. **נגישות** — reference [accessibility-checklist.md](../../../docs/ux/accessibility-checklist.md);
   state the non-negotiables: keyboard nav, Hebrew aria-labels, 4.5:1 contrast, RTL correctness.
7. **פיצ'רים עתידיים (מאופיינים, לא ממומשים)** — explicitly scoped OUT of current implementation:
   - התראות: הפעמון יציג הגשות שסיימו בדיקת AI; עד אז — ללא מונה מזויף.
   - התחברות ותפקידים (מורה/תלמידה): flow sketch only.
   - מחיקה מרובה בפועל (server bulk endpoint): UI exists design-only.

## Workflow

1. Read [docs/ux/redesign-plan.md](../../../docs/ux/redesign-plan.md) — the vision and decisions are
   locked there; the master spec elaborates, it does not re-decide.
2. Read the existing per-feature docs in `docs/ux/` and [client/spec.md](../../../client/spec.md);
   extract what is already shared (tokens, sg-* conventions) instead of inventing new rules.
3. Draft the document per the structure above; keep it under ~300 lines — link to per-feature docs
   and skills rather than duplicating them.
4. For the student area and login screen, include a Figma-ready flow description per
   [ux-flow-spec-pattern](../ux-flow-spec-pattern/SKILL.md) (entry point → numbered steps → exit
   points) — design/spec only, no implementation steps.
5. Cross-check: every rule in the master spec must be either implemented, in the redesign plan's
   phases, or explicitly listed under "פיצ'רים עתידיים". No orphan rules.

## What NOT to Do

- Do not duplicate per-feature JTBD/journey/flow content — link to it.
- Do not introduce design decisions that contradict the redesign plan (palette, Rubik, 3 elevations).
- Do not spec implementation details of auth/notifications beyond flow + UI — they are future tasks.
- Do not write in English — the product and its docs are Hebrew/RTL.

## See Also

- [docs/ux/redesign-plan.md](../../../docs/ux/redesign-plan.md) — the source of decisions.
- [ux-flow-spec-pattern](../ux-flow-spec-pattern/SKILL.md), [ux-jtbd-analysis-pattern](../ux-jtbd-analysis-pattern/SKILL.md),
  [ux-journey-map-pattern](../ux-journey-map-pattern/SKILL.md) — per-feature docs.
- [client-list-table-pattern](../client-list-table-pattern/SKILL.md) — the list mother-template details.
