export interface LessonResponseDto {
  id: number;
  name: string | null;
  subject: string | null;
  lessonDate: string;
  lessonDateHebrew: string;
  hebrewYear: number;
  hebrewMonth: number;
  hebrewDay: number;
  teacherName: string | null;
  createdAt: string;
  assignmentsCount: number;
}

export interface CreateLessonRequestDto {
  name: string | null;
  subject: string | null;
  hebrewYear: number;
  hebrewMonth: number;
  hebrewDay: number;
  teacherName: string | null;
}

export interface UpdateLessonRequestDto {
  name: string | null;
  subject: string | null;
  hebrewYear: number;
  hebrewMonth: number;
  hebrewDay: number;
  teacherName: string | null;
}
