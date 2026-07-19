import { Directive, HostListener, Optional, Self } from "@angular/core";
import { NgControl } from "@angular/forms";

const HAS_HEBREW = /[\u0590-\u05FF]/;
const HEBREW_CHARS = /[\u0590-\u05FF]/g;

/**
 * חוסם הזנת תווים בעברית בשדה קלט (הקלדה, הדבקה, גרירה, autofill).
 * עובד גם על p-password — אירועי input עולים ב-bubbling מה-input הפנימי.
 */
@Directive({
  selector: "[sgNoHebrew]",
  standalone: true,
})
export class NoHebrewDirective {
  constructor(@Optional() @Self() private readonly ngControl: NgControl) {}

  @HostListener("input", ["$event"])
  onInput(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    if (!input || typeof input.value !== "string") {
      return;
    }

    const value = input.value;
    if (!HAS_HEBREW.test(value)) {
      return;
    }

    const caret = input.selectionStart ?? value.length;
    const removedBeforeCaret = (value.slice(0, caret).match(HEBREW_CHARS) ?? [])
      .length;
    const clean = value.replace(HEBREW_CHARS, "");

    input.value = clean;
    const newCaret = caret - removedBeforeCaret;
    input.setSelectionRange(newCaret, newCaret);

    this.ngControl?.control?.setValue(clean);
  }
}
