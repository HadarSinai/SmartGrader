export type SubmissionStatus =
  | "PendingAi"
  | "ProcessingAi"
  | "Done"
  | "AiFailed"
  | "CompilationFailed"
  | "JudgeUnavailable";

export const STATUS_LABELS_HE: Record<string, string> = {
  PendingAi: "ממתין לבדיקה",
  ProcessingAi: "בבדיקה...",
  Done: "נבדק",
  AiFailed: "שגיאת בדיקה",
  CompilationFailed: "שגיאת קומפילציה",
  JudgeUnavailable: "תקלה במערכת הבדיקה",
};

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

export interface AiFeedbackScoresDto {
  testScore: number | null;
  codeQualityScore: number | null;
  efficiencyScore: number | null;
  finalScore: number | null;
}

export interface AiFeedbackResultDto {
  good: string[];
  issues: AiFeedbackIssuesDto;
  minimalChanges: string[];
  optionalFullSolution: string | null;
  scores: AiFeedbackScoresDto;
  /** false כאשר תשובת ה-AI לא פורשה בהצלחה — יש להציג rawResponse כטקסט גולמי */
  parseSucceeded: boolean;
  rawResponse: string | null;
}

export interface SubmissionResponseDto {
  id: number;
  studentId: number;
  assignmentId: number;
  sourceCode: string | null;
  sourceFiles: SubmissionFileDto[];
  score: number | null;
  feedback: AiFeedbackResultDto | null;
  testResults: TestCaseResultDto[];
  status: SubmissionStatus | null;
  aiError: string | null;
  compileError: string | null;
  submittedAt: string;
  studentName: string | null;
  assignmentName: string | null;
}

export interface CreateSubmissionRequestDto {
  assignmentId: number;
  sourceCode: string | null;
  files: SubmissionFileDto[] | null;
}

export interface UpdateSubmissionRequestDto {
  sourceCode: string | null;
}
