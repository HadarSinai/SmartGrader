export interface LessonClassDto {
  id: number;
  name: string;
}

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
  classes: LessonClassDto[];
  classNames: string;
}

export interface CreateLessonRequestDto {
  name: string | null;
  subject: string | null;
  hebrewYear: number;
  hebrewMonth: number;
  hebrewDay: number;
  teacherName: string | null;
  classIds: number[];
}

export interface UpdateLessonRequestDto {
  name: string | null;
  subject: string | null;
  hebrewYear: number;
  hebrewMonth: number;
  hebrewDay: number;
  teacherName: string | null;
  classIds: number[];
}
