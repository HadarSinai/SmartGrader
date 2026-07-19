---
description: "Feature plan: Client-Side Sorting & Search for All List Pages"
---
# Plan: Client-Side Sorting & Search for All List Pages

## TL;DR

Add client-side ONLY sorting and searching to the SmartGrader Angular list pages. **No server changes** —
all `getAll` endpoints already return full arrays to the browser (no server paging), so PrimeNG's built-in
`pSortableColumn` sorting and in-memory `filter()` getters are sufficient. Follow the existing pattern from
the Students list (`sg-search` input + dropdown filter + `filteredStudents` getter +
`hasActiveFilters` empty state) in `client/src/app/pages/students/`.

## Current State (verified 2026-07-15)

| Page | Search | Filter | Sorting |
|---|---|---|---|
| Students | ✅ by name (`filteredStudents`) | ✅ by class (`classOptions`) | ❌ none |
| Lessons | ✅ by name/subject (`filteredLessons`) | ❌ | ❌ none |
| Assignments | ✅ by title/description (`filteredAssignments`) | ❌ | ⚠️ only `pSortableColumn="title"` |
| Submissions | ❌ (binds raw `submissions`) | ❌ | ❌ none |
| LessonResults | ❌ | ❌ | ❌ none |
| Student area (`/my`) | ❌ | ❌ | ❌ (EXCLUDED — see Scope) |

Key facts:

- Lessons & Assignments have BOTH a desktop `p-table` (`desktop-only`) and a mobile `p-dataView`
  (`mobile-only`), both bound to the same filtered getter — search automatically applies to both;
  column sorting applies to the desktop table only (acceptable).
- `SubmissionResponseDto` already has `studentName`, `assignmentName`, `status`
  (`PendingAi|ProcessingAi|Done|AiFailed|CompilationFailed`, Hebrew labels via `STATUS_LABELS_HE`),
  `score: number | null`, `submittedAt` (ISO string) — everything needed for search/filter/sort.
- LessonResults row view-model has `studentName`, `completedAssignments`, `totalAssignments`,
  `finalScore: number | null`, `isComplete`.
- PrimeNG sorts client arrays automatically when `pSortableColumn` + `p-sortIcon` are present —
  no `sortField`/`sortOrder` state code needed.

## Steps

### Phase 1 — Students list (sorting only; search already exists)

1. In `client/src/app/pages/students/students-list.component.html`, add `pSortableColumn` +
   `p-sortIcon` to the שם (`fullName`) and כיתה (`className`) header cells.

### Phase 2 — Lessons list (sorting only)

2. In `client/src/app/pages/lessons/lessons-list.component.ts` (inline template), add
   `pSortableColumn` + `p-sortIcon` to: שם (`name`), מקצוע (`subject`), תאריך (`lessonDate` —
   sort on the ISO Gregorian field, keep displaying `lessonDateHebrew`).

### Phase 3 — Assignments list (complete the sorting)

3. In `client/src/app/pages/assignments/assignments-list.component.ts`, add `pSortableColumn` +
   `p-sortIcon` to the הגשות column (`submissionsCount`). Keep the existing `title` sort.
   Skip the test-cases column (`tests?.length` is computed — not sortable without a mapped field;
   not worth a view-model for this).

### Phase 4 — Submissions list (the biggest change: search + status filter + sorting)

4. In `client/src/app/pages/submissions/submissions-list.component.ts`:
   - Add `query = ""` and `statusFilter: SubmissionStatus | null = null` properties.
   - Add a `filteredSubmissions` getter: match `query` against `studentName` / `assignmentName`
     (trim + toLowerCase, same as students), AND exact-match `statusFilter` when set.
   - Add a `hasActiveFilters` getter (same pattern as students).
   - Add `statusOptions` built from `STATUS_LABELS_HE` plus a `{ label: "כל הסטטוסים", value: null }` first entry.
   - Add `FormsModule`, `InputTextModule`, `DropdownModule` to the component `imports` array.
5. In `client/src/app/pages/submissions/submissions-list.component.html`:
   - Add the `sg-search` input (copy the exact markup pattern from
     `students-list.component.html` — `p-input-icon-right sg-search` span, `pInputText`,
     `[(ngModel)]="query"`, Hebrew placeholder + `aria-label`).
   - Add a `p-dropdown` for the status filter bound to `statusFilter`.
   - Change the table binding from `[value]="submissions"` to `[value]="filteredSubmissions"`.
   - Add `pSortableColumn` + `p-sortIcon` on: תלמיד (`studentName`), מטלה (`assignmentName`),
     הוגש (`submittedAt`), סטטוס (`status`), ציון (`score`).
   - Update the empty state: when `hasActiveFilters` is true show a "no results match the filter"
     message with a clear-filters action (students pattern), otherwise keep the existing empty state.

### Phase 5 — LessonResults list (search + status filter + sorting)

6. In `client/src/app/pages/lesson-results/lesson-results-list.component.ts`:
   - Add `query = ""` and `completionFilter: boolean | null = null`.
   - Add a `filteredRows` getter: `query` vs `studentName`; `completionFilter` vs `isComplete`
     (options: הכל / הושלם / בתהליך).
   - Add `FormsModule`, `InputTextModule`, `DropdownModule` imports; bind the table to the getter.
   - Add `pSortableColumn` + `p-sortIcon` on: שם תלמיד (`studentName`), ציון סופי (`finalScore`),
     התקדמות (`completedAssignments`), סטטוס (`isComplete`).
   - Same `hasActiveFilters` + filtered empty state treatment as Phase 4.

### Phase 6 — Verification

7. `npx ng build` in `client/` — must compile with no errors.
8. Manual checks (dev server + browser):
   - RTL: sort icons render on the correct side of the header text; dropdowns open RTL.
   - Sorting a column with `null` values (`score`, `finalScore`) does not crash and groups nulls.
   - Search + status filter combine correctly (AND semantics) on Submissions.
   - Lessons/Assignments mobile card views (`p-dataView`) still receive filtered results.
   - Breakpoints 360 / 768 / 1280px still look right on the touched toolbars.

## Decisions

- **Client-side only** (explicit user decision): no `[FromQuery]` search/sort/page parameters, no
  EF `Skip/Take`. Server-side becomes relevant only if real pagination or thousands of rows arrive.
- Sorting on Lessons date uses the Gregorian `lessonDate` ISO field (correct chronological order),
  while the cell keeps showing the Hebrew date string.
- Hebrew text search: `toLowerCase()` is a no-op for Hebrew but kept for Latin content (method names etc.).

## Scope Exclusions

- No server changes of any kind.
- No search/sort in the student area (`/my` screens) — short personal lists, `responsiveLayout="stack"` tables.
- No pagination changes, no bulk actions, no new design tokens (reuse existing `sg-search` / filter styles).
