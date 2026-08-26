import { Injectable } from '@angular/core';

/**
 * העדפות הנגישות של המשתמשת.
 *
 * ⚠️ זה המקום היחיד שבו הן נשמרות. קודם היו כאן שני מנגנונים במקביל: השירות הזה שמר JSON
 * תחת מפתח אחד, והווידג'ט שמר בנוסף מפתחות גולמיים (`theme`, `fontScale`, `reduceMotion`)
 * והחיל מחלקות משלו. reduceMotion חי בשניהם — ולכן "איפוס" ניקה עותק אחד בזמן שהשני
 * חזר בטעינה הבאה, כי main.ts קרא דווקא את הגולמי.
 */
export interface AccessibilityState {
  /** מכפיל גודל הטקסט. 1 = ברירת מחדל. */
  scale: number;
  dark: boolean;
  highContrast: boolean;
  highlightLinks: boolean;
  reduceMotion: boolean;
}

export const ACCESSIBILITY_KEY = 'sg_accessibility';

/**
 * ⚠️ "טקסט גדול" הוא נקודה על אותו סרגל ולא מנגנון שני. קודם המתג שינה את
 * `documentElement.fontSize` והמחוון את `body.fontSize`, כך שהם הצטברפו זה על זה
 * ושתי פקדים שונים שלטו באותו דבר.
 */
export const LARGE_TEXT_SCALE = 1.15;

export const DEFAULT_ACCESSIBILITY_STATE: AccessibilityState = {
  scale: 1,
  dark: false,
  highContrast: false,
  highlightLinks: false,
  reduceMotion: false,
};

/**
 * קורא את ההעדפות השמורות. לא זורק — אחסון חסום או JSON פגום מחזירים ברירת מחדל.
 *
 * ⚠️ מיוצא כפונקציה חופשית ולא רק כמתודה של השירות, כי main.ts חייב להחיל את ההעדפות
 * לפני ש-Angular עולה (אחרת יש הבהוב ערכת נושא), ובשלב הזה אין עדיין DI.
 */
export function readAccessibilityState(): AccessibilityState {
  try {
    const saved = localStorage.getItem(ACCESSIBILITY_KEY);
    if (!saved) return { ...DEFAULT_ACCESSIBILITY_STATE };
    return { ...DEFAULT_ACCESSIBILITY_STATE, ...JSON.parse(saved) };
  } catch {
    return { ...DEFAULT_ACCESSIBILITY_STATE };
  }
}

/**
 * מחיל את ההעדפות על ה-DOM. מקור יחיד לשמות המחלקות — main.ts והשירות קוראים לו,
 * ואף אחד מהם לא מחזיק רשימה משלו.
 */
export function applyAccessibilityState(state: AccessibilityState): void {
  document.body.classList.toggle('dark', state.dark);
  document.body.classList.toggle('a11y-contrast', state.highContrast);
  document.body.classList.toggle('a11y-links', state.highlightLinks);
  document.body.classList.toggle('a11y-reduce-motion', state.reduceMotion);
  document.body.style.fontSize = `${state.scale * 100}%`;
}

@Injectable({ providedIn: 'root' })
export class AccessibilityService {
  state: AccessibilityState = { ...DEFAULT_ACCESSIBILITY_STATE };

  init() {
    this.state = readAccessibilityState();
    this.apply();
  }

  update(patch: Partial<AccessibilityState>) {
    this.state = { ...this.state, ...patch };
    try {
      localStorage.setItem(ACCESSIBILITY_KEY, JSON.stringify(this.state));
    } catch {}
    this.apply();
  }

  /** מחזיר את ההעדפות לברירת המחדל, בעותק היחיד שקיים. */
  reset() {
    this.update({ ...DEFAULT_ACCESSIBILITY_STATE });
  }

  get largeText(): boolean {
    return this.state.scale >= LARGE_TEXT_SCALE;
  }

  private apply() {
    applyAccessibilityState(this.state);
  }
}
