# SmartGrader Client – Modern UI Spec (Figma-inspired)

## Goal

Upgrade the entire app UI to a modern, consistent look inspired by the provided Figma (not pixel-perfect),
without changing features/flows and without replacing PrimeNG/PrimeFlex.

## Constraints (locked)

- PrimeNG + PrimeFlex only
- No feature changes, no new flows
- Keep RTL/Hebrew correct
- Avoid touching services/API unless explicitly requested

## Pilot Screen

- Students page (students-list.component) is the pilot.
  Standardize global tokens + recurring UI patterns there first, then roll out to other pages.

## Visual Language (high level)

- Clean surfaces, consistent spacing, slightly rounded corners, subtle shadows
- Typography: readable hierarchy (page title, section title, labels, table text)
- Buttons/cards/tables/forms share a consistent “design system” feel

## Design Tokens (minimum required)

All typography tokens are **rem**. All spacing tokens are **rem**.
New tokens must **map to existing --app-\* / theme variables** to avoid duplicate sources of truth.

Define tokens in global styles (src/styles.css):

- Radius: --radius-sm, --radius-md, --radius-lg
- Shadows: --shadow-sm, --shadow-md
- Spacing: --space-1, --space-2, --space-3, --space-4, --space-6
- Typography: --text-sm, --text-base, --text-lg, --text-xl

Mapping rule:

- If a token can be derived from existing variables (e.g. --app-border, --app-shadow, --app-text-strong),
  derive it. Do not create “parallel” colors/values.

## Where changes should happen

Primary:

- src/styles.css (tokens + sg-\* standards + minimal PrimeNG overrides)
- Existing sg-\* classes (standardize and reuse; avoid uncontrolled new classes)

Secondary (only if needed):

- component-level styles for specific layout fixes (scoped, minimal)

## Component Standards (recurring)

### Cards / Containers

- Consistent padding via space tokens
- Border + shadow consistent
- Title typography consistent

### Buttons

- Same radius, height, icon alignment (RTL-safe)
- Avoid random colors; use existing tokens/theme

### Forms

- Labels spacing, input heights, error message style consistent
- RTL alignment correct

### Tables (PrimeNG)

- Header style consistent, row padding consistent
- Empty state and loading state consistent

### Filter Panels / Toolbar Areas

- Standard panel style for filters/search
- Consistent spacing, alignments, RTL-safe

### Toasts / Dialogs

- Typography and spacing consistent with tokens

## Verification checklist

- Students page looks consistent and modern: toolbar, table, empty/loading, actions
- RTL sanity: icons, paddings, action columns align correctly
- Responsive sanity: 360px / 768px / 1280px without broken layout
- No console errors introduced
- Build succeeds
