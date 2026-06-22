export interface SubmissionResponseDto {
  id: number;
  studentId: number;
  assignmentId: number;
  sourceCode: string | null;
  score: number | null;
  comments: string | null;
  status: string | null;
  aiError: string | null;
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
