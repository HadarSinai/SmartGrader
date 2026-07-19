---
name: ux-journey-map-pattern
description: "Use when writing or reviewing docs/ux/{feature}-journey.md for the SmartGrader client: mapping the List→Create→Edit→Delete lifecycle with what the user is Doing/Thinking/Feeling, Pain points, and Opportunities at each stage. USE FOR: 'write a journey map for X', 'map the user journey for Lessons/Students/Assignments/Submissions', 'stage-by-stage UX walkthrough'. NOT for the Job Statement/persona doc (see ux-jtbd-analysis-pattern) or the improved flow/design spec (see ux-flow-spec-pattern)."
---

# UX Journey Map Pattern

Every SmartGrader feature's journey map lives at `docs/ux/{feature}-journey.md` and walks the full
lifecycle of the screen — List → Create → Edit → Delete — stage by stage, seeded from the persona and
pain points already established in the paired `{feature}-jtbd.md`.

## When to Use

- Writing the journey map for an existing screen right after its JTBD doc is done.
- Writing a journey map for a brand-new screen (e.g. LessonResults) — same stage structure, grounded in
  the intended flow instead of an existing component.
- Reviewing an existing `{feature}-journey.md` for stages that no longer match the current component
  behavior.

## Workflow

1. **Read the paired `docs/ux/{feature}-jtbd.md` first** (if it exists) — reuse its persona and pain
   points as seeds rather than re-deriving them from scratch.
2. **Define the stages.** For an existing CRUD screen this is always at least: `List / Discover`,
   `Create`, `Edit`, `Delete` — grounded in the real actions the component actually exposes (don't add a
   stage for a button that doesn't exist). For a dual-persona feature, either write two separate stage
   sets in one file or split into two files — state the persona per stage set explicitly.
3. **For each stage, write four fields**:
   - **Doing**: the concrete UI action, referencing real element labels/copy where possible (e.g. the
     actual Hebrew button text).
   - **Thinking**: a short quoted internal monologue, plausible for the persona.
   - **Feeling**: an emoji + one-word/short label (🙂 Confident, 😕 Confused, 😐 Neutral, etc.).
   - **Pain points**: concrete, sourced from the jtbd doc or the component code. It is fine — and
     expected — to write "None significant" for a stage that is already well-built; don't manufacture a
     pain point just to fill the field.
   - **Opportunity**: one concrete, actionable fix per pain point (or "None urgent — keep as reference
     pattern" when there's no pain point).
4. **Close with a "Success Metric"** section: one sentence describing observable success, tied back to
   the JTBD outcome (e.g. "completes create/edit/delete in under 30 seconds with zero confusion").

## Real Example

[`docs/ux/lessons-journey.md`](../../../docs/ux/lessons-journey.md), Stage 4:

```markdown
## Stage 4: Delete

**Doing**: Clicks the trash icon, sees a confirm dialog, confirms.
**Thinking**: "Wait — this message doesn't read right for me."
**Feeling**: 😕 Momentarily confused/put-off by a grammar mismatch.
**Pain points**: Hardcoded feminine-gendered confirm-dialog copy (`"בטוחה שברצונך למחוק..."`,
`acceptLabel: "מחקי"`).
**Opportunity**: Rewrite confirm copy in gender-neutral Hebrew phrasing.
```

And Stage 1, showing that "no significant pain point" is a valid, expected outcome:

```markdown
## Stage 1: List / Discover

**Feeling**: 🙂 Confident — the table and search are clear and already well-built.
**Pain points**: None significant; empty state and loading state both exist.
**Opportunity**: None urgent — this stage is in good shape; keep as the reference pattern.
```

## Template

Copy-paste starting point: [assets/journey.template.md](./assets/journey.template.md)

## Pitfalls

- Don't invent stages that don't exist in the real UI flow (e.g. a "bulk delete" stage when there's no
  bulk-delete button).
- Don't force a manufactured pain point onto a stage that's genuinely fine — "None significant" is
  expected and preserves credibility of the doc.
- Don't re-derive the persona/pain points from scratch — pull them from the paired
  [ux-jtbd-analysis-pattern](../ux-jtbd-analysis-pattern/SKILL.md) doc.
- Don't propose the actual fix implementation details here (copy strings, PrimeNG API calls) — that
  level of detail belongs in [ux-flow-spec-pattern](../ux-flow-spec-pattern/SKILL.md); keep
  "Opportunity" to one sentence.

## See Also

- [ux-jtbd-analysis-pattern](../ux-jtbd-analysis-pattern/SKILL.md) — the persona/pain-point source this
  journey map is seeded from.
- [ux-flow-spec-pattern](../ux-flow-spec-pattern/SKILL.md) — turns each stage's "Opportunity" into a
  concrete, implementable flow fix.
