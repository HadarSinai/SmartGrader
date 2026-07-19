---
name: backend-hebrew-calendar-pattern
description: 'Use when converting, validating, or formatting Hebrew dates in the SmartGrader .NET backend with System.Globalization.HebrewCalendar: Hebrew→Gregorian conversion, gematria formatting (י"ד תמוז תשפ"ו), .NET month numbering (Tishrei=1, Adar I=6/Adar II=7), leap-year logic, or the static HebrewDateConverter utility. USE FOR: ''convert a Hebrew date'', ''store lessonDate from Hebrew components'', ''format a DateTime as a Hebrew date string'', ''validate hebrewYear/hebrewMonth/hebrewDay''. NOT for the Angular picker UI (see client-cva-form-control-pattern) or generic AutoMapper profiles (see backend-automapper-profile-pattern).'
---

# Backend Hebrew Calendar Pattern

All Hebrew-date logic on the server uses `System.Globalization.HebrewCalendar` — **built into .NET, zero NuGet packages**. The DB schema never changes: entities keep plain `DateTime`, and conversion happens at the mapping boundary via a pure static utility.

## When to Use

- Implementing or editing `server/Application/Common/HebrewDate/HebrewDateConverter.cs`.
- Mapping `HebrewYear/HebrewMonth/HebrewDay` request fields to a stored `DateTime` (and back) in an AutoMapper profile.
- Writing FluentValidation rules for Hebrew date components.
- Formatting a `DateTime` as a gematria string (e.g. `י"ד תמוז תשפ"ו`) for a Response DTO.

## Critical .NET Facts (get these wrong and dates silently shift)

1. **Month numbering — Tishrei = 1.** NOT Nisan. Order: תשרי=1, חשוון=2, כסלו=3, טבת=4, שבט=5, …
2. **Leap year has 13 months:** אדר א׳ = **6**, אדר ב׳ = **7**, so ניסן=8 … אלול=13.
   **Non-leap year has 12 months:** אדר = **6**, ניסן=7 … אלול=12.
   ⚠️ The same month name maps to a _different number_ depending on the year — never hardcode a name→number table without the year.
3. **Leap-year formula:** `(7 * hebrewYear + 1) % 19 < 7` (or use `calendar.IsLeapYear(y)` — prefer the built-in).
4. **Gematria formatting is native** — no manual gematria code:

```csharp
var ci = new CultureInfo("he-IL");
ci.DateTimeFormat.Calendar = new HebrewCalendar();
string s = date.ToString("dd MMMM yyyy", ci); // → י"ד תמוז תשפ"ו
```

5. **`HebrewCalendar` supported range** is Hebrew years 5343–5999; validate the year before calling `ToDateTime` or it throws `ArgumentOutOfRangeException`.

## The Static Utility Pattern

`server/Application/Common/HebrewDate/HebrewDateConverter.cs`, namespace `SmartGrader.Application.Common.HebrewDate`. **Pure static class — no DI registration, nothing in `DependencyInjection.cs`.** Callable from AutoMapper `MapFrom` lambdas and validators.

```csharp
public static class HebrewDateConverter
{
    private static readonly HebrewCalendar Calendar = new();

    public static DateTime ToGregorian(int hebrewYear, int hebrewMonth, int hebrewDay)
        => Calendar.ToDateTime(hebrewYear, hebrewMonth, hebrewDay, 0, 0, 0, 0); // midnight

    public static (int Year, int Month, int Day) GetHebrewParts(DateTime date)
        => (Calendar.GetYear(date), Calendar.GetMonth(date), Calendar.GetDayOfMonth(date));

    public static string ToHebrewString(DateTime date)
    {
        var ci = new CultureInfo("he-IL");
        ci.DateTimeFormat.Calendar = new HebrewCalendar();
        return date.ToString("dd MMMM yyyy", ci);
    }

    public static bool IsValidHebrewDate(int year, int month, int day)
    {
        if (year < 5343 || year > 5999) return false;          // HebrewCalendar supported range
        if (month < 1 || month > Calendar.GetMonthsInYear(year)) return false;
        return day >= 1 && day <= Calendar.GetDaysInMonth(year, month);
    }
}
```

## Mapping Boundary (handlers stay untouched)

All conversion lives in the AutoMapper profile — follow [backend-automapper-profile-pattern](../backend-automapper-profile-pattern/SKILL.md) for profile structure:

```csharp
// Request DTO (HebrewYear/Month/Day) → Entity (DateTime)
CreateMap<CreateLessonRequestDto, Lesson>()
    .ForMember(d => d.LessonDate,
        opt => opt.MapFrom(s => HebrewDateConverter.ToGregorian(s.HebrewYear, s.HebrewMonth, s.HebrewDay)));

// Entity (DateTime) → Response DTO (ISO + gematria + parts)
CreateMap<Lesson, LessonResponseDto>()
    .ForMember(d => d.LessonDateHebrew, opt => opt.MapFrom(s => HebrewDateConverter.ToHebrewString(s.LessonDate)))
    .ForMember(d => d.HebrewYear,  opt => opt.MapFrom(s => HebrewDateConverter.GetHebrewParts(s.LessonDate).Year))
    .ForMember(d => d.HebrewMonth, opt => opt.MapFrom(s => HebrewDateConverter.GetHebrewParts(s.LessonDate).Month))
    .ForMember(d => d.HebrewDay,   opt => opt.MapFrom(s => HebrewDateConverter.GetHebrewParts(s.LessonDate).Day));
```

## Validation Pattern (FluentValidation)

```csharp
RuleFor(x => x.Dto.HebrewYear).InclusiveBetween(5000, 6000)
    .WithMessage("שנה עברית לא תקינה");
RuleFor(x => x.Dto.HebrewMonth).InclusiveBetween(1, 13);
RuleFor(x => x.Dto.HebrewDay).InclusiveBetween(1, 30);
RuleFor(x => x.Dto)
    .Must(d => HebrewDateConverter.IsValidHebrewDate(d.HebrewYear, d.HebrewMonth, d.HebrewDay))
    .WithMessage("התאריך העברי אינו קיים");
```

The `Must` on the whole tuple catches real-calendar failures the range checks miss: month 13 in a non-leap year, day 30 in a 29-day month (טבת, אדר…).

## Pitfalls

- **Never assume Nisan=1** — .NET uses Tishrei=1. Cross-check any month-name table against a known date before trusting it.
- **Intl (JS) vs .NET numbering differ** — never send a month _number_ produced by `Intl.DateTimeFormat('he-u-ca-hebrew')` to the server. The client must map Intl month **names** to the .NET-numbered arrays.
- **Don't reformat gematria manually** — the `he-IL` + `HebrewCalendar` culture trick handles geresh/gershayim correctly.
- **Store midnight** — `ToDateTime(..., 0,0,0,0)`. No time-of-day component for Hebrew-date fields.
- **No DI, no migration** — the converter is static; the entity keeps `DateTime`; SQLite TEXT storage is unchanged.

## See Also

- [backend-automapper-profile-pattern](../backend-automapper-profile-pattern/SKILL.md) — profile structure and Ignore rules.
- [client-cva-form-control-pattern](../client-cva-form-control-pattern/SKILL.md) — the Angular Hebrew-date picker that produces the `hebrewYear/Month/Day` payload.
