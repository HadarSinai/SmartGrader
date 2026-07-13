---
name: ux-jtbd-analysis-pattern
description: "Use when writing or reviewing docs/ux/{feature}-jtbd.md for the SmartGrader client: Job Statement, persona, context, and 'Current Solution & Pain Points' grounded in an actual read of the existing component (not assumptions). USE FOR: 'write a jtbd for X', 'jobs-to-be-done analysis for the Lessons/Students/Assignments/Submissions screen', 'why does this screen exist'. NOT for journey-stage mapping (see ux-journey-map-pattern) or the improved flow/design spec (see ux-flow-spec-pattern)."
---

# UX JTBD Analysis Pattern

Every SmartGrader feature's Jobs-to-be-Done doc lives at `docs/ux/{feature}-jtbd.md` and is grounded in
a real read of the current Angular component(s) for that feature — never in assumed/generic pain
points. The rest of the UX artifact set (`{feature}-journey.md`, `{feature}-flow.md`) builds on top of
this file, so its persona and pain-point list must be accurate.

## When to Use

- Writing the JTBD doc for an existing screen (e.g. auditing Lessons, Students, Assignments, or
  Submissions for Create/Delete UX gaps).
- Writing the JTBD doc for a brand-new, not-yet-built screen (e.g. LessonResults) where there is no
  client component to read yet — ground it in the backend contract instead.
- Reviewing an existing `{feature}-jtbd.md` for whether its pain points still match the current code.

## Workflow

1. **Read [docs/ux/personas.md](../../../docs/ux/personas.md) first** to pick the correct persona
   (Teacher, Student, or both) — state it explicitly at the top of the doc.
2. **Read the real component(s) before writing anything.** For an existing feature, open both
   `client/src/app/pages/{feature}/{feature}-list.component.ts` and
   `client/src/app/pages/{feature}/{feature}-form.component.ts` (plus any detail component, e.g.
   `submission-detail.component.ts`). For a brand-new feature with no client code yet, read the backend
   controller/DTOs/entity instead (there is nothing to audit client-side — say so explicitly).
3. **Write the Job Statement** using the template: `When [situation], I want to [motivation], so I can
   [outcome].` One sentence, tied to the persona's real goal, not a feature request.
4. **Write the Context** section: `Situation` / `Motivation` / `Outcome` bullets.
5. **Write "Current Solution & Pain Points (grounded in current code)"**: link to the actual component
   file(s) with markdown links, and cite concrete, specific findings — hardcoded strings, missing
   validation, gendered/inconsistent copy, dead/legacy components sitting alongside the real one, etc.
   Every bullet must be traceable to something you actually read, not a generic UX-checklist item.
6. **Write "Consequence of the Gaps"**: 1-3 sentences tying the pain points back to trust, wasted time,
   or confusion — this justifies the fixes that will appear later in the flow spec.
7. **For a brand-new feature** (no existing component): skip step 5's "read the component" instruction
   and instead ground pain points in the *absence* of UI (e.g. "no client model, service, or component
   exists for X yet") and in real backend constraints (e.g. an endpoint that is commented out/disabled)
   — flag any such backend dependency explicitly rather than designing around it silently.

## Real Example

[`docs/ux/lessons-jtbd.md`](../../../docs/ux/lessons-jtbd.md) — Job Statement framed around a teacher
prepping lessons, with pain points grounded in actual code read from
[lesson-form.component.ts](../../../client/src/app/pages/lessons/lesson-form.component.ts):

```markdown
## Job Statement

When I'm prepping for a class or reorganizing my curriculum, I want to create, update, and remove
lesson records quickly and with confidence that I entered everything correctly, so I can move on to
building assignments without second-guessing myself.

## Current Solution & Pain Points (grounded in current code)

- **Gendered Hebrew copy bug**: the delete-confirmation dialog hardcodes feminine grammar —
  `"בטוחה שברצונך למחוק..."` and `acceptLabel: "מחקי"` — which is incorrect/exclusionary if the
  logged-in teacher identifies as male.
- **Inconsistent language**: the list page's toasts are Hebrew, but the form page's toasts are
  hardcoded English ("Error", "Failed to load lesson").
```

Note every bullet names a real string/behavior found in the component, not a hypothetical.

## Template

Copy-paste starting point: [assets/jtbd.template.md](./assets/jtbd.template.md)

## Pitfalls

- Don't write pain points from assumption or from a generic UX checklist — every bullet must cite
  something you actually read in the component (a literal string, a missing handler, a hardcoded
  value).
- Don't skip stating the persona explicitly — every jtbd doc names Teacher, Student, or both.
- Don't blend in journey-stage detail (thoughts/feelings per stage) — that belongs in
  [ux-journey-map-pattern](../ux-journey-map-pattern/SKILL.md), not here.
- Don't invent client-side bugs for a feature that has no client code yet — for greenfield features,
  ground gaps in the missing UI and the real backend contract instead.
- Don't silently design around a disabled/commented-out backend endpoint — call it out as a dependency.

## See Also

- [ux-journey-map-pattern](../ux-journey-map-pattern/SKILL.md) — the stage-by-stage journey built from
  this file's persona and pain points.
- [ux-flow-spec-pattern](../ux-flow-spec-pattern/SKILL.md) — the improved flow that fixes these pain
  points within `client.instructions.md` conventions.
