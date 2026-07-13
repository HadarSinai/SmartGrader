# Lessons — User Journey Map

**Persona**: Teacher (see [personas.md](./personas.md)).
**Scope**: Full lifecycle — List → Create → Edit → Delete — grounded in the current
`lessons-list`/`lesson-form` components.

## Stage 1: List / Discover

**Doing**: Opens `/lessons`, scans the table (name, subject, date, teacher, assignment count),
optionally searches by name/subject/teacher.
**Thinking**: "Is this lesson already here? How many assignments does it have so far?"
**Feeling**: 🙂 Confident — the table and search are clear and already well-built.
**Pain points**: None significant; empty state ("אין שיעורים להצגה") and loading state both exist.
**Opportunity**: None urgent — this stage is in good shape; keep as the reference pattern.

## Stage 2: Create

**Doing**: Clicks "שיעור חדש", fills name/subject/date/teacher, clicks "יצירה".
**Thinking**: "Did I fill in everything correctly? Why is the button still grey?"
**Feeling**: 😐 Neutral-to-mildly-frustrated when the submit button is disabled with no explanation.
**Pain points**:

- Submit button disables silently on invalid form — no per-field message explaining what's missing.
- If the create request fails, the toast shows **English** text ("Failed to create lesson") in an
  otherwise all-Hebrew flow — jarring and looks broken.
  **Opportunity**: Add inline required-field messages; translate all form-page toasts to Hebrew to match
  the list page's tone.

## Stage 3: Edit

**Doing**: Clicks the pencil icon on a row, form pre-fills with existing values, edits, clicks "שמירה".
**Thinking**: "Did my changes save? Can I get back to the list safely if I change my mind?"
**Feeling**: 🙂 Generally fine, but same validation/English-toast gaps as Create apply here too.
**Pain points**: Same as Create stage; additionally, clicking "ביטול" discards in-progress edits with
zero confirmation.
**Opportunity**: Add a lightweight "unsaved changes" guard on cancel/navigate-away if the form is dirty.

## Stage 4: Delete

**Doing**: Clicks the trash icon, sees a confirm dialog, confirms.
**Thinking**: "Wait — this message doesn't read right for me." (if the teacher isn't a woman, the
feminine grammar of "בטוחה"/"מחקי" is factually wrong for them.)
**Feeling**: 😕 Momentarily confused/put-off by a grammar mismatch, independent of gender identity this
also reads as unpolished.
**Pain points**: Hardcoded feminine-gendered confirm-dialog copy (`"בטוחה שברצונך למחוק..."`,
`acceptLabel: "מחקי"`).
**Opportunity**: Rewrite confirm copy in gender-neutral Hebrew phrasing (e.g., "האם למחוק את...?" /
"מחיקה" / "ביטול") — small copy fix, meaningfully improves trust and correctness.

## Success Metric

Teacher completes create/edit/delete of a lesson in under 30 seconds with zero confusion about
required fields, and every message she sees (success, error, confirm) reads as correct, professional
Hebrew.
