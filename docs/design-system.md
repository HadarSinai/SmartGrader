# Design System

> SmartGrader · Version 1.0 · Last updated 2026-08-27 · Status: as-built

| Version | Date | Change |
|---|---|---|
| 1.0 | 2026-08-27 | First edition. Absorbs `ux/master-spec.md` §4–§6, `ux/accessibility-checklist.md` and `client/spec.md`. |

**What this document answers:** what makes every screen one system rather than eleven screens that
happen to share a font.

**What it does not answer:** what a given screen *does* (the area docs, phase A4), or what it *shows*
(each area doc's `Screen Composition`, phase A5).

---

## Three mother templates

**Every screen is declared as an instance of one of three templates. There is no free-form screen.**
That is the rule the rest of the document exists to make possible — a fourth shape is not a design
decision, it is a screen nobody will maintain.

### List

Page header (title · subtitle · primary "+ new" at the start of the row · breadcrumb) → search and
filter row → selection toolbar, shown only when something is selected → table
(`[☑] | data columns | [👁 view] | [⋯ actions]`) → paginator, correct in RTL.

Full anatomy: [client-list-table-pattern](../.claude/skills/client-list-table-pattern/SKILL.md).

⚠️ **The selection toolbar is design-only.** "מחיקת נבחרים" raises an information toast; there is no
bulk-delete endpoint. Building it for real is Plan B's B5.

**Instances:** students · lessons · assignments · submissions · lesson results · classes · courses ·
teachers · logs · my lessons · my grades.

### Form

Compact fields with a thin `--app-border` and a soft focus ring · inline validation as a `p-error`
message beneath the field, shown only after `touched` · required fields marked `*` **in the label** ·
Hebrew placeholders only · actions at the bottom, "ביטול" (outlined) then the primary action, aligned
to the end of the row · leaving with unsaved changes raises a confirmation.

**Instances:** student · lesson · assignment · class · course · teacher · submission · login ·
forgot-password · reset-password · profile.

### Detail

Page header with secondary actions (back, edit) → **one unified status area**, where status,
compilation error, model error and notes all appear in the same place and the same visual language,
instead of scattered boxes → a key-value grid → a code block (`sg-code-box`: LTR, monospace, one dark
background).

**Instances:** submission detail · student feedback.

---

## Foundations

Tokens are defined once in `client/src/styles.css`. **A component that invents its own colour has left
the system**; `DesignTokenTests` counts those.

<!-- gen:tokens -->

| Token | Role |
|---|---|
| `--app-bg` | the page ground |
| `--app-surface` | a card or panel |
| `--app-surface-2` | a nested or recessed surface |
| `--app-border` | every hairline |
| `--app-text` | body text |
| `--app-text-strong` | headings and emphasis |
| `--app-muted` | captions and secondary text |
| `--app-shadow` | the base shadow colour |
| `--app-font` | the single family |
| `--app-font-mono` | code, everywhere it is shown |
| `--primary-color` | the primary action |
| `--primary-color-text` | text on primary |
| `--accent` | the warm accent |
| `--accent-2` | its secondary tone |
| `--accent-ink` | text on accent |
| `--focus-ring` | the visible focus indicator |
| `--status-success` | graded, complete |
| `--status-success-bg` | its tint |
| `--status-success-ink` | text on that tint |
| `--status-warn` | waiting, or a fault that is not the student's |
| `--status-warn-bg` | its tint |
| `--status-warn-ink` | text on that tint |
| `--status-error` | a failure the student can act on |
| `--status-error-bg` | its tint |
| `--status-error-ink` | text on that tint |
| `--status-info` | neutral, in progress |
| `--status-info-bg` | its tint |
| `--status-info-ink` | text on that tint |
| `--radius-sm` | inputs and chips |
| `--radius-md` | cards |
| `--radius-lg` | dialogs and panels |
| `--shadow-sm` | resting elevation |
| `--shadow-md` | raised elevation |
| `--space-1` | the 8pt grid |
| `--space-2` | |
| `--space-3` | |
| `--space-4` | |
| `--space-6` | |
| `--text-xs` | the smallest caption |
| `--text-sm` | caption |
| `--text-base` | body |
| `--text-lg` | subheading |
| `--text-xl` | page title |

<!-- /gen -->

**Three elevation levels, and only three:** flat (no shadow) for the page · `--shadow-sm` for a card ·
`--shadow-md` for anything floating — a dialog, an overflow menu, a toast. A fourth depth is not
readable as a fourth meaning.

**Spacing is the 8pt grid**, through `--space-*`. **Type is one scale**, four steps. Neither is
negotiable per screen — that is what "one system" means in practice.

### Deliberately dropped

`ux/master-spec.md` specified pixel values like "compact fields (~38px)". **That kind of precision was
never enforceable and never enforced** — nothing checked it, and a tilde in a specification is an
admission that it will not be. Height comes from the spacing scale and the component library. What is
specified here is what can be verified.

---

## Status semantics

Seven statuses, from the same enum as [domain-model.md](domain-model.md). **Status is never colour
alone** — always colour **and** icon **and** a Hebrew label, because a colour-only status is invisible
to a colour-blind reader and to a screen reader alike.

<!-- gen:enum SmartGrader.Domain.Entities.SubmissionStatus -->

| Member | Value | Hebrew label · token · icon |
|---|---|---|
| `PendingAi` | 0 | «ממתין לבדיקה» · `--status-warn` · `pi-clock` |
| `ProcessingAi` | 1 | «בבדיקה...» · `--status-info` · `pi-spinner` |
| `Done` | 2 | «נבדק» · `--status-success` · `pi-check-circle` |
| `AiFailed` | 3 | «שגיאת בדיקה» · `--status-error` · `pi-exclamation-triangle` |
| `CompilationFailed` | 4 | «שגיאת קומפילציה» · `--status-error` · `pi-exclamation-triangle` |
| `JudgeUnavailable` | 5 | «תקלה במערכת הבדיקה» · `--status-warn` · `pi-exclamation-circle` |
| `RequirementsNotMet` | 6 | «הדרישות לא התקיימו» · `--status-error` · `pi-ban` |

<!-- /gen -->

**Two of these carry a decision, not a style choice.**

`JudgeUnavailable` is **amber, not red**. It is an infrastructure fault, the student did nothing wrong,
and there is nothing for her to fix — showing it in the same red as her own compilation error tells her
to go debug a problem she does not have.

`RequirementsNotMet` gets `pi-ban`, **not** the shared error triangle. It is not a technical fault: the
code ran fine, it simply did not do what the exercise asked. A different icon is what keeps
"rejection" from reading as "crash" (`G-1`).

### The mapping has one home — `STATUS_PRESENTATION`

The table above is realised once, in `client/src/app/models/submission.model.ts`. Every screen reads
from it. **A sixth special case is not the way to fix a seventh disagreement.**

**It was written five times, and two of the copies disagreed.** Kept here because the two failures are
the argument for the single source:

`submissions/submissions-list` derived severity by substring matching —

```ts
if (s.includes("fail") || s.includes("error")) return "danger";
```

— and `"judgeunavailable"` contains none of the tested substrings, so it fell through to the default
`"info"`. On the teacher's submissions list an outage rendered as a neutral blue information chip while
every other screen showed it amber. The comment directly above that code already warned about this
exact failure for `RequirementsNotMet`, which had been special-cased; the second case was missed.

`my/my-grades` gave `CompilationFailed` the icon `pi-times-circle` where every other screen gave it
`pi-exclamation-triangle`. Nobody would have found that by reading — only by opening two screens side
by side and noticing.

---

## Shared patterns

### Empty, loading, error

| State | Pattern |
|---|---|
| **Empty** | `pi-inbox` icon · one short Hebrew sentence · the primary action as a call to action — «אין תרגילים להצגה.» + «תרגיל חדש» |
| **Loading** | `[loading]` on tables, skeletons on KPI cards. **Never a blank screen.** |
| **HTTP error** | caught globally by `ApiErrorInterceptor` → an error toast. A component never calls `console.error` and never calls `alert`. |

Empty and loading states exist on all eleven list screens — verified.

### Toasts and delete confirmations

Toasts go through `MessageService` only. Hebrew, gender-neutral: summary «בוצע» / «שגיאה», detail
«השיעור נמחק בהצלחה».

Deletion goes through `ConfirmationService` only, in one shape:

> message: «האם למחוק את "{שם}"? לא ניתן לשחזר פעולה זו.»
> header: «אישור מחיקה» · accept: «מחיקה» · reject: «ביטול»

**The "cannot be undone" clause is part of the pattern, not decoration.** A confirmation that does not
say what is lost is a speed bump, not a decision point.

### Dates

`dd.MM.yy HH:mm` — `13.07.26 10:33`. Hebrew locale, no AM/PM, no calendar icon inside a table cell.
Configured once (`LOCALE_ID` in `app.config.ts`) and used by every date pipe.

A lesson date is **entered and displayed as a Hebrew date** through the shared picker; the underlying
value stays a `DateTime`.

### Copy

Hebrew only, and **gender-neutral throughout** — this system is used by women, and copy that assumes
otherwise is a defect, not a nuance. Prefer a neutral construction over a dual form: «נמחק בהצלחה»,
not «נמחקת/נמחק».

Student-submitted source is always rendered as **escaped text**, never as HTML (`B-51`).

---

## Accessibility requirements

Accessibility is a **product contract**, not a checklist. Each requirement below is numbered and
carries an acceptance criterion, so it is verified like any other requirement rather than ticked.

> **Note on ids.** Decision 3 registers `G-N` and `B-N` only. `D-N` is a third prefix introduced here
> because these are binding requirements that other documents must be able to cite. Flagged rather
> than assumed.

| Id | Requirement | Acceptance criterion |
|---|---|---|
| D-1 | Every interactive element shall be reachable by keyboard. | Given any screen, when the user presses Tab repeatedly, then every button, input, row action and menu item receives focus. |
| D-2 | Tab order shall follow the RTL reading order. | Given a list screen, when the user tabs from the page title, then focus moves right-to-left and top-to-bottom, never jumping to the visual left first. |
| D-3 | Every focusable element shall show a visible focus indicator distinct from its hover state. | Given any focusable element, when it receives keyboard focus, then `--focus-ring` is visible and differs from the element's hover appearance. |
| D-4 | Escape shall close every dialog. | Given an open `p-dialog` or `p-confirmDialog`, when the user presses Escape, then it closes and focus returns to the trigger. |
| D-5 | Every icon-only button shall carry a Hebrew `aria-label` naming both the action and its subject. | Given a row delete button, when a screen reader reads it, then it announces «מחיקת שיעור: מבוא לפייתון» — not «מחיקה». |
| D-6 | Every form input shall have an associated label element. | Given any form, when inputs are inspected, then none relies on a placeholder as its only name. |
| D-7 | A status change during polling shall be announced, not only redrawn. | Given a submission being polled, when its status changes from «בבדיקה...» to «נבדק», then the change is announced through a live region. |
| D-8 | Text contrast shall be at least 4.5:1. | Given any text on any token background, when measured, then the ratio is ≥ 4.5:1 — including `--status-*-ink` on `--status-*-bg`. |
| D-9 | Status shall never be conveyed by colour alone. | Given any status indicator, when rendered in greyscale, then its meaning is still readable from its icon and label. |
| D-10 | An interactive target shall be at least 24×24px, and a primary action at least 44px high. | Given any icon button, when measured, then it is ≥ 24×24; given a primary form action, then it is ≥ 44px high. |
| D-11 | The layout shall hold at 360, 768 and 1280px, and at 200% text zoom. | Given each of the three widths and 200% zoom, when a list screen is loaded, then no horizontal scrolling of the page body occurs and no content is clipped. |
| D-12 | Directional icons shall be mirrored for RTL. | Given the paginator and any next/back control, when rendered, then arrows point in the direction that matches RTL movement. |
| D-13 | Numbers and dates shall render LTR inside RTL text. | Given `13.07.26 10:33` inside a Hebrew sentence, when rendered, then its digits are not visually reversed. |
| D-14 | `prefers-reduced-motion` shall be honoured globally. | Given the OS setting is on, when any screen loads, then no non-essential animation plays. |
| D-15 | Accessibility preferences shall have exactly one storage mechanism. | Given the user changes a preference and presses «איפוס», when the page is reloaded, then nothing returns. |

**D-15 exists because it was a real defect** — the widget and the service each stored `reduceMotion`
under a different key, so "reset" cleared one copy and the other came back on the next load. Fixed in
Plan B's B3; the requirement is what stops it recurring.

---

## What is verified, and how

| Check | Enforced by |
|---|---|
| every token named here exists in `styles.css` | `DesignTokenTests` |
| the status table matches the enum | `EnumTableConformanceTests` |
| hardcoded colours in `client/src/app` do not increase | `DesignTokenTests`, ratcheted |

**The colour ratchet is currently 0 files.** A6 converted all fourteen. From here on, one hardcoded
colour anywhere under `client/src/app` is a failing test — and the failure names the file.

**What the conversion actually found.** Every one of those hex values was a CSS fallback
(`var(--token, #hex)`), not a raw colour — so at first glance the tokens were already in use. But
**four of the token names did not exist**: `--app-text-muted`, `--app-surface-muted`, `--app-warning`
and `--app-danger`. Those fallbacks were firing on every render, silently, and in **dark mode they
painted light-theme colours** into an otherwise dark screen. Four more uses of `--app-text-muted` had
no fallback at all, so the declaration was simply invalid and the colour was inherited.

A fallback is not a safety net here. It is the thing that hides a typo in a token name for months.

`D-1` … `D-15` are **not** machine-verified. Every one of them needs a person with a keyboard, a
screen reader, or a contrast meter. Saying so plainly is better than a green test that checked
something easier.
