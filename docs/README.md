# SmartGrader Documentation

> SmartGrader · Version 1.0 · Last updated 2026-08-26

**What this file is:** a map. Given a question, it names the one file that answers it — and nothing
else. A summary here would be a second copy of the answer, and the second copy is the one that goes
wrong.

---

## Which file answers which question

| Document | Answers | Status |
|---|---|---|
| [glossary.md](glossary.md) | What does this Hebrew term mean, and which identifier in the code carries it? | ✅ |
| [domain-model.md](domain-model.md) | What *is* this thing — its fields, its states, what is deliberately absent? | ✅ |
| [permissions.md](permissions.md) | May this person do this, to whose data? | ✅ |
| [grading-rules.md](grading-rules.md) | How is a grade produced, and how do I explain one to a parent? | ✅ |
| [business-rules.md](business-rules.md) | What is the rule about resubmission / lockout / deletion, and where is it enforced? | ✅ |
| [design-system.md](design-system.md) | What makes every screen one system — templates, states, statuses, accessibility? | ✅ |
| [areas/teacher-content.md](areas/teacher-content.md) | Courses, lessons, assignments — authoring what students will do | ✅ |
| [areas/teacher-classroom.md](areas/teacher-classroom.md) | Classes, students, submissions, lesson results, dashboard — running a class | ✅ |
| [areas/student.md](areas/student.md) | The `/my` area — what a student sees of her own work | ✅ |
| [areas/admin.md](areas/admin.md) | Teachers and the system log | ✅ |
| [areas/auth-account.md](areas/auth-account.md) | Login, password recovery, profile, lockout | ✅ |
| [areas/shared-ui.md](areas/shared-ui.md) | The components every area uses — topbar, shells, notifications bell, feedback panel, form controls, accessibility widget | ✅ |

Thirteen files including this one. A document not listed above does not belong in `docs/`.

## What keeps these true

Prose cannot be verified, so it is not asserted. Everything derived from code is, by tests under
`server/Tests/SmartGrader.UnitTests/Docs/`:

| Test | Fails when |
|---|---|
| `DocsIndexTests` | a document is missing from this index, or a relative link is broken |
| `GlossaryConformanceTests` | the glossary names an identifier the code no longer has |
| `EnumTableConformanceTests` | an enum table drifts from the enum |
| `PermissionsMatrixConformanceTests` | an endpoint or client route has no row, or its roles disagree |
| `GradingRuleCoverageTests` | a `G-N` loses the test that proves it, or a test cites a rule that does not exist |
| `BusinessRuleAnchorTests` | a `B-N` cites a file that no longer exists, or the ids stop being unique |
| `DesignTokenTests` | the design system names a token `styles.css` does not define, or hardcoded colours increase |
| `AreaRouteCoverageTests` | a client route is claimed by no area document, or by two |

Tables inside an invisible `<!-- gen: … -->` marker are the machine-checked ones. Meaning and rationale
columns are prose and are deliberately left alone — asserting them would make ordinary editing hostile,
and the markers would be deleted within a month.

## Conventions

- **English**, filenames and content. Hebrew UI strings are quoted verbatim, in Hebrew, because a
  translation is a second string that will drift.
- **As-built.** These documents describe what exists. Desired-but-unbuilt work lives in
  `.github/prompts/`.
- **Stable rule ids** — `G-N` grading, `B-N` business, `D-N` design and accessibility. A rule is stated
  once and referenced everywhere else by id.

  `D-N` is a **third** prefix, added during A3 and approved by the owner. It exists because
  accessibility is neither a grading rule nor a business rule, and folding it into `B-N` would have put
  "4.5:1 contrast" in the same numbering as "a submission locks at the retry threshold". The `D-N`
  requirements are the only ones in the set that are **not** machine-verified — each needs a person with
  a keyboard, a screen reader or a contrast meter — and a separate prefix is what keeps that distinction
  visible instead of hiding it inside a list that is otherwise tested.

  The area docs carry their own local prefixes for screen-level rules (`S-N` student, `SH-N` shared UI,
  `AU-N` auth, `AD-N` admin, `TC-N` teacher content), scoped to their document.
- Written with the [spec-requirement-writing](../.claude/skills/spec-requirement-writing/SKILL.md),
  [spec-domain-doc-conformance](../.claude/skills/spec-domain-doc-conformance/SKILL.md) and
  [spec-feature-area-doc](../.claude/skills/spec-feature-area-doc/SKILL.md) skills.

---

## Deleted documents

Phase A7 deleted the previous specification set — 20 files under `docs/ux/`, `docs/auth-plan.md` and
`client/spec.md`. **They are gone from the working tree, not from history**: `git log --follow` on any
path below still shows every version. This table is what replaced each one, so that a reference found
in an old commit message, PR or prompt can still be resolved.

| Deleted file | Replaced by |
|---|---|
| `auth-plan.md` | [business-rules.md](business-rules.md) `B-11` … `B-24` for what shipped; [.github/prompts/plan-authOpenWork.prompt.md](../.github/prompts/plan-authOpenWork.prompt.md) for what did not |
| `client/spec.md` | [design-system.md](design-system.md) — every token it named is in the token table, and `DesignTokenTests` proves each one exists |
| `ux/master-spec.md` | [design-system.md](design-system.md) |
| `ux/accessibility-checklist.md` | [design-system.md](design-system.md) — a checklist became numbered `D-1` … `D-15` requirements with acceptance criteria |
| `ux/personas.md` | the three-line persona inside each [area doc](areas/) |
| `ux/redesign-plan.md` | nothing. A dated work plan is not a specification |
| `ux/README.md` | this file |
| `ux/lessons-*.md` · `ux/assignments-*.md` (jtbd · journey · flow) | [areas/teacher-content.md](areas/teacher-content.md) |
| `ux/students-*.md` · `ux/submissions-*.md` · `ux/lessonresults-*.md` (jtbd · journey · flow) | [areas/teacher-classroom.md](areas/teacher-classroom.md) |

**Why they were deleted rather than left in place.** Their `[Fix]` items are written in the imperative
and read like open work, but they were implemented months ago — every one in `assignments-flow.md` was
already in the code. **A document that describes a finished job as a pending one is worse than no
document**, because it is read as instructions. That is the specific failure this rewrite exists to
end, and it is why the replacement is tested rather than merely rewritten.

Deleted alongside them: four `ux-*` skills (their methodology lives in
[spec-feature-area-doc](../.claude/skills/spec-feature-area-doc/SKILL.md) and
[spec-requirement-writing](../.claude/skills/spec-requirement-writing/SKILL.md)) and seven agents whose
only input was a `docs/ux/` file. `phase-client-flow-fix-implementation` was deleted rather than
retargeted: it existed to walk `**[Fix]**` markers, the area docs have none, and pointing it at a
document of a different shape would have been a worse lie than removing it.

**Three feature plans still cite the deleted paths and were deliberately kept:**
`plan-clientUxScreensSpec`, `plan-notifications-bell` and `plan-studentAreaMyJourney` (in
`.github/prompts/` and `.claude/commands/`). Each now opens with a ⛔ banner saying it is a historical
record of how a feature was built and must not be read as instructions. They are the only files in the
repository where `docs/ux` and `client/spec.md` still appear.
