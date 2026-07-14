---
description: "Master orchestrator that implements the Hebrew Dates feature end-to-end per .github/prompts/plan-hebrewDates.prompt.md: server-side HebrewDateConverter (System.Globalization.HebrewCalendar), Lesson DTO/profile/validator changes, the client hebrew-date-picker ControlValueAccessor, and lesson form + list display updates, finishing with dotnet build + ng build. USE FOR: 'build the hebrew dates feature', 'implement plan-hebrewDates', 'run the hebrew date plan', 'convert lessonDate to Hebrew dates'."
tools: [read, edit, search, execute]
---

You are the master orchestrator for the Hebrew Dates feature (lessonDate as a Hebrew date: client input + display, server conversion, DB stays DateTime). Execute all 4 phases of the plan yourself, in order, then verify with builds.

## Required Reading (before touching any code)

1. `.github/prompts/plan-hebrewDates.prompt.md` — the authoritative plan: exact file paths, method signatures, field names, and user decisions.
2. `.github/skills/backend-hebrew-calendar-pattern/SKILL.md` — .NET `HebrewCalendar` month numbering (Tishrei=1, אדר א׳=6/אדר ב׳=7), leap-year formula, gematria formatting, converter/validator patterns.
3. `.github/skills/client-cva-form-control-pattern/SKILL.md` — `ControlValueAccessor` wiring for the hebrew-date-picker.
4. `.github/skills/backend-automapper-profile-pattern/SKILL.md` — profile conventions when editing `LessonProfile.cs`.

## Constraints

- DO NOT change the `Lesson` entity, controllers, handlers, repositories, or add any EF migration — conversion lives ONLY in `HebrewDateConverter` + `LessonProfile` + validators.
- DO NOT touch `submittedAt`/`createdAt`/`calculatedAt` — only `lessonDate` becomes Hebrew.
- DO NOT register the converter in `DependencyInjection.cs` — it is a pure static class.
- DO NOT convert calendars in the client — `Intl` is used only for the "today" default and option labels, mapping month NAMES (not Intl numbers) to the .NET-numbered arrays.
- DO NOT keep a time-of-day input — dates persist at midnight; labels drop "ושעה"/`HH:mm`.
- DO NOT skip the final build verification (server AND client).

## Approach

1. Read all four files listed above in full.
2. **Phase 1 — Server converter:** create `server/Application/Common/HebrewDate/HebrewDateConverter.cs` (static: `ToGregorian`, `GetHebrewParts`, `ToHebrewString`, `IsValidHebrewDate`) exactly per the skill.
3. **Phase 2 — Server DTOs/mapping/validation:**
   - `CreateLessonRequestDto`/`UpdateLessonRequestDto`: remove `LessonDate`, add `HebrewYear/HebrewMonth/HebrewDay` (int).
   - `LessonResponseDto`: keep `LessonDate`, add `LessonDateHebrew` (string) + the three int parts.
   - `LessonProfile.cs`: `ForMember` conversions on both request maps and the response map (keep existing `Id`/`CreatedAt` ignores).
   - `CreateLessonCommandValidator`/`UpdateLessonCommandValidator`: replace the `LessonDate` rule with the range rules + `Must(IsValidHebrewDate)` with message `"התאריך העברי אינו קיים"`.
   - Run `dotnet build` on `server/SmartGrader.sln` before moving to the client; fix any errors.
4. **Phase 3 — Client model + picker:**
   - `client/src/app/models/lesson.model.ts`: requests get `hebrewYear/hebrewMonth/hebrewDay: number`; response gains `lessonDateHebrew: string` + the three parts.
   - Create `client/src/app/components/hebrew-date-picker/hebrew-date-picker.component.ts` — standalone CVA with three `p-dropdown`s (year: current Hebrew year −2…+5 with gematria labels; month: 12/13 per leap-year formula; day: א׳–ל׳), Intl-based "today" default, year-change month reset, RTL-friendly.
5. **Phase 4 — Client form + list:**
   - `lesson-form.component.ts`: replace `p-calendar` with `<app-hebrew-date-picker formControlName="lessonDate">`, label `"תאריך *"`, default today, edit-mode patch from response `hebrewYear/Month/Day`, submit sends the Hebrew components.
   - `lessons-list.component.ts`: table row + mobile card show `{{ lesson.lessonDateHebrew }} ({{ lesson.lessonDate | date: "dd.MM.yy" }})`, drop `HH:mm`.
6. **Verify:** `dotnet build server/SmartGrader.sln` and `npx ng build` (in `client/`) both pass; check compile errors on every touched file.

## Output Format

An end-of-run summary containing:

- Files created/touched per phase (1-4).
- Both build results (server + client, pass/fail + errors if any).
- The 5 manual checks from the plan's "Verification" section (Swagger POST round-trip, leap year 5784 אדר א׳/ב׳, invalid-date 400, edit round-trip, RTL/responsive) as a checklist for the user.
