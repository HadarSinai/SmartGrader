// עזרי שנה עברית: שנה נוכחית + פורמט גימטריה (5786 → תשפ"ו)

export function getCurrentHebrewYear(): number {
  const parts = new Intl.DateTimeFormat("en-u-ca-hebrew", {
    year: "numeric",
  }).formatToParts(new Date());
  const year = parts.find((p) => p.type === "year")?.value ?? "";
  return parseInt(year, 10);
}

const ONES = ["", "א", "ב", "ג", "ד", "ה", "ו", "ז", "ח", "ט"];
const TENS = ["", "י", "כ", "ל", "מ", "נ", "ס", "ע", "פ", "צ"];
const HUNDREDS = ["", "ק", "ר", "ש", "ת"];

export function hebrewYearToGematria(year: number): string {
  // משמיטים את האלפים (5786 → 786), כמקובל בכתיבת שנה
  let n = year % 1000;
  let result = "";

  while (n >= 100) {
    const h = Math.min(Math.floor(n / 100), 4);
    result += HUNDREDS[h];
    n -= h * 100;
  }

  // ט"ו וט"ז במקום י-ה / י-ו
  if (n === 15) {
    result += "טו";
    n = 0;
  } else if (n === 16) {
    result += "טז";
    n = 0;
  } else {
    result += TENS[Math.floor(n / 10)];
    result += ONES[n % 10];
  }

  // גרשיים לפני האות האחרונה (או גרש לאות בודדת)
  if (result.length === 1) return `${result}׳`;
  return `${result.slice(0, -1)}"${result.slice(-1)}`;
}
