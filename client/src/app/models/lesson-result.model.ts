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
}
