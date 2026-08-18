export interface CourseResponseDto {
  id: number;
  name: string;
  lessonsCount: number;
  createdAt: string;
}

export interface CreateCourseRequestDto {
  name: string | null;
}

export interface UpdateCourseRequestDto {
  name: string | null;
}
