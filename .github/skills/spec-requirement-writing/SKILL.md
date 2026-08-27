---
name: spec-requirement-writing
description: "Use when writing or reviewing a single sentence in the SmartGrader specification set under docs/ — a functional requirement, a grading rule (G-N), a business rule (B-N), an acceptance criterion, or an outcome statement. Grounded in ISO/IEC/IEEE 29148:2018: the two requirements-construct patterns (5.2.4), the nine characteristics of an individual requirement (5.2.5), the five characteristics of a set (5.2.6), and the shall/should/will/may language criteria (5.2.7). Covers the three writing failures this repository already produced (a defect list filed as specification, a goal phrased as a C# type name, a [Fix] that outlived its fix), why C# type names are banned from outcome statements, why Hebrew UI strings are quoted verbatim, stable rule ids, Given/When/Then acceptance criteria, and the document header. USE FOR: 'write a requirement for X', 'is this requirement well-formed', 'review the wording in grading-rules.md', 'turn this behaviour into a G-N rule', 'write acceptance criteria', 'shall or should'. NOT for deciding which document a sentence belongs in or what an area doc must cover (that is spec-feature-area-doc), and NOT for the test that keeps a table true (that is spec-domain-doc-conformance)."
---

# Writing a Requirement in the SmartGrader Spec Set

One sentence at a time. This skill is about the sentence — not the document that holds it
([spec-feature-area-doc](../spec-feature-area-doc/SKILL.md)) and not the test that keeps it true
([spec-domain-doc-conformance](../spec-domain-doc-conformance/SKILL.md)).

## Why this skill exists — three failures this repo already produced

Not hypothetical. All three are transcribed verbatim from the superseded UX document set — the one
this skill's output replaced. Those files were deleted in Plan A's phase A7, so the quotations below
are the surviving record of them; [docs/README.md](../../../docs/README.md) maps what replaced what.

### 1. A defect list filed as specification

The superseded `assignments-jtbd.md` gave **3 lines** to the Job Statement and **28 lines** to
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

The superseded `assignments-flow.md`, line 35:

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

## The sentence shape — 29148:2018 clause 5.2.4, *Requirements construct*

The standard gives **two** patterns. Use the one that fits; do not invent a third.

```
SYNTAX-1   [subject] [action] [constraint of action]
SYNTAX-2   [condition] [subject] [action] [object] [constraint of action]
```

SYNTAX-1 is for a requirement that always applies — no condition to state. SYNTAX-2 is the full form.

| Slot | Question | Example |
|---|---|---|
| condition | when does this apply? | *While the assignment title is empty and has been touched,* |
| subject | who or what acts? — the system or a part of it | *the assignment form* |
| action | one verb, `shall` | *shall display* |
| object | on what is the action performed? | *the message «שם התרגיל הוא שדה חובה»* |
| constraint of action | bounded how, or to what result? | *beneath the title field.* |

The standard's own example, for comparison:

> "Upon receiving signal x *[condition]*, the system *[subject]* shall set *[action]* the 'signal x
> received' bit *[object]* within 2 seconds *[constraint of action]*."

**Dropping the `condition` is a decision, not an omission** — SYNTAX-1 states *always*. If that is not
what you meant, the requirement is incomplete.

### The four modals — clause 5.2.7, *Requirement language criteria*

The standard does not ban the other modals. It assigns each one a distinct job, and mixing them up is
what makes a reader unable to tell what is binding:

| Modal | Means | Binding? |
|---|---|---|
| **shall** | a requirement | ✅ **yes** |
| should | a preference or goal | ❌ no |
| will | a statement of fact, futurity, or purpose — also used to set context | ❌ no |
| may | a suggestion or an allowance | ❌ no |

Non-requirement prose uses plain verbs — *is*, *are*, *was*.

**"must" is the one to avoid.** It carries no assigned meaning here and reads as a synonym for `shall`,
so it quietly creates a second binding modal. In `docs/`, a rule is `shall`; everything else is either
one of the three non-binding modals, used on purpose, or plain prose.

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

## The nine characteristics — clause 5.2.5, one question each

Run every sentence through these. A `no` sends it back.

| # | Characteristic | 29148's meaning | The question to ask here |
|---|---|---|---|
| 1 | **Necessary** | Removing it would leave a deficiency | If this were deleted, would anything be lost? A restatement of another rule is not necessary — cite its id. |
| 2 | **Appropriate** | The level of abstraction fits; no unnecessary constraints; no implementation detail | Is this the right document and the right altitude? A pixel value is not a functional requirement, and neither is a DTO name. |
| 3 | **Unambiguous** | One interpretation only | Can two readers reach two different implementations? "clear instructions", "correct", "trustworthy" — all ambiguous. |
| 4 | **Complete** | Everything needed to understand it is in it | Can this be built without asking a follow-up question? (Pair 4.) |
| 5 | **Singular** | One capability, one requirement | One `shall`, one behaviour. An "and" joining two behaviours is two requirements. |
| 6 | **Feasible** | Achievable within technical, cost and regulatory bounds | Can it be built here, with this stack, this year? |
| 7 | **Verifiable** | Can be proven met, by a defined method | Name the test or the observation that decides pass/fail. If you cannot, rewrite until you can. |
| 8 | **Correct** | An accurate representation of the actual need | Does the code actually do this today? `docs/` is **as-built** (decision 2) — desired-but-unbuilt goes to `.github/prompts/`. |
| 9 | **Conforming** | Follows the approved template and conventions | Right pattern (SYNTAX-1/2), right modal, indicative mood — not imperative. |

**7 is the one that bites.** "The dashboard shall feel responsive" fails it. "The dashboard shall
render its KPI cards within 2 seconds of the four requests resolving" passes.

## The five set characteristics — clause 5.2.6

The nine above judge one sentence. `grading-rules.md`, `business-rules.md` and each area doc's
`Functional Requirements` are **sets**, and a set can fail while every sentence in it passes.

| # | Characteristic | The question to ask of the whole document |
|---|---|---|
| 1 | **Complete** | Does the set cover everything needed, with nothing left "to be determined"? |
| 2 | **Consistent** | Do any two rules contradict? Is one term used for one thing throughout — the `glossary.md` job? |
| 3 | **Feasible** | Can the set be satisfied *together*, within budget and schedule? Two individually feasible rules can be jointly impossible. |
| 4 | **Comprehensible** | Can the owner read the set and understand what the system does? |
| 5 | **Able to be validated** | Can the set as a whole be confirmed against the real need? |

**5 has a concrete acceptance test here**, from the plan: hand `grading-rules.md` to the owner with a
real graded submission and ask her to reproduce the number by hand, from the document alone. If she
cannot, the set fails — regardless of how well-formed each `G-N` is.

**2 is what the `G-N`/`B-N` registry exists to protect.** Consistency is a property of the set, so it
cannot be checked one sentence at a time; a single numbered registry is what makes checking it possible
at all.

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
| "must" used for a requirement | It has no assigned meaning in 5.2.7 and reads as a second binding modal. Use `shall`. |
| "should" / "will" / "may" used where `shall` was meant | Each is explicitly **non-binding**. The reader will correctly conclude the rule is optional. |
| Restating a rule instead of citing `G-N`/`B-N` | Two sources of truth; the copy wins by accident. |
| A hand-counted number (`33 constructs`) | The catalog has **31**. That figure drifted within days of being written. Numbers go in a marked block with a test — see the sibling skill. |
| A requirement with no acceptance criterion | Unverifiable by construction. |
| Translating a Hebrew UI string into English | A second string, guaranteed to drift. |
| Prose inside a `<!-- gen: -->` block | Over-assertion; the markers get deleted by the next person they annoy. |

## Provenance of the 29148 content

Everything attributed to the standard above is from **ISO/IEC/IEEE 29148:2018**, clauses 5.2.4–5.2.7.
The standard is paywalled, so this was verified against the official ISO/iTeh sample (which carries the
clause numbering and titles) plus independent secondary sources that agree with each other:

| Claim | Clause | Verified against |
|---|---|---|
| Clause numbers and titles | 5.2.4–5.2.7 | The official ISO/IEC/IEEE 29148:2018 sample PDF's table of contents |
| Two syntax patterns, five slot names, the "signal x" example | 5.2.4 | Multiple published renderings of the construct |
| The nine characteristics and their meanings | 5.2.5 | Two independent sources naming the same nine |
| The five set characteristics | 5.2.6 | Same |
| shall / should / will / may semantics | 5.2.7 | Same |

⚠️ **The full normative text was not read** — it is behind ISO/IEEE licensing. If an argument ever turns
on the standard's exact wording rather than its substance, buy the clause; do not settle it from here.

Note that **29148:2011 is a different list.** Its stakeholder-requirements characteristics were
*necessary, implementation free, unambiguous, consistent, complete, singular, feasible, traceable,
verifiable, affordable, bounded*. Several online summaries still quote that list under a 2018 heading.
This repository uses the **2018** nine.

## See Also

- [spec-feature-area-doc](../spec-feature-area-doc/SKILL.md) — which document a sentence belongs in, and the fixed section outline every area doc shares.
- [spec-domain-doc-conformance](../spec-domain-doc-conformance/SKILL.md) — the marker convention and the test that makes a table fail CI when the code moves.
- [backend-unit-test-pattern](../backend-unit-test-pattern/SKILL.md) — where the `[Trait("Rule", "G-N")]` bindings live.
