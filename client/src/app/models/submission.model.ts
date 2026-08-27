export type SubmissionStatus =
  | "PendingAi"
  | "ProcessingAi"
  | "Done"
  | "AiFailed"
  | "CompilationFailed"
  | "JudgeUnavailable"
  | "RequirementsNotMet";

export const STATUS_LABELS_HE: Record<string, string> = {
  PendingAi: "ממתין לבדיקה",
  ProcessingAi: "בבדיקה...",
  Done: "נבדק",
  AiFailed: "שגיאת בדיקה",
  CompilationFailed: "שגיאת קומפילציה",
  JudgeUnavailable: "תקלה במערכת הבדיקה",
  // ⚠️ לא "ציון נמוך": דרישה חוסמת שלא התקיימה היא דחייה. התרגיל דרש רקורסיה והתלמידה
  // כתבה לולאה — כלומר לא עשתה את מה שהתבקשה, ואין ציון כלל.
  RequirementsNotMet: "הדרישות לא התקיימו",
};

/** חומרת התגית, בשמות ש-PrimeNG מכיר. */
export type SubmissionStatusSeverity =
  | "success"
  | "info"
  | "warning"
  | "danger"
  | "secondary";

export interface SubmissionStatusPresentation {
  label: string;
  severity: SubmissionStatusSeverity;
  icon: string;
}

/**
 * המראה של כל אחד משבעת הסטטוסים — תווית, חומרה ואייקון — במקום אחד.
 *
 * ⚠️ נגזר מהטבלה ב-docs/design-system.md, וזו הטבלה שנבדקת מול ה-enum. סטטוס = צבע
 * **וגם** אייקון **וגם** תווית, לעולם לא צבע לבדו (D-9).
 *
 * 🔴 המיפוי הזה היה כתוב חמש פעמים בנפרד, ואחד מהעותקים היה שגוי: submissions-list גזרה
 * חומרה לפי התאמת תת-מחרוזת, ו-"judgeunavailable" אינו מכיל fail/error/pending, ולכן נפל
 * לברירת המחדל "info". תקלת תשתית הוצגה כתגית מידע ניטרלית במסך המורה, בעוד שבכל מסך אחר
 * היא ענבר. ההערה מעל אותו קוד כבר הזהירה מהכשל הזה עבור RequirementsNotMet, שקיבל טיפול
 * מיוחד — המקרה השני פוספס. **מיפוי מפורש ולא נגזר: אין כאן תת-מחרוזות.**
 */
export const STATUS_PRESENTATION: Record<
  SubmissionStatus,
  SubmissionStatusPresentation
> = {
  PendingAi: {
    label: STATUS_LABELS_HE["PendingAi"],
    severity: "warning",
    icon: "pi pi-clock",
  },
  ProcessingAi: {
    label: STATUS_LABELS_HE["ProcessingAi"],
    severity: "info",
    icon: "pi pi-spinner",
  },
  Done: {
    label: STATUS_LABELS_HE["Done"],
    severity: "success",
    icon: "pi pi-check-circle",
  },
  AiFailed: {
    label: STATUS_LABELS_HE["AiFailed"],
    severity: "danger",
    icon: "pi pi-exclamation-triangle",
  },
  CompilationFailed: {
    label: STATUS_LABELS_HE["CompilationFailed"],
    severity: "danger",
    icon: "pi pi-exclamation-triangle",
  },
  // ⚠️ ענבר ולא אדום: תקלת תשתית. התלמידה לא עשתה שום דבר רע ואין לה מה לתקן, ואדום היה
  // שולח אותה לנפות באג שאין לה.
  JudgeUnavailable: {
    label: STATUS_LABELS_HE["JudgeUnavailable"],
    severity: "warning",
    icon: "pi pi-exclamation-circle",
  },
  // ⚠️ pi-ban ולא משולש השגיאה המשותף: הקוד רץ מצוין, הוא פשוט לא עשה את מה שהתרגיל ביקש.
  // אייקון של תקלה היה קורא כמו תקלה.
  RequirementsNotMet: {
    label: STATUS_LABELS_HE["RequirementsNotMet"],
    severity: "danger",
    icon: "pi pi-ban",
  },
};

/** ברירת מחדל להגשה שעדיין לא קיימת, או לסטטוס שאינו מוכר. */
export const NO_SUBMISSION_PRESENTATION: SubmissionStatusPresentation = {
  label: "טרם הוגש",
  severity: "secondary",
  icon: "pi pi-minus-circle",
};

/** המראה של סטטוס, בלי להניח שהוא מוכר. */
export function statusPresentation(
  status?: string | null,
): SubmissionStatusPresentation {
  if (!status) return NO_SUBMISSION_PRESENTATION;
  return (
    STATUS_PRESENTATION[status as SubmissionStatus] ?? NO_SUBMISSION_PRESENTATION
  );
}

/**
 * כל כמה זמן מסך פרטים מרענן הגשה שנמצאת ב-PendingAi/ProcessingAi.
 *
 * ⚠️ מספר אחד, ובמכוון לא שניים. קודם התלמידה רועננה כל 5 שניות והמורה כל 7 — אותה
 * המתנה בדיוק, לאותה הגשה, בשני קצבים שאיש לא בחר. שני מסכים שמחזיקים כל אחד את המספר
 * שלו הם גם שני מסכים שיסתרו זה את זה בפעם הבאה שמישהי תכוונן אחד מהם.
 */
export const SUBMISSION_POLL_INTERVAL_MS = 5000;

export interface SubmissionFileDto {
  fileName: string;
  content: string;
}

export interface TestCaseResultDto {
  input: string;
  expected: string;
  actual: string;
  passed: boolean;
  error: string | null;
  /** "Wrong Answer" מול "Runtime Error (SIGSEGV)" — מבדיל תשובה שגויה מקריסה. */
  statusDescription: string | null;
  /** נשמר בזמן הבדיקה. false = מקרה מוסתר. */
  isSample: boolean;
  /**
   * true כשהשרת ריקן את פרטי השורה עבור הקורא הנוכחי (מקרה מוסתר, תצוגת תלמידה).
   * במצב הזה input/expected/actual/error ריקים ורק passed אמיתי.
   */
  isHidden: boolean;
}

export interface AiFeedbackIssuesDto {
  correctness: string[];
  readability: string[];
  performance: string[];
}

/**
 * המשוב המילולי.
 *
 * 🔴 <b>אין כאן ציונים.</b> השדות scores (ארבעה מספרים שהמודל ניקד בעצמו) ו-
 * optionalFullSolution (הפתרון המלא, שנמסר לתלמידה) הוסרו מהשרת: את הציון קובעים Roslyn,
 * מריץ הקוד ו-ScoreCalculator, והפירוק האמיתי חוזר ב-SubmissionResponseDto.scoreBreakdown.
 */
export interface AiFeedbackResultDto {
  good: string[];
  issues: AiFeedbackIssuesDto;
  minimalChanges: string[];
  /** false כאשר תשובת ה-AI לא פורשה בהצלחה — יש להציג rawResponse כטקסט גולמי */
  parseSucceeded: boolean;
  rawResponse: string | null;
}

/**
 * פירוק הציון כפי שהוא מוצג — "בדיקות 64 · דרישות 0 · סה"כ 64".
 * כל מספר כאן הוא תוצאה של חישוב דטרמיניסטי שאפשר לשחזר, ולכן אפשר גם להתווכח איתו
 * מול הקוד. זה מה שהחליף את ארבעת אריחי הציון של ה-AI.
 */
export interface ScoreBreakdownDto {
  testPoints: number;
  rulePoints: number;
  total: number;
  testsAllocation: number;
  rulesAllocation: number;
  passedTests: number;
  totalTests: number;
  /**
   * ⚠️ false מאפס את נקודות הבדיקות לגמרי: מקרה ליבה שנכשל אומר שהפתרון לא עשה את
   * הדבר המרכזי, ומקרי קצה שעברו במקרה אינם מזכים בנקודות.
   */
  allCorePassed: boolean;
}

/** תוצאת דרישה מבנית אחת. הניסוח העברי נבנה בשרת (StructuralRuleDescriber) ולא כאן. */
export interface StructuralRuleResultDto {
  /** "חובה להשתמש ברקורסיה", "לכל היותר 3 if". */
  requirement: string;
  /** "לא נמצאה רקורסיה בקוד", "נמצאו 4 מופעים של if (בשורות 3, 7)". */
  finding: string;
  passed: boolean;
  severity: "Blocking" | "Scored" | "Advisory";
  points: number;
  expectedCount: number;
  actualCount: number;
  lines: number[];
}

/**
 * ניסיון הגשה קודם — שורה בציר "ניסיון 1: 40 · ניסיון 2: 78".
 * ⚠️ רק הניסיון האחרון נחשב כציון; השורות כאן אינן נכנסות לשום ממוצע.
 */
export interface SubmissionAttemptDto {
  attemptNumber: number;
  score: number | null;
  status: SubmissionStatus | string;
  submittedAt: string;
  gradedAt: string | null;
  /** התוכן הכבד נגזם — נשמרים 10 הניסיונות האחרונים במלואם. */
  isCollapsed: boolean;
}

export interface SubmissionResponseDto {
  id: number;
  studentId: number;
  assignmentId: number;
  /** השיעור שמתחת לתרגיל — מגיע מהשרת, אין צורך לגרור אותו ב-queryParams. */
  lessonId: number;
  sourceCode: string | null;
  sourceFiles: SubmissionFileDto[];
  score: number | null;
  /** null כשאין ציון — למשל דרישה חוסמת שלא התקיימה, או הגשה שנבדקה לפני מנוע הרובריקה. */
  scoreBreakdown: ScoreBreakdownDto | null;
  feedback: AiFeedbackResultDto | null;
  testResults: TestCaseResultDto[];
  structuralResults: StructuralRuleResultDto[];
  /**
   * האם ההגשה פתוחה כרגע להגשה חוזרת. ⚠️ מחושב בשרת ולא בקליינט: הכלל מערב את סף הציון
   * של התרגיל, נעילת השיעור לתלמידה ואישור חד-פעמי של המורה.
   */
  canResubmit: boolean;
  /**
   * ההסבר לנעילה סופית — שיעור שסוכם לתלמידה, או כיתה בארכיון. null כשאינה נעולה.
   *
   * ⚠️ נעילה גוברת גם על canResubmit וגם על אישור המורה. כשיש כאן טקסט אין להציע
   * "אישור הגשה נוספת" — הוא לא יפתח דבר. חסימה בגלל סף הציון (lockReason === null
   * ו-canResubmit === false) המורה *כן* יכולה לעקוף.
   */
  lockReason: string | null;
  /** מספר הניסיון הנוכחי, החל מ-1. */
  attemptNumber: number;
  /** הניסיונות הקודמים, מהחדש לישן. הנוכחי אינו כאן. */
  attempts: SubmissionAttemptDto[];
  /** המורה אישרה ניסיון נוסף שטרם נוצל. גובר על סף הציון. */
  hasUnusedExtraAttempt: boolean;
  /** הסיבה שהמורה ציינה לאישור — יומן הביקורת שמחליף "מי השתמשה בקוד". */
  extraAttemptReason: string | null;
  /** הסיבה שהמורה ציינה לדריסת הציון. null כשהציון מחושב. */
  scoreOverrideReason: string | null;
  status: SubmissionStatus | null;
  aiError: string | null;
  compileError: string | null;
  submittedAt: string;
  studentName: string | null;
  assignmentName: string | null;
}

// ── דריסות המורה ──────────────────────────────────────────────────────────
//
// ⚠️ אין כאן מזהה מורה בכוונה — הוא נלקח מה-claims של הטוקן בבקר. שדה בגוף הבקשה היה
// הופך את יומן הביקורת לדיווח עצמי, כלומר לחסר ערך.

export interface GrantExtraAttemptRequestDto {
  /** חובה. זה מה שמחליף את "לראות מי השתמשה בקוד". */
  reason: string;
}

export interface OverrideScoreRequestDto {
  score: number;
  reason: string;
}

export interface CreateSubmissionRequestDto {
  assignmentId: number;
  sourceCode: string | null;
  files: SubmissionFileDto[] | null;
}

export interface UpdateSubmissionRequestDto {
  sourceCode: string | null;
  /** null בהגשה חד-קובצית. בתרגיל רב-קובצי חובה לשלוח את כל הקבצים הצפויים. */
  files: SubmissionFileDto[] | null;
}
