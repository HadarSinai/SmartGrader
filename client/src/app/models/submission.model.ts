export type SubmissionStatus =
  | "PendingAi"
  | "ProcessingAi"
  | "Done"
  | "AiFailed"
  | "CompilationFailed";

export const STATUS_LABELS_HE: Record<string, string> = {
  PendingAi: "ממתין לבדיקה",
  ProcessingAi: "בבדיקה...",
  Done: "נבדק",
  AiFailed: "שגיאת בדיקה",
  CompilationFailed: "שגיאת קומפילציה",
};

export interface SubmissionResponseDto {
  id: number;
  studentId: number;
  assignmentId: number;
  sourceCode: string | null;
  score: number | null;
  comments: string | null;
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
}

export interface UpdateSubmissionRequestDto {
  sourceCode: string | null;
}
