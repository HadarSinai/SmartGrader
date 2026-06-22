export interface LessonResponseDto {
  id: number;
  name: string | null;
  subject: string | null;
  lessonDate: string;
  teacherName: string | null;
  createdAt: string;
  assignmentsCount: number;
}

export interface CreateLessonRequestDto {
  name: string | null;
  subject: string | null;
  lessonDate: string;
  teacherName: string | null;
}

export interface UpdateLessonRequestDto {
  name: string | null;
  subject: string | null;
  lessonDate: string;
  teacherName: string | null;
}
