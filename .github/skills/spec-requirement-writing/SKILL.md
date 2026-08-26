---
name: spec-requirement-writing
description: "Use when writing or reviewing a single sentence in the SmartGrader specification set under docs/ — a functional requirement, a grading rule (G-N), a business rule (B-N), an acceptance criterion, or an outcome statement. Grounded in ISO/IEC/IEEE 29148: the sentence shape [condition][subject][action][object][constraint] and the nine quality characteristics as a review checklist. Covers the three writing failures this repository already produced (a defect list filed as specification, a goal phrased as a C# type name, a [Fix] that outlived its fix), why C# type names are banned from outcome statements, why Hebrew UI strings are quoted verbatim, stable rule ids, Given/When/Then acceptance criteria, and the document header. USE FOR: 'write a requirement for X', 'is this requirement well-formed', 'review the wording in grading-rules.md', 'turn this behaviour into a G-N rule', 'write acceptance criteria'. NOT for deciding which document a sentence belongs in or what an area doc must cover (that is spec-feature-area-doc), and NOT for the test that keeps a table true (that is spec-domain-doc-conformance)."
---

# Writing a Requirement in the SmartGrader Spec Set

One sentence at a time. This skill is about the sentence — not the document that holds it
([spec-feature-area-doc](../spec-feature-area-doc/SKILL.md)) and not the test that keeps it true
([spec-domain-doc-conformance](../spec-domain-doc-conformance/SKILL.md)).

## Why this skill exists — three failures this repo already produced

Not hypothetical. All three are quoted from `docs/ux/`, the set being replaced.

### 1. A defect list filed as specification

`docs/ux/assignments-jtbd.md` gives **3 lines** to the Job Statement and **28 lines** to
"Current Solution & Pain Points":

> **Most severe language-consistency finding across the whole audit**: […] the **entire Assignments
> feature — list and form — is 100% English-language**: toasts ("Error"/"Success"/"Failed to load
> assignments") […]

That is a true and useful sentence. It is not a requirement — it describes **what was broken in July
2026**, so it expired the day someone fixed it. A specification says what the system *shall* do;
a defect list says what it *did wrong last month*. Filing the second as the first is what made the
old set unreadable.

**Rule:** a sentence that would become false by fixing a bug is not a requirement. It is class C —
dated filename, delete clause, out of `docs/`.

### 2. A goal phrased as a C# type name

Same file, the Outcome the whole document exists to state:

> **Outcome**: A correct `AssignmentResponseDto` with `methodName` and `tests` that the AI grading
> pipeline can consume without ambiguity.

A teacher's goal, written as a serialization type. Nobody can confirm or deny it: "correct" is
undefined, and `AssignmentResponseDto` is not something a teacher wants.

**Rule: C# type names are banned from outcome statements and from requirement text.** Use the
glossary term — `תרגיל` / assignment, `מקרה בדיקה` / test case. `glossary.md` exists precisely so the
other twelve documents can describe an English codebase without filling up with DTO names. Type names
are allowed **only** in a code-anchor field, where they are pointing at code on purpose.

### 3. A `[Fix]` that outlived its fix

`docs/ux/assignments-flow.md:35`:

> 2. **[Fix]** Add the same inline-error pattern already used for `methodName` to the `title` field
>    too […] `<small class="p-error" *ngIf="form.get('title')?.invalid && …">כותרת היא שדה חובה</small>`

`client/src/app/pages/assignments/assignment-form.component.html:31` has done exactly that since
long before this was read. The same is true of the `[Fix]` on line 39 demanding Hebrew toasts —
`assignments-list.component.ts:344` reads `detail: "טעינת התרגילים נכשלה"`. **Every `[Fix]` in that
document is already implemented.** The document describes a system that no longer exists, and it does
so in the imperative mood, so a reader cannot tell it is stale — it reads like work waiting to be
done.

**Rule:** a requirement is a **statement about the system**, in the indicative. Never an instruction
to a developer. "Add X" is a work item; "the form shall display X" is a requirement. The first goes
stale silently; the second goes stale loudly, because a test can read it.

## The sentence shape (29148)

```
[condition] [subject] [action] [object] [constraint]
```

| Slot | Question | Example |
|---|---|---|
| condition | when does this apply? | *While the assignment title is empty and has been touched,* |
| subject | who or what acts? | *the assignment form* |
| action | one verb, `shall` | *shall display* |
| object | on what? | *the message «שם התרגיל הוא שדה חובה»* |
| constraint | bounded how? | *beneath the title field.* |

`shall` for a requirement. Not "should", not "must", not "will" — one modal, so a reader never has to
guess whether a sentence is binding.

**Omit a slot only deliberately.** A missing `condition` means *always*; if that is not what you meant,
the requirement is incomplete.

## Four before/after pairs, all from this repo

**Pair 1 — the outcome that was a type name**

| | |
|---|---|
| ❌ | **Outcome**: A correct `AssignmentResponseDto` with `methodName` and `tests` that the AI grading pipeline can consume without ambiguity. |
| ✅ | When a teacher saves an assignment, the system shall store its method name and its test cases together with it, and shall reject the save when the method name is empty. |
| Fails | **Unambiguous** (a type name is not a teacher-visible outcome), **Verifiable** ("correct" has no test). |

**Pair 2 — the defect that was filed as spec**

| | |
|---|---|
| ❌ | **Columns that render broken for every row, always**: the list table is typed as `AssignmentExtended` […] which has **none of those fields**. |
| ✅ | The assignments list shall display, per assignment, only values returned by `GET /api/lessons/{lessonId}/assignments`. |
| Fails | **Necessary** / **Appropriate** — a defect report is class C. The defect belongs in a dated list; the invariant it violated belongs here. |

**Pair 3 — the `[Fix]` in the imperative**

| | |
|---|---|
| ❌ | **[Fix]** Add the same inline-error pattern already used for `methodName` to the `title` field too. |
| ✅ | While the assignment title is empty and has been touched, the assignment form shall display «שם התרגיל הוא שדה חובה» beneath the title field. |
| Fails | **Conforming** — an instruction to a developer, not a statement about the system, so it cannot be checked against the system and it expires on merge. |

**Pair 4 — the list requirement that stopped too early**

| | |
|---|---|
| ❌ | The submissions list shall be sortable and searchable. |
| ✅ | The submissions list shall sort by submission time descending by default; shall page at 10 rows with options 10/25/50; shall match search text against the student's full name and the assignment title; and shall discard filters when the user navigates away from the screen. |
| Fails | **Complete** — four decisions were left to whoever writes the component next, which is how eleven list screens ended up each deciding on its own. |

**Every list screen's requirements must answer those four questions**: default sort · page size and
options · which fields search matches · whether filters survive navigating back.

## The nine characteristics, one question each

Run every sentence through these. A `no` sends it back.

| # | Characteristic | The question |
|---|---|---|
| 1 | **Necessary** | If this were deleted, would anything be lost? A restatement of another rule is not necessary — cite its id. |
| 2 | **Appropriate** | Is this the right document, and the right level? A pixel value is not a functional requirement. |
| 3 | **Unambiguous** | Can two readers reach two different implementations? "clear instructions", "correct", "trustworthy" — all ambiguous. |
| 4 | **Complete** | Can this be built without asking a follow-up question? (Pair 4.) |
| 5 | **Singular** | One `shall`, one behaviour. An "and" joining two behaviours is two requirements. |
| 6 | **Feasible** | Can it be built here, with this stack, this year? |
| 7 | **Verifiable** | Name the test or the observation that decides pass/fail. If you cannot, rewrite until you can. |
| 8 | **Correct** | Does the code actually do this today? `docs/` is **as-built** (decision 2) — desired-but-unbuilt goes to `.github/prompts/`. |
| 9 | **Conforming** | Right shape, right modal, right mood — indicative, not imperative. |

**7 is the one that bites.** "The dashboard shall feel responsive" fails it. "The dashboard shall
render its KPI cards within 2 seconds of the four requests resolving" passes.

## Stable rule ids

Two registries, ids never reused and never renumbered:

- `G-N` — grading rules, `docs/grading-rules.md`
- `B-N` — business rules, `docs/business-rules.md`

**A rule is stated once and referenced everywhere else.** An area doc's `Applicable Rules` section
lists ids; it does not restate the rule text. Restating creates a second source of truth, and the
second copy is the one that goes wrong.

When a rule is withdrawn, keep the id with a one-line tombstone (`G-12 — withdrawn 2026-08, superseded
by G-19`). Renumbering breaks every `[Trait("Rule", "G-N")]` binding in the test project silently.

## Hebrew UI strings are quoted verbatim

The product is Hebrew and RTL; the documents are English (decision 1). Any string the user actually
sees is quoted **exactly as it appears in the code**, in Hebrew, inside guillemets:

> the form shall display «שם התרגיל הוא שדה חובה»

Never translate it and never paraphrase it. The quoted string is the thing a test greps for, and a
translation is a second string that will drift. The same applies to test-failure messages.

Copy is gender-neutral Hebrew — a requirement that quotes gendered copy is quoting a defect.

## Acceptance criteria — Given/When/Then

Every functional requirement carries at least one. Pass/fail with no interpretation:

```
Given a teacher is editing an assignment whose title is empty
When she moves focus out of the title field
Then «שם התרגיל הוא שדה חובה» appears beneath it and the save button is disabled
```

**A criterion that needs judgement violates *Verifiable* and goes back.** "Then the screen looks
uncluttered" is not a criterion. "Then the table shows 5 columns" is.

## Document header

Every document in `docs/` opens with:

```markdown
# Grading Rules

> SmartGrader · Version 1.2 · Last updated 2026-08-26 · Status: as-built

| Version | Date | Change |
|---|---|---|
| 1.2 | 2026-08-26 | G-14 added — bonus is proportional |
| 1.1 | 2026-08-20 | G-7 corrected: `allocatable == 0` yields full marks |
```

The revision table is the cheapest defence against the failure above: a reader who can see the
document has not been touched since July knows to distrust it.

## Anti-patterns

| Anti-pattern | Why it is fatal |
|---|---|
| `[Fix]`, `TODO`, "currently broken" in `docs/` | Class C in a class B file. It expires and nothing goes red. |
| A C# type name in an outcome | Nobody outside the code can confirm it. Use the glossary term. |
| "should" / "must" / "will" mixed with "shall" | The reader cannot tell what is binding. |
| Restating a rule instead of citing `G-N`/`B-N` | Two sources of truth; the copy wins by accident. |
| A hand-counted number (`33 constructs`) | The catalog has **31**. That figure drifted within days of being written. Numbers go in a marked block with a test — see the sibling skill. |
| A requirement with no acceptance criterion | Unverifiable by construction. |
| Translating a Hebrew UI string into English | A second string, guaranteed to drift. |
| Prose inside a `<!-- gen: -->` block | Over-assertion; the markers get deleted by the next person they annoy. |

## See Also

- [spec-feature-area-doc](../spec-feature-area-doc/SKILL.md) — which document a sentence belongs in, and the seven sections every area doc has.
- [spec-domain-doc-conformance](../spec-domain-doc-conformance/SKILL.md) — the marker convention and the test that makes a table fail CI when the code moves.
- [backend-unit-test-pattern](../backend-unit-test-pattern/SKILL.md) — where the `[Trait("Rule", "G-N")]` bindings live.
