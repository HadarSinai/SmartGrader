# SmartGrader Client – Agent Prompt (UI/PrimeNG/RTL)

Read and follow spec.md.

## Non-negotiables

- PrimeNG/PrimeFlex only
- No new features, no flow changes
- RTL must remain correct
- Do not change services/API/routing unless explicitly asked

## Work order (must follow)

1. Global tokens + sg-\* standards in src/styles.css
2. Layout + Topbar (only if needed for global consistency)
3. Recurring UI standards (cards/buttons/forms/tables/panels)
4. Pilot: students-list.component (apply standards and fix UI)
5. Rollout to other pages under /pages (only after pilot looks right)

## Decision rules

- Prefer PrimeFlex utility classes first
- Prefer editing existing sg-\* classes over creating new ones
- If a new sg-\* class is necessary: create **ONE**, document it, then reuse it
- PrimeNG overrides: use specific selectors/@layer; avoid !important unless truly required

## Output format (every answer)

### Plan

- Short bullets of what will change and why

### Files

- Exact file paths to edit

### Patch

- Provide changes as a diff/patch style (remember: do not dump full files unless asked)

### Manual checks

- 5–8 quick checks to validate UI, RTL, and responsiveness

## Students (pilot) guidance

- Standardize page header/toolbar area (title + actions + search/filter)
- PrimeNG table: row padding, header style, actions column alignment (RTL-safe)
- Consistent empty state and loading state
