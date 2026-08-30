export interface TestCaseDto {
  input: string | null;
  expected: string | null;
  /**
   * מקרה דוגמה — התלמידה רואה אותו. מקרה שאינו דוגמה מסונן בשרת ולא מגיע ללקוח כלל,
   * כך שברשימה שמתקבלת בתצוגת תלמידה כל הפריטים הם isSample=true.
   */
  isSample: boolean;
  /**
   * מקרה ליבה מול מקרה קצה. ⚠️ <b>שער חוסם</b>: אם מקרה ליבה אחד נכשל, נקודות הבדיקות
   * מתאפסות לגמרי — הפתרון לא עשה את הדבר המרכזי שהתרגיל ביקש. מקרי קצה נספרים
   * באופן יחסי בלבד. ר' ScoreCalculator בשרת.
   */
  isCore: boolean;
}

/**
 * קובץ מתוך הפתרון לדוגמה של המורה.
 * ⚠️ התשובה המלאה לתרגיל. השרת מרוקן את השדה הזה עבור תלמידה, ולכן בתצוגת תלמידה
 * הרשימה תמיד ריקה — אין להסתמך על הסתרה בתבנית.
 */
export interface ReferenceSolutionFileDto {
  fileName: string;
  content: string;
}

export interface ExpectedFileDto {
  fileName: string;
  description: string | null;
  methodName: string | null;
}

/**
 * אופן הבדיקה של תרגיל — נבחר ע"י המורה, קובע גם איך ה-Runner מריץ את הקוד
 * וגם אילו הנחיות התלמיד רואה במסך ההגשה.
 */
export type GradingMode = "FullProgram" | "Method" | "MultiFileMethod";

export const GRADING_MODE_LABELS_HE: Record<GradingMode, string> = {
  FullProgram: "תוכנית שלמה (עם Main)",
  Method: "מתודה בודדת",
  MultiFileMethod: "פרויקט רב־קובצי (מתודת כניסה)",
};

// ── דרישות מבניות ─────────────────────────────────────────────────────────
//
// ⚠️ אלה אינן תוספת אלא כלי הניקוד המרכזי בקורס הזה: רוב התרגילים מקודדים את הנתונים
// בתוך הקוד, ולכן יש להם מקרה בדיקה יחיד שהניקוד עליו בינארי בהכרח, והדרישות הן
// שנושאות את רוב הנקודות. הבדיקה עצמה נעשית ב-Roslyn ולא ב-AI — ולכן אותו קוד מקבל
// תמיד אותו ציון.

/** תואם ל-RuleKind בשרת. עובר כמחרוזת, בדיוק כמו GradingMode. */
export type RuleKind = "MustUse" | "MustNotUse" | "AtLeast" | "AtMost";

/** תואם ל-RuleSeverity בשרת. */
export type RuleSeverity = "Blocking" | "Scored" | "Advisory";

/** תואם ל-CodeConstruct בשרת. הוספת מבנה = ערך אחד כאן + case אחד במנתח. */
export type CodeConstruct =
  | "If"
  | "Switch"
  | "Ternary"
  | "For"
  | "While"
  | "DoWhile"
  | "Foreach"
  | "AnyLoop"
  | "Method"
  | "Recursion"
  | "Array"
  | "Matrix"
  | "List"
  | "Dictionary"
  | "BoolVariable"
  | "StringVariable"
  | "CharVariable"
  | "LocalVariable"
  | "Constant"
  | "Class"
  | "Property"
  | "Constructor"
  | "Field"
  | "Inheritance"
  | "Interface"
  | "TryCatch"
  | "Linq"
  | "Break"
  | "Continue"
  | "Goto"
  | "NestedLoopDepth";

export interface StructuralRuleDto {
  kind: RuleKind;
  construct: CodeConstruct;
  /** רלוונטי ל-AtLeast/AtMost בלבד. */
  threshold: number;
  severity: RuleSeverity;
  /** נקודות — רק ל-Scored. דרישה חוסמת היא שער ואינה נושאת ניקוד. */
  points: number;
}

export const RULE_KIND_LABELS_HE: Record<RuleKind, string> = {
  MustUse: "חובה להשתמש ב…",
  MustNotUse: "אסור להשתמש ב…",
  AtLeast: "לפחות …",
  AtMost: "לכל היותר …",
};

export const RULE_SEVERITY_LABELS_HE: Record<RuleSeverity, string> = {
  Blocking: "חוסמת",
  Scored: "מנוקדת",
  Advisory: "המלצה",
};

export const RULE_SEVERITY_HINTS_HE: Record<RuleSeverity, string> = {
  Blocking:
    "אם הדרישה לא התקיימה — אין ציון כלל וההגשה חוזרת לתלמידה. זה שער, ולכן הוא אינו נושא נקודות.",
  Scored: "אי-עמידה מפסידה את כל נקודות הדרישה. אין ניקוד חלקי.",
  Advisory: "הערה במשוב בלבד, בלי שום השפעה על הציון.",
};

/**
 * שמות המבנים בעברית. ⚠️ חייב להישאר זהה ל-StructuralRuleDescriber.ConstructName בשרת —
 * המורה בוחרת כאן ורואה את אותו ניסוח בדיוק בתוצאה. מבנים ששמם בקוד הוא מילת מפתח
 * (if, while) נשארים באנגלית, כך התלמידה כותבת אותם בפועל.
 */
export const CODE_CONSTRUCT_LABELS_HE: Record<CodeConstruct, string> = {
  If: "if",
  Switch: "switch",
  Ternary: "אופרטור תנאי (?:)",
  For: "לולאת for",
  While: "לולאת while",
  DoWhile: "לולאת do-while",
  Foreach: "לולאת foreach",
  AnyLoop: "לולאה",
  Method: "מתודה",
  Recursion: "רקורסיה",
  Array: "מערך חד-ממדי",
  Matrix: "מערך דו-ממדי",
  List: "List",
  Dictionary: "Dictionary",
  BoolVariable: "משתנה בוליאני",
  StringVariable: "משתנה מחרוזת",
  CharVariable: "משתנה תו",
  LocalVariable: "משתנה מקומי",
  Constant: "קבוע (const)",
  Class: "מחלקה",
  Property: "תכונה (property)",
  Constructor: "בנאי",
  Field: "שדה",
  Inheritance: "ירושה",
  Interface: "ממשק",
  TryCatch: "try-catch",
  Linq: "LINQ",
  Break: "break",
  Continue: "continue",
  Goto: "goto",
  NestedLoopDepth: "עומק קינון לולאות",
};

/** מקובץ לפי סדר ההוראה בכיתה ולא לפי אלפבית — הרשימה מוצגת למורה כפי שהיא. */
export const CODE_CONSTRUCT_GROUPS: {
  label: string;
  items: CodeConstruct[];
}[] = [
  { label: "תנאים", items: ["If", "Switch", "Ternary"] },
  { label: "לולאות", items: ["For", "While", "DoWhile", "Foreach", "AnyLoop"] },
  { label: "מתודות", items: ["Method", "Recursion"] },
  { label: "אוספים", items: ["Array", "Matrix", "List", "Dictionary"] },
  {
    label: "סוגי משתנים",
    items: [
      "BoolVariable",
      "StringVariable",
      "CharVariable",
      "LocalVariable",
      "Constant",
    ],
  },
  {
    label: "מונחה עצמים",
    items: [
      "Class",
      "Property",
      "Constructor",
      "Field",
      "Inheritance",
      "Interface",
    ],
  },
  { label: "מתקדם", items: ["TryCatch", "Linq"] },
  { label: "בקרת זרימה", items: ["Break", "Continue", "Goto"] },
  { label: "יעילות", items: ["NestedLoopDepth"] },
];

/**
 * בן הזוג הטבעי של דרישת "חובה".
 *
 * 🔴 הפרצה שזה סוגר: <c>while (false) { }</c> מקיים את "חובה להשתמש בלולאת while"
 * מבחינה תחבירית, והתלמידה יכולה לפתור את התרגיל ב-for לצידו. Roslyn סופר את הצומת
 * ואינו יודע אם הקוד בכלל רץ. הבדיקה "האם הקוד באמת מגיע לשם" מחוץ להיקף — הצמד הוא
 * הבקרה המעשית.
 */
export const PAIRED_CONSTRUCT: Partial<Record<CodeConstruct, CodeConstruct>> = {
  While: "For",
  For: "While",
  DoWhile: "While",
  Foreach: "For",
  Recursion: "AnyLoop",
  Switch: "If",
  If: "Switch",
  Ternary: "If",
};

/** ניסוח הדרישה בעברית. חייב להישאר זהה ל-StructuralRuleDescriber.Describe בשרת. */
export function describeRule(rule: StructuralRuleDto): string {
  const name = CODE_CONSTRUCT_LABELS_HE[rule.construct] ?? rule.construct;

  // עומק קינון אינו ספירת מופעים — "לכל היותר 2 עומק קינון לולאות" לא היה נקרא.
  if (rule.construct === "NestedLoopDepth") {
    switch (rule.kind) {
      case "AtMost":
        return `${name} לא יעלה על ${rule.threshold}`;
      case "AtLeast":
        return `${name} יהיה לפחות ${rule.threshold}`;
      case "MustNotUse":
        return "אסור לקנן לולאות";
      default:
        return `${name} לפחות 1`;
    }
  }

  // "ב-if" ולא "בif": מקף בין אות היחס למילה לועזית.
  const withPrefix = /^[A-Za-z]/.test(name) ? `ב-${name}` : `ב${name}`;

  switch (rule.kind) {
    case "MustUse":
      return `חובה להשתמש ${withPrefix}`;
    case "MustNotUse":
      return `אסור להשתמש ${withPrefix}`;
    case "AtLeast":
      return `לפחות ${rule.threshold} ${name}`;
    case "AtMost":
      return `לכל היותר ${rule.threshold} ${name}`;
    default:
      return name;
  }
}

// ── הרובריקה ──────────────────────────────────────────────────────────────
//
// ⚠️ שלוש הפונקציות הבאות הן שיקוף מדויק של AssignmentGradeability בשרת. השרת פוסל
// שמירה שאינה מסתכמת בתקרה, ולכן ולידציה שנוטה ממנו אפילו במקרה קצה אחד מייצרת 400
// שהמורה אינה יכולה להסביר לעצמה.

/** ציון מלא לתרגיל רגיל. תואם ל-Assignment.TotalPoints. */
export const TOTAL_POINTS = 100;

/** ברירת המחדל של סף ההגשה החוזרת. תואם ל-Assignment.DefaultRetryThreshold. */
export const DEFAULT_RETRY_THRESHOLD = 85;

/** סכום הנקודות של הדרישות המנוקדות. חוסמת והמלצה אינן נושאות ניקוד. */
export function scoredRulePoints(rules: StructuralRuleDto[] | null): number {
  return (rules ?? [])
    .filter((r) => r.severity === "Scored")
    .reduce((sum, r) => sum + (r.points || 0), 0);
}

/**
 * האם הרובריקה מסתכמת בתקרה בדיוק.
 * ⚠️ המקרה שקל לפספס: תרגיל בלי טסטים ובלי דרישות מנוקדות — רק חוסמות, הצורה הטבעית
 * של תרגיל מחלקות. אין למה להקצות נקודות, והוא חוקי: תלמידה שקיימה כל שער מקבלת 100.
 */
export function hasValidRubric(
  testsAllocation: number,
  testsCount: number,
  rules: StructuralRuleDto[] | null,
): boolean {
  // ⚠️ 100 גם בתרגיל בונוס: הבונוס אינו מרים את תקרת התרגיל אלא מוסיף לציון השיעור.
  const maxScore = TOTAL_POINTS;

  if (testsAllocation < 0 || testsAllocation > maxScore) return false;

  const rulesAllocation = scoredRulePoints(rules);
  const hasTests = testsCount > 0;

  if (!hasTests && rulesAllocation === 0) return true;

  const effectiveTestsAllocation = hasTests ? testsAllocation : 0;
  return effectiveTestsAllocation + rulesAllocation === maxScore;
}

/** תרגיל שאין במה לנקד אותו. "או", לא "וגם" — ר' AssignmentGradeability.IsGradeable. */
export function isGradeable(
  testsCount: number,
  rules: StructuralRuleDto[] | null,
): boolean {
  return testsCount > 0 || (rules ?? []).length > 0;
}

export interface AssignmentResponseDto {
  id: number;
  lessonId: number;
  title: string | null;
  description: string | null;
  methodName: string | null;
  gradingMode: GradingMode;
  isBonus: boolean;
  bonusValue: number;
  createdAt: string;
  submissionsCount: number;
  tests: TestCaseDto[] | null;
  expectedFiles: ExpectedFileDto[] | null;
  referenceSolution: ReferenceSolutionFileDto[] | null;
  /**
   * ⚠️ בניגוד ל-tests ול-referenceSolution, אין כאן מה להסתיר מהתלמידה: הדרישה נכתבה
   * בניסוח המטלה מלכתחילה ("חובה רקורסיה"), ובלעדיה היא לא יודעת מה נדרש ממנה.
   */
  structuralRules: StructuralRuleDto[] | null;
  /** כמה מהתקרה מוקצה למקרי הבדיקה. */
  testsAllocation: number;
  /** כמה נקודות מחולקות בין הדרישות המנוקדות. משלים לתקרה. */
  rulesAllocation: number;
  /** הציון שמתחתיו ההגשה נשארת פתוחה להגשה חוזרת. */
  retryThreshold: number;
}

export interface CreateAssignmentRequestDto {
  title: string | null;
  description: string | null;
  methodName: string | null;
  gradingMode: GradingMode;
  isBonus: boolean;
  bonusValue: number;
  tests: TestCaseDto[] | null;
  expectedFiles: ExpectedFileDto[] | null;
  referenceSolution: ReferenceSolutionFileDto[] | null;
  structuralRules: StructuralRuleDto[] | null;
  testsAllocation: number;
  retryThreshold: number;
}

export interface UpdateAssignmentRequestDto {
  title: string | null;
  description: string | null;
  methodName: string | null;
  gradingMode: GradingMode;
  isBonus: boolean;
  bonusValue: number;
  tests: TestCaseDto[] | null;
  expectedFiles: ExpectedFileDto[] | null;
  referenceSolution: ReferenceSolutionFileDto[] | null;
  structuralRules: StructuralRuleDto[] | null;
  testsAllocation: number;
  retryThreshold: number;
}

// ── בדיקת מקרי הבדיקה מול הפתרון לדוגמה ──────────────────────────────────
//
// שום דבר כאן לא נשמר בשרת: אין הגשה, אין ציון, אין קריאה ל-AI. התוצאה מתארת את
// הטיוטה שבטופס ברגע הלחיצה, ולכן היא נמחקת מהמסך ברגע שהמורה משנה מקרה בדיקה.

export interface VerifyTestCasesRequestDto {
  gradingMode: GradingMode;
  methodName: string | null;
  referenceSolution: ReferenceSolutionFileDto[];
  expectedFiles: ExpectedFileDto[];
  tests: TestCaseDto[];
}

export interface TestCaseVerificationDto {
  /** מיקום המקרה בטופס — לפיו נכתב התיקון לשורה הנכונה. */
  index: number;
  input: string;
  /** מה שהמורה הקלידה. */
  expected: string;
  /** מה שהפתרון שלה החזיר בפועל — הערך שכפתור "תיקון" כותב. */
  actual: string;
  passed: boolean;
  error: string | null;
  statusDescription: string | null;
  /** false כששגיאת ריצה החזירה פלט ריק — אז אין מה להציע לתקן אליו. */
  canFix: boolean;
}

export interface VerifyTestCasesResultDto {
  passed: number;
  total: number;
  /** ⚠️ שגיאה בפתרון של המורה עצמה — מוצגת בנפרד מרשימת המקרים. */
  hasCompileError: boolean;
  compileError: string | null;
  results: TestCaseVerificationDto[];
}

// ── הצעת מקרי בדיקה ע"י AI ────────────────────────────────────────────────

export interface SuggestTestCasesRequestDto {
  description: string;
  gradingMode: GradingMode;
  methodName: string | null;
  count: number;
  referenceSolution: ReferenceSolutionFileDto[];
  expectedFiles: ExpectedFileDto[];
}

export interface SuggestedTestCaseDto {
  input: string;
  /** הפלט שייכתב לטופס — התוצאה שרצה בפועל כשיש פתרון לדוגמה. */
  expected: string;
  /** מה שהמודל הציע. שונה מ-expected רק כשהם חלקו. */
  aiExpected: string;
  why: string | null;
  isCore: boolean;
  verified: boolean;
  /** המודל והפתרון חלקו. הפתרון ניצח, והמחלוקת מוצגת ולא נבלעת. */
  disagreed: boolean;
  verificationError: string | null;
}

export interface SuggestTestCasesResultDto {
  cases: SuggestedTestCaseDto[];
  verified: boolean;
  warning: string | null;
}
