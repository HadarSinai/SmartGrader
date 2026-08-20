export interface LessonResultResponseDto {
  id: number;
  studentId: number;
  lessonId: number;
  finalScore: number | null;
  isComplete: boolean;
  calculatedAt: string;
  totalAssignments: number;
  completedAssignments: number;
}

export interface CompleteLessonRequestDto {
  studentId: number;
  lessonId: number;
  finalScore: number;
  hasBonus: boolean;
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
  isBonus: boolean;
  /** תקרת הציון של התרגיל — 100, או יותר בתרגיל בונוס. */
  maxScore: number;
}

export interface LessonScoreSuggestionDto {
  studentId: number;
  lessonId: number;
  studentName: string | null;
  assignmentScores: AssignmentScoreDto[];
  /** הממוצע על התרגילים שיש להם ציון — הצעה בלבד, ניתנת לעריכה. null כשאין מה לחשב. */
  suggestedScore: number | null;
  gradedCount: number;
  /** ⚠️ הדיאלוג חייב לומר זאת במפורש — ממוצע שמדלג על תרגיל בשקט נראה נכון ואינו נכון. */
  ungradedCount: number;
  hasBonus: boolean;
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
