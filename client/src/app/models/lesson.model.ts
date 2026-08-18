export interface LessonClassDto {
  id: number;
  name: string;
}

export interface LessonResponseDto {
  id: number;
  courseId: number;
  courseName: string;
  subject: string | null;
  lessonDate: string;
  lessonDateHebrew: string;
  hebrewYear: number;
  hebrewMonth: number;
  hebrewDay: number;
  createdAt: string;
  assignmentsCount: number;
  classes: LessonClassDto[];
  classNames: string;
}

export interface CreateLessonRequestDto {
  courseId: number;
  subject: string | null;
  hebrewYear: number;
  hebrewMonth: number;
  hebrewDay: number;
  classIds: number[];
}

export interface UpdateLessonRequestDto {
  courseId: number;
  subject: string | null;
  hebrewYear: number;
  hebrewMonth: number;
  hebrewDay: number;
  classIds: number[];
}
