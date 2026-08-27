# Area: Shared UI

> SmartGrader · Version 1.0 · Last updated 2026-08-27 · Status: as-built

| Version | Date | Change |
|---|---|---|
| 1.0 | 2026-08-27 | First edition. Written last, once the five callers existed. |

## Purpose

The eight components every other area uses, and which therefore belong to none of them.

**This document exists because they had no owner.** The five job-based area docs cover all thirteen
page areas between them, and these eight fall through every one of those seams — most sharply the
notifications bell, which is a **feature** with two role-dependent feeds, a poll, browser-persisted read
state and real detection thresholds behind it, and which was specified nowhere at all.

## Who Uses This

Everyone, always, without noticing. A shared component's failure is felt as "the app is broken",
not as "that component is broken" — which is the argument for specifying them at all.

## Screens & Routes

Shared components render inside other areas' routes and claim only the application shell itself.

<!-- gen:arearoutes shared-ui -->

| `path` | Full route | Screen |
|---|---|---|
| `` | `/` | `AppLayoutComponent` — the teacher and admin shell |

<!-- /gen -->

`StudentLayoutComponent` is claimed by [student.md](student.md) through the `my` route, because it
exists only for that area.

## The eight components

| Component | What it is |
|---|---|
| `layout/notifications-bell` | two entirely different feeds by role, polled |
| `submission-feedback-panel` | the one shared rendering of a graded submission |
| `layout/topbar` | navigation, and the role-gated items |
| `layout/app-layout` | the teacher and admin shell — topbar, hero strip, footer |
| `layout/student-layout` | the student shell |
| `hebrew-date-picker` | three dropdowns as one form value |
| `password-checklist` | the four password rules, live |
| `accessibility/accessibility-widget` | theme, font scale and reduced motion |
| `submitted-code` | every file of a submission, escaped |

Nine rows for "eight components" — `submitted-code` was extracted during Plan B's B3, after two detail
screens were each found to render only `sourceCode` and never `sourceFiles`, so a multi-file submission
showed an empty box on both.

## Functional Requirements

### Notifications bell

**SH-1** The bell shall show **class signals** to a teacher or admin and **recently graded
submissions** to a student — two different feeds behind one control.

**SH-2** The bell shall poll every 30 seconds.

**SH-3** Read state shall be kept in browser storage, per device, and corrupt stored data shall be
treated as "everything is new" rather than raising an error.

**SH-4** A class signal shall be shown only when at least three students are affected **and** at least
half of those who submitted (`B-26`).

**SH-5** A hidden test case's input shall never appear in a signal's text — only its position (`B-31`).

**SH-6** Class signals shall never reach a student (`B-36`).

### Feedback panel

**SH-7** The panel shall render the score tiles, the requirements table, the model's prose and the test
results, and shall be the **single** implementation used by both the teacher's detail screen and the
student's feedback screen.

**SH-8** The panel shall render whatever the server sent it and shall perform **no redaction of its
own** — hiding is a server concern (`S-7`, `S-8`).

**SH-9** A score of `null` shall be rendered as "no grade", visually distinct from a score of 0.

### Shell and navigation

**SH-10** The topbar shall render the admin-only items only for an admin.

**SH-11** Every topbar item shall navigate to a real screen and shall highlight itself when active.

**SH-12** The hero strip shall render on the dashboard only.

### Form controls

**SH-13** The Hebrew date picker shall present three dependent dropdowns as one form value, shall
handle Adar I and Adar II in a leap year, and shall reset the dependent fields when a parent changes.

**SH-14** The password checklist shall reflect the four password rules as they are, never a second copy
of them.

### Accessibility widget

**SH-15** Accessibility preferences shall have exactly one owner and one storage key (`D-15`).

**SH-16** The widget shall hold no state of its own and shall only call the service.

**SH-17** «איפוס» shall clear every preference, and nothing shall return on the next load.

### Submitted code

**SH-18** The component shall render every file of a multi-file submission with its name, and shall
fall back to the single source when there are no files.

**SH-19** Submitted source shall always be rendered as escaped text, never as HTML (`B-51`).

## Applicable Rules

| Rule | Why it reaches this area |
|---|---|
| `B-25` … `B-36` | everything the bell does, including why there is no notifications table |
| `B-51` | the reason `submitted-code` exists in the form it does |
| `D-1` … `D-15` | shared components are where an accessibility failure multiplies by thirteen screens |
| `G-1` | the feedback panel must render "no grade" as rejection, not as a low score |

## Acceptance Criteria

**AC-1 (SH-1)** Given a teacher and a student both open the bell, when both feeds load, then the
teacher sees class signals and the student sees her own graded submissions, and neither sees the
other's.

**AC-2 (SH-3)** Given corrupt data in the stored read-state key, when the bell loads, then it renders
with everything marked unread and no error appears.

**AC-3 (SH-5)** Given a signal about a hidden test case, when its text is rendered, then it names
«בדיקה 3» and contains no input value.

**AC-4 (SH-7)** Given the same graded submission opened by its teacher and by the student, when both
render, then the layout is identical and the differences are only in the fields the server sent.

**AC-5 (SH-9)** Given a submission with status `RequirementsNotMet`, when the panel renders it, then it
shows that no grade exists — not «0».

**AC-6 (SH-11)** Given each topbar item in turn, when it is clicked, then a real screen loads and that
item is the one highlighted.

**AC-7 (SH-17)** Given the user changes theme, font scale and reduced motion, when she presses «איפוס»
and reloads, then all three are at their defaults.

**AC-8 (SH-18)** Given a submission of three files, when either detail screen renders it, then three
named blocks appear.

## Screen Composition

### Feedback panel — the one that matters

Four regions stacked: score tiles · requirements table · the model's prose in tabs · test results.

**Decision: same panel, two default states.**

| Viewer | Open on load | Collapsed |
|---|---|---|
| **Student** | the score and the prose | the requirements table, the test results |
| **Teacher** | everything | nothing |

| | |
|---|---|
| **Comes off** | **Nothing, for either viewer.** The difference is what is open, not what exists. |
| **Eye hits first** | Student: the score and the sentence explaining it. Teacher: the same score, in a screen she reads top to bottom to diagnose. |
| **Reading order** | Score → why → which requirement → which test. |
| **Per row** | Requirements: what was required · was it met · where in the code. Test results: verdict · case name, and for a hidden case the verdict alone (`S-8`). |

**Why collapsed and not removed.** When a blocking requirement fails there is **no grade at all**
(`G-1`), and the requirements table is the only element on the screen that says which one. Remove it
and a student is looking at «אין ציון» with no reason given — which is the precise failure this whole
area exists to prevent. Collapsing costs her one click; removing costs her the explanation.

**Why the same component and not two.** Two screens rendering the same graded submission is exactly the
shape that produced the multi-file bug: both detail screens forgot `sourceFiles` and each showed an
empty box, independently. One implementation, two default states.

**Two refinements the implementation added, both to keep "collapsed" honest.**

1. **The pass/fail counts live in the panel's header, not inside it.** Collapsed, a student still
   reads «עברו 4 · נכשלו 1 · מתוך 5». What folds away is the case-by-case table — the detail — not
   the result. A collapsed section that hides its own headline is a section that was removed.
2. **The requirements table opens for a student too, when a blocking requirement failed.** In that
   state `G-1` means there is no grade at all, and the table stops being detail: it *is* the
   explanation for the number she came to understand. Collapsing it there would produce exactly the
   «אין ציון» with no reason that the rule above forbids.

Implemented as one `viewer: "student" | "teacher"` input on the panel, defaulting to `teacher`.

**The score tile must render `null` and `0` differently** (`SH-9`). «אין ציון» and «0» mean opposite
things — one is a rejection, the other is a grade — and a tile that shows «0» for both tells a student
she failed when she was never assessed.

### Notifications bell

| | |
|---|---|
| **Comes off** | Nothing. |
| **Eye hits first** | The unread count. |
| **Reading order** | Newest first, unread before read. |
| **Per row** | One sentence, built server-side, plus its time. |

**The sentence is built on the server on purpose**, so the bell and the daily digest email can never
drift into sounding like two different alert systems. A hidden test case's input never appears in it —
only its position (`SH-5`, `B-31`) — because the same sentence is emailed and email gets forwarded.

**There is no "no news" state to design for a teacher's email**: a day with nothing to report sends
nothing at all (`B-34`). The bell itself does need an empty state, and it must not read as an error.

### Topbar and shells

| | |
|---|---|
| **Comes off** | **The two fake items.** «תרגילים» and «הגשות» point at `redirectTo` stubs, so they land on the wrong screen and highlight the wrong item (`SH-11`). |
| **Eye hits first** | The current location. |
| **Reading order** | **By the two real hierarchies**: courses → lessons → assignments, and classes → students → submissions. |
| **Per row** | — |

The nav order should follow the hierarchies the area docs establish, which is why Plan B's B4 was
blocked on this phase. It no longer is. **Topbar first, then the routes** — reversed, a wrong highlight
becomes a hard 404.

The hero strip renders on the dashboard only (`SH-12`); a decorative band on every screen is a band
nobody sees and everybody scrolls past.

### Form controls

| | |
|---|---|
| **Hebrew date picker** | Three dropdowns, one value. Day and month reset when the year changes, because Adar I and Adar II exist only in a leap year and a stale month is a wrong date. |
| **Password checklist** | Four rules, filling in live. It reflects the rules; it never restates them (`SH-14`). |
| **Submitted code** | Every file with its name, LTR, monospace, one dark background — and always escaped (`SH-19`, `B-51`). |

### Accessibility widget

| | |
|---|---|
| **Comes off** | Nothing to remove — but it holds **no state of its own** (`SH-16`). |
| **Eye hits first** | The controls, in the order people actually need them: contrast, then text size, then motion. |
| **Reading order** | Theme → font scale → reduced motion → reset. |
| **Per row** | — |

**«איפוס» must clear everything, and nothing may come back on reload** (`SH-17`, `D-15`). That is
written as a requirement because it was a real defect: the widget and the service each stored
`reduceMotion` under a different key, so reset cleared one copy and the other returned on the next load.

## Explicitly Not Supported

- ⚠️ **`SH-11` fails today.** The topbar links to `/assignments` and `/submissions`, which are
  `redirectTo` stubs — so «תרגילים» lands on Lessons and highlights the wrong item. Plan B's B4.
- **There is no notification entity and no notifications table, and one must not be added** (`B-25`).
  Every signal is computed on demand from submission rows in a date window; a table would only record
  what the date range already determines.
- **Read state does not travel between devices or browsers.** It is `localStorage`, per device, and
  that is deliberate — there is nothing worth a server round trip in "which alerts have I glanced at".
- **The bell has no history.** It shows the current window; there is no archive to scroll back through.
- **Preferences saved under the old accessibility keys were not migrated.** Anyone who had set them
  before Plan B's B3 gets defaults once. A migration path for a handful of display toggles would
  outlive its usefulness immediately.
- **The feedback panel does not redact anything.** If a field reaches it, it renders it — which is why
  redaction must happen in the handler and never here.
