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

export interface StudentGradeItemDto {
  lessonId: number;
  lessonName: string;
  lessonDateHebrew: string;
  finalScore: number | null;
  isComplete: boolean;
}

export interface StudentGradesSummaryDto {
  studentId: number;
  studentName: string;
  average: number | null;
  grades: StudentGradeItemDto[];
}
