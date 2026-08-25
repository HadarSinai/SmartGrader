/**
 * סוג הסיגנל. שני הראשונים על הכיתה (מה ללמד מחדש), שני האחרונים על התרגיל
 * (שנכתב לא נכון). מגיע מהשרת כמחרוזת, לא כמספר.
 */
export type ClassSignalType =
  | "StructuralRequirementFailed"
  | "TestCaseFailed"
  | "NobodyPassed"
  | "CompilationFailedForMost";

/**
 * סיגנל אחד בפעמון של המורה — אגרגציה על תרגיל, לא הגשה בודדת.
 *
 * ⚠️ אין כאן `id`: אין ישות ואין שורה בבסיס הנתונים. `key` הוא המזהה היציב,
 * ולפיו נשמר מה כבר נקרא.
 */
export interface ClassSignalDto {
  key: string;
  type: ClassSignalType;
  lessonId: number;
  lessonSubject: string;
  assignmentId: number;
  assignmentTitle: string;
  detail: string | null;
  affectedCount: number;
  totalCount: number;
  /** המשפט המוצג. מגיע מהשרת כדי שהפעמון והמייל יאמרו בדיוק אותו דבר. */
  message: string;
}
