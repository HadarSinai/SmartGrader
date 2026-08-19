export interface TestCaseDto {
  input: string | null;
  expected: string | null;
  /**
   * מקרה דוגמה — התלמידה רואה אותו. מקרה שאינו דוגמה מסונן בשרת ולא מגיע ללקוח כלל,
   * כך שברשימה שמתקבלת בתצוגת תלמידה כל הפריטים הם isSample=true.
   */
  isSample: boolean;
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
}
