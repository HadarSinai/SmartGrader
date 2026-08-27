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
| `areas/teacher-content.md` | Courses, lessons, assignments — authoring what students will do | phase A4 |
| `areas/teacher-classroom.md` | Classes, students, submissions, lesson results, dashboard — running a class | phase A4 |
| `areas/student.md` | The `/my` area — what a student sees of her own work | phase A4 |
| `areas/admin.md` | Teachers and the system log | phase A4 |
| `areas/auth-account.md` | Login, password recovery, profile, lockout | phase A4 |
| `areas/shared-ui.md` | The components every area uses — topbar, shells, notifications bell, feedback panel, form controls, accessibility widget | phase A4 |

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

Tables inside an invisible `<!-- gen: … -->` marker are the machine-checked ones. Meaning and rationale
columns are prose and are deliberately left alone — asserting them would make ordinary editing hostile,
and the markers would be deleted within a month.

## Conventions

- **English**, filenames and content. Hebrew UI strings are quoted verbatim, in Hebrew, because a
  translation is a second string that will drift.
- **As-built.** These documents describe what exists. Desired-but-unbuilt work lives in
  `.github/prompts/`.
- **Stable rule ids** — `G-N` grading, `B-N` business, `D-N` design and accessibility. A rule is stated once and referenced
  everywhere else by id.
- Written with the [spec-requirement-writing](../.claude/skills/spec-requirement-writing/SKILL.md),
  [spec-domain-doc-conformance](../.claude/skills/spec-domain-doc-conformance/SKILL.md) and
  [spec-feature-area-doc](../.claude/skills/spec-feature-area-doc/SKILL.md) skills.

---

## Superseded documents

These are still on disk and still readable. **Each is replaced by a document above, and each is deleted
in phase A7** once every reference to it has been retargeted. Until its replacing phase runs, the old
file is still the only description of its subject.

| File | Replaced by | Phase |
|---|---|---|
| [auth-plan.md](auth-plan.md) | `business-rules.md` (what shipped) and `.github/prompts/` (what did not) | A2 |
| [ux/master-spec.md](ux/master-spec.md) | `design-system.md` | A3 |
| [ux/accessibility-checklist.md](ux/accessibility-checklist.md) | `design-system.md` — accessibility becomes numbered requirements with acceptance criteria | A3 |
| [ux/personas.md](ux/personas.md) | the three-line persona inside each area doc | A4 |
| [ux/redesign-plan.md](ux/redesign-plan.md) | nothing — a dated work plan belongs in `.github/prompts/` | A7 |
| [ux/README.md](ux/README.md) | this file | A7 |
| [ux/lessons-jtbd.md](ux/lessons-jtbd.md) · [ux/lessons-journey.md](ux/lessons-journey.md) · [ux/lessons-flow.md](ux/lessons-flow.md) | `areas/teacher-content.md` | A4 |
| [ux/assignments-jtbd.md](ux/assignments-jtbd.md) · [ux/assignments-journey.md](ux/assignments-journey.md) · [ux/assignments-flow.md](ux/assignments-flow.md) | `areas/teacher-content.md` | A4 |
| [ux/students-jtbd.md](ux/students-jtbd.md) · [ux/students-journey.md](ux/students-journey.md) · [ux/students-flow.md](ux/students-flow.md) | `areas/teacher-classroom.md` | A4 |
| [ux/submissions-jtbd.md](ux/submissions-jtbd.md) · [ux/submissions-journey.md](ux/submissions-journey.md) · [ux/submissions-flow.md](ux/submissions-flow.md) | `areas/teacher-classroom.md` | A4 |
| [ux/lessonresults-jtbd.md](ux/lessonresults-jtbd.md) · [ux/lessonresults-journey.md](ux/lessonresults-journey.md) · [ux/lessonresults-flow.md](ux/lessonresults-flow.md) | `areas/teacher-classroom.md` | A4 |

⚠️ **Read the old set with the date in mind.** Its `[Fix]` items are written in the imperative and read
like open work, but they were implemented months ago — every one in `assignments-flow.md` is already in
the code. That is the specific failure this rewrite exists to end, and it is why the replacement is
tested rather than merely rewritten.
