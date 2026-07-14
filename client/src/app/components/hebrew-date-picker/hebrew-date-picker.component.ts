import { CommonModule } from "@angular/common";
import { Component, forwardRef } from "@angular/core";
import {
    ControlValueAccessor,
    FormsModule,
    NG_VALUE_ACCESSOR,
} from "@angular/forms";
import { DropdownModule } from "primeng/dropdown";

export interface HebrewDateValue {
  hebrewYear: number;
  hebrewMonth: number;
  hebrewDay: number;
}

interface DropdownOption {
  label: string;
  value: number;
}

// .NET HebrewCalendar month numbering: Tishrei = 1.
// Non-leap year: Adar = 6; leap year: Adar I = 6, Adar II = 7.
const NON_LEAP_MONTHS: string[] = [
  "תשרי",
  "חשוון",
  "כסלו",
  "טבת",
  "שבט",
  "אדר",
  "ניסן",
  "אייר",
  "סיוון",
  "תמוז",
  "אב",
  "אלול",
];

const LEAP_MONTHS: string[] = [
  "תשרי",
  "חשוון",
  "כסלו",
  "טבת",
  "שבט",
  "אדר א׳",
  "אדר ב׳",
  "ניסן",
  "אייר",
  "סיוון",
  "תמוז",
  "אב",
  "אלול",
];

const GEMATRIA_UNITS = ["", "א", "ב", "ג", "ד", "ה", "ו", "ז", "ח", "ט"];
const GEMATRIA_TENS = ["", "י", "כ", "ל", "מ", "נ", "ס", "ע", "פ", "צ"];
const GEMATRIA_HUNDREDS = ["", "ק", "ר", "ש", "ת"];

function isHebrewLeapYear(year: number): boolean {
  return (7 * year + 1) % 19 < 7;
}

function gematriaLetters(n: number): string {
  let letters = "";
  let hundreds = Math.floor(n / 100);
  while (hundreds > 4) {
    letters += "ת";
    hundreds -= 4;
  }
  letters += GEMATRIA_HUNDREDS[hundreds];
  const rem = n % 100;
  if (rem === 15) {
    letters += "טו";
  } else if (rem === 16) {
    letters += "טז";
  } else {
    letters += GEMATRIA_TENS[Math.floor(rem / 10)];
    letters += GEMATRIA_UNITS[rem % 10];
  }
  return letters;
}

function withGeresh(letters: string): string {
  if (letters.length === 1) {
    return letters + "׳";
  }
  return letters.slice(0, -1) + "״" + letters.slice(-1);
}

function gematriaYear(year: number): string {
  return withGeresh(gematriaLetters(year % 1000));
}

function gematriaDay(day: number): string {
  return withGeresh(gematriaLetters(day));
}

function normalizeMonthName(name: string): string {
  let n = name.replace(/[׳״'"]/g, "").trim();
  if (n === "חשון" || n === "מרחשוון" || n === "מרחשון") {
    n = "חשוון";
  }
  if (n === "סיון") {
    n = "סיוון";
  }
  if (n === "אדר I") {
    n = "אדר א";
  }
  if (n === "אדר II") {
    n = "אדר ב";
  }
  return n;
}

/**
 * Current Hebrew year via Intl (latin digits so the year is parseable).
 * Intl is used ONLY for "today"; month numbers are never taken from Intl.
 */
function getCurrentHebrewYear(): number {
  try {
    const parts = new Intl.DateTimeFormat("he-u-ca-hebrew-nu-latn", {
      year: "numeric",
    }).formatToParts(new Date());
    const yearPart = parts.find((p) => p.type === "year");
    if (yearPart) {
      const year = parseInt(yearPart.value, 10);
      if (!isNaN(year)) {
        return year;
      }
    }
  } catch {
    // fall through to approximation
  }
  return new Date().getFullYear() + 3761; // rough fallback
}

/**
 * Today's Hebrew date as .NET-numbered components. The Intl month NAME
 * (never Intl's month number) is mapped onto the .NET-numbered arrays.
 */
export function getHebrewToday(): HebrewDateValue | null {
  try {
    const parts = new Intl.DateTimeFormat("he-u-ca-hebrew-nu-latn", {
      year: "numeric",
      month: "long",
      day: "numeric",
    }).formatToParts(new Date());

    const yearPart = parts.find((p) => p.type === "year");
    const monthPart = parts.find((p) => p.type === "month");
    const dayPart = parts.find((p) => p.type === "day");
    if (!yearPart || !monthPart || !dayPart) {
      return null;
    }

    const hebrewYear = parseInt(yearPart.value, 10);
    const hebrewDay = parseInt(dayPart.value, 10);
    if (isNaN(hebrewYear) || isNaN(hebrewDay)) {
      return null;
    }

    const names = isHebrewLeapYear(hebrewYear) ? LEAP_MONTHS : NON_LEAP_MONTHS;
    const normalized = normalizeMonthName(monthPart.value);
    const index = names.findIndex(
      (name) => normalizeMonthName(name) === normalized,
    );
    if (index < 0) {
      return null;
    }

    return { hebrewYear, hebrewMonth: index + 1, hebrewDay };
  } catch {
    return null;
  }
}

@Component({
  selector: "app-hebrew-date-picker",
  standalone: true,
  imports: [CommonModule, FormsModule, DropdownModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => HebrewDatePickerComponent),
      multi: true,
    },
  ],
  template: `
    <div class="hdp" dir="rtl">
      <p-dropdown
        class="hdp__part"
        styleClass="w-full"
        [options]="dayOptions"
        [(ngModel)]="day"
        (onChange)="onDayChange()"
        [disabled]="disabled"
        placeholder="יום"
        ariaLabel="יום"
        appendTo="body"
      ></p-dropdown>
      <p-dropdown
        class="hdp__part hdp__month"
        styleClass="w-full"
        [options]="monthOptions"
        [(ngModel)]="month"
        (onChange)="onMonthChange()"
        [disabled]="disabled"
        placeholder="חודש"
        ariaLabel="חודש"
        appendTo="body"
      ></p-dropdown>
      <p-dropdown
        class="hdp__part"
        styleClass="w-full"
        [options]="yearOptions"
        [(ngModel)]="year"
        (onChange)="onYearChange()"
        [disabled]="disabled"
        placeholder="שנה"
        ariaLabel="שנה"
        appendTo="body"
      ></p-dropdown>
    </div>
  `,
  styles: [
    `
      .hdp {
        display: flex;
        gap: 0.5rem;
      }
      .hdp__part {
        flex: 1 1 0;
        min-width: 0;
      }
      .hdp__month {
        flex: 1.4 1 0;
      }
    `,
  ],
})
export class HebrewDatePickerComponent implements ControlValueAccessor {
  readonly yearOptions: DropdownOption[];
  monthOptions: DropdownOption[] = [];
  readonly dayOptions: DropdownOption[];

  year: number | null = null;
  month: number | null = null;
  day: number | null = null;
  disabled = false;

  private onChange: (value: HebrewDateValue | null) => void = () => {};
  private onTouched: () => void = () => {};

  constructor() {
    const currentYear = getCurrentHebrewYear();
    this.yearOptions = [];
    for (let y = currentYear - 2; y <= currentYear + 5; y++) {
      this.yearOptions.push({ label: gematriaYear(y), value: y });
    }

    this.dayOptions = [];
    for (let d = 1; d <= 30; d++) {
      this.dayOptions.push({ label: gematriaDay(d), value: d });
    }

    this.monthOptions = this.buildMonthOptions(currentYear);
  }

  writeValue(value: HebrewDateValue | null): void {
    if (value) {
      this.year = value.hebrewYear;
      this.month = value.hebrewMonth;
      this.day = value.hebrewDay;
    } else {
      this.year = null;
      this.month = null;
      this.day = null;
    }
    this.monthOptions = this.buildMonthOptions(
      this.year ?? getCurrentHebrewYear(),
    );
  }

  registerOnChange(fn: (value: HebrewDateValue | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  onYearChange(): void {
    if (this.year !== null) {
      this.monthOptions = this.buildMonthOptions(this.year);
      // Reset the month when it's no longer valid (e.g. אדר ב׳ in a non-leap year).
      if (this.month !== null && this.month > this.monthOptions.length) {
        this.month = null;
      }
    }
    this.emit();
  }

  onMonthChange(): void {
    this.emit();
  }

  onDayChange(): void {
    this.emit();
  }

  private buildMonthOptions(year: number): DropdownOption[] {
    const names = isHebrewLeapYear(year) ? LEAP_MONTHS : NON_LEAP_MONTHS;
    return names.map((name, i) => ({ label: name, value: i + 1 }));
  }

  private emit(): void {
    if (this.year !== null && this.month !== null && this.day !== null) {
      this.onChange({
        hebrewYear: this.year,
        hebrewMonth: this.month,
        hebrewDay: this.day,
      });
    } else {
      this.onChange(null);
    }
    this.onTouched();
  }
}
