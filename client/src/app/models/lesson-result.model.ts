export interface LessonResultResponseDto {
  id: number;
  studentId: number;
  lessonId: number;
  finalScore: number | null;
  /** מה שהמערכת חישבה. נשמר גם כשהמורה דרסה — כך אפשר לדעת ממה חרגו. */
  computedScore: number | null;
  isComplete: boolean;
  calculatedAt: string;
  totalAssignments: number;
  completedAssignments: number;

  // ── יומן הביקורת של דריסת הציון הסופי ──
  isFinalScoreOverridden: boolean;
  finalScoreOverriddenByUserId: number | null;
  finalScoreOverriddenAt: string | null;
  finalScoreOverrideReason: string | null;
}

/**
 * ⚠️ finalScore כאן הוא **בקשת דריסה, לא הציון**. השרת גוזר את הציון הסופי מההגשות
 * בשיעור; הערך הזה נכנס לתוקף רק כשהוא שונה מהמחושב, ואז overrideReason הוא חובה.
 * null = "קבעי את מה שחושב".
 *
 * ⚠️ hasBonus הוסר: הוא נשלח מכאן וקבע את תקרת הציון (150 במקום 100), כלומר הדפדפן
 * קבע לעצמו את הטווח החוקי. התקרה נגזרת עכשיו מהתרגילים בפועל בשיעור.
 */
export interface CompleteLessonRequestDto {
  studentId: number;
  lessonId: number;
  finalScore: number | null;
  overrideReason: string | null;
}

// ── הצעת הציון הסופי ──────────────────────────────────────────────────────
//
// 🔴 הפער שזה סוגר: המערכת חישבה ציון לכל הגשה, והוא נעצר בהגשה ולא הגיע לציון השיעור.
// המורה חישבה ממוצע ביד, לכל תלמידה, בזמן שכל המספרים כבר היו במערכת.

export interface AssignmentScoreDto {
  assignmentId: number;
  title: string | null;
  /** null = לא נבדק, ולכן אינו נכנס לממוצע. */
  score: number | null;
  /** סטטוס ההגשה, כדי שהדיאלוג יסביר *למה* אין ציון. */
  status: string;
  /**
   * תרגיל בונוס. הציון שלו הוא עדיין מתוך 100 — מה שמשתנה הוא שהוא אינו נכנס לממוצע
   * אלא מוסיף bonusValue × (הציון ÷ 100) לציון השיעור.
   */
  isBonus: boolean;
  /** כמה נקודות התרגיל מוסיף לשיעור כשהוא נעשה במלואו. 0 כשאינו בונוס. */
  bonusValue: number;
}

export interface LessonScoreSuggestionDto {
  studentId: number;
  lessonId: number;
  studentName: string | null;
  assignmentScores: AssignmentScoreDto[];
  /** הבסיס ועוד הבונוס — הצעה בלבד, ניתנת לעריכה. null כשאין מה לחשב. */
  suggestedScore: number | null;
  gradedCount: number;
  /** ⚠️ הדיאלוג חייב לומר זאת במפורש — ממוצע שמדלג על תרגיל בשקט נראה נכון ואינו נכון. */
  ungradedCount: number;
  /** הממוצע על תרגילי החובה שנבדקו, לפני הבונוס. null כשאין אף אחד כזה. */
  baseScore: number | null;
  /** כמה נקודות תרגילי הבונוס הוסיפו בפועל. */
  bonusPoints: number;
  /**
   * תקרת השיעור: 100 + סכום ה-bonusValue.
   * ⚠️ נקראת מהשרת ולא מחושבת כאן — הדפדפן שקבע לעצמו את הטווח החוקי הוא בדיוק הבאג
   * שבגללו hasBonus הוסר מהבקשה.
   */
  maxScore: number;
}

export interface StudentGradeItemDto {
  lessonId: number;
  subject: string;
  lessonDateHebrew: string;
  finalScore: number | null;
  isComplete: boolean;
}

export interface CourseAverageDto {
  courseId: number;
  courseName: string;
  average: number | null;
  grades: StudentGradeItemDto[];
}

export interface StudentGradesSummaryDto {
  studentId: number;
  studentName: string;
  courses: CourseAverageDto[];
}
