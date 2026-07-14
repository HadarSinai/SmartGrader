# Plan: Hebrew Dates for lessonDate (client input + display, server conversion, DB stays DateTime)

## TL;DR

The lessons form will let the teacher pick a **Hebrew date** (day/month/year dropdowns, no time), the client
sends the Hebrew components as numbers, and the **server converts** them with `System.Globalization.HebrewCalendar`
(built into .NET, no packages) and stores a regular `DateTime` at midnight. **No DB change, no migration.**
Lists display Hebrew + Gregorian, e.g. `י"ד תמוז תשפ"ו (14.07.26)`. Only `lessonDate` is affected —
`submittedAt`/`createdAt` stay Gregorian. All conversions are centralized in the AutoMapper profile, so the
MediatR handlers stay untouched.

## User decisions (already confirmed)

- Scope: input + display, only `lessonDate`; time is dropped (save at midnight)
- Conversion on the SERVER (client never converts calendars; `Intl` is used only for "today" default + option labels)
- Display format: Hebrew + Gregorian

## Key facts (from codebase research)

- Server: `CreateLessonRequestDto` / `UpdateLessonRequestDto` / `LessonResponseDto` in `server/Application/Dtos/Lessons/`.
  `CreateLessonHandler` does `_mapper.Map<Lesson>(request.Dto)`; `UpdateLessonHandler` does `_mapper.Map(request.Dto, lesson)` —
  both purely AutoMapper-driven. `LessonProfile.cs` in `server/Application/Common/Mapping/`. FluentValidation validators
  run via `ValidationBehavior` pipeline. `Lesson.LessonDate` is `DateTime` (SQLite TEXT).
- Client: `client/src/app/models/lesson.model.ts`; `lesson-form.component.ts` uses `p-calendar` (form init ~line 183,
  patch ~204, submit `toISOString()` ~230); `lessons-list.component.ts` shows the date at line ~146 (table) and ~232
  (mobile card). No `ControlValueAccessor` exists in the client yet. Dashboard does NOT use `lessonDate`.
- .NET `HebrewCalendar` month numbering: Tishrei=1; leap year has 13 months with Adar I=6, Adar II=7; non-leap Adar=6.
  Leap-year formula: `(7*year + 1) % 19 < 7`.
- .NET formats Hebrew dates with gematria natively: `CultureInfo("he-IL")` with `DateTimeFormat.Calendar = new HebrewCalendar()`,
  then `date.ToString("dd MMMM yyyy", ci)` → `י"ד תמוז תשפ"ו`.

## Steps

### Phase 1 — Server: converter utility

1. Create `server/Application/Common/HebrewDate/HebrewDateConverter.cs` — a pure **static** class (no DI registration):
   - `DateTime ToGregorian(int hebrewYear, int hebrewMonth, int hebrewDay)` — via `new HebrewCalendar().ToDateTime(...)`, midnight
   - `(int Year, int Month, int Day) GetHebrewParts(DateTime date)` — via `GetYear`/`GetMonth`/`GetDayOfMonth`
   - `string ToHebrewString(DateTime date)` — gematria via he-IL culture with HebrewCalendar (`"dd MMMM yyyy"`)
   - `bool IsValidHebrewDate(int y, int m, int d)` — `m <= GetMonthsInYear(y) && d <= GetDaysInMonth(y, m)` (+ sane year range)

### Phase 2 — Server: DTOs, mapping, validation

2. `CreateLessonRequestDto` + `UpdateLessonRequestDto`: **remove** `DateTime LessonDate`, **add**
   `int HebrewYear`, `int HebrewMonth`, `int HebrewDay`.
3. `LessonResponseDto`: keep `LessonDate` (ISO) and **add** `string LessonDateHebrew`, `int HebrewYear`,
   `int HebrewMonth`, `int HebrewDay` (so the edit form can populate dropdowns without client-side conversion).
4. `LessonProfile.cs` — all conversion lives here, handlers unchanged:
   - `CreateMap<CreateLessonRequestDto, Lesson>()` + `CreateMap<UpdateLessonRequestDto, Lesson>()`:
     `.ForMember(d => d.LessonDate, opt => opt.MapFrom(s => HebrewDateConverter.ToGregorian(s.HebrewYear, s.HebrewMonth, s.HebrewDay)))`
     (Update map keeps its existing `Id`/`CreatedAt` ignores.)
   - `CreateMap<Lesson, LessonResponseDto>()`: `ForMember` for `LessonDateHebrew` (← `ToHebrewString`) and the three
     Hebrew part fields (← `GetHebrewParts`).
5. `CreateLessonCommandValidator` + `UpdateLessonCommandValidator`: replace the `LessonDate` rule with:
   `HebrewYear` InclusiveBetween(5000, 6000); `HebrewMonth` InclusiveBetween(1, 13); `HebrewDay` InclusiveBetween(1, 30);
   plus a cascading `Must` on the tuple using `HebrewDateConverter.IsValidHebrewDate` with message
   `"התאריך העברי אינו קיים"`.
6. No controller / handler / repository / entity / migration changes.

### Phase 3 — Client: model + hebrew-date-picker component

7. `client/src/app/models/lesson.model.ts`: requests → `hebrewYear/hebrewMonth/hebrewDay: number`;
   `LessonResponseDto` += `lessonDateHebrew: string`, `hebrewYear/hebrewMonth/hebrewDay: number`.
8. New shared standalone component `client/src/app/components/hebrew-date-picker/hebrew-date-picker.component.ts`
   implementing `ControlValueAccessor`; value type `{ hebrewYear: number; hebrewMonth: number; hebrewDay: number } | null`:
   - Three PrimeNG `p-dropdown`s: **year** (gematria labels, range: current Hebrew year −2 … +5),
     **month** (12 or 13 options per leap-year formula `(7*y+1)%19 < 7`; names incl. אדר א׳=6 / אדר ב׳=7 to match .NET
     numbering; non-leap: תשרי,חשוון,כסלו,טבת,שבט,אדר,ניסן,אייר,סיוון,תמוז,אב,אלול), **day** (א׳–ל׳ gematria, 1–30;
     final existence validation is server-side).
   - "Today" default computed with `Intl.DateTimeFormat('he-u-ca-hebrew')` `formatToParts`, mapping the month **name**
     to our number arrays (avoids Intl-vs-.NET numbering mismatch).
   - Changing year resets the month if it's no longer valid (e.g. אדר ב׳ in a non-leap year). RTL-friendly.

### Phase 4 — Client: form + list display

9. `lesson-form.component.ts`: replace `p-calendar` with `<app-hebrew-date-picker formControlName="lessonDate">`;
   label `"תאריך ושעה *"` → `"תאריך *"`; init with today's Hebrew parts; edit mode patches from the response's
   `hebrewYear/Month/Day`; submit sends `{ name, subject, teacherName, hebrewYear, hebrewMonth, hebrewDay }`.
10. `lessons-list.component.ts` (~line 146 table + ~line 232 mobile card):
    `{{ lesson.lessonDateHebrew }} ({{ lesson.lessonDate | date: "dd.MM.yy" }})` — drop `HH:mm`.

## Verification

1. `dotnet build` in `server/`; run API; Swagger: POST `/api/lessons` with Hebrew components → stored `LessonDate`
   correct, response includes gematria `lessonDateHebrew`.
2. Leap year: 5784 — אדר א׳/אדר ב׳ appear in the picker and convert correctly.
3. Invalid date (e.g. day 30 in טבת) → 400 validation error with the Hebrew message.
4. Edit round-trip: open existing lesson → dropdowns show correct Hebrew date → save unchanged → `LessonDate` identical.
5. `ng build` in `client/`; manual check: RTL layout, list display at 360/768/1280.

## Decisions / non-goals

- Existing lessons in DB keep working — Hebrew fields are computed from the stored `DateTime` on read.
- Only `lessonDate`; `submittedAt`, `createdAt`, `calculatedAt` remain Gregorian everywhere.
- No time-of-day input; dates persist at midnight.
- Converter is a static pure utility (testable without DI); not registered in `DependencyInjection.cs`.
