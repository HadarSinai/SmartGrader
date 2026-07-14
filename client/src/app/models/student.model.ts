export interface StudentResponseDto {
  id: number;
  fullName: string | null;
  className: string | null;
  createdAt: string;
  submissionsCount: number;
  lessonResultsCount: number;
  hasAccount: boolean;
}

export interface CreateStudentRequestDto {
  fullName: string | null;
  className: string | null;
}

export interface UpdateStudentRequestDto {
  fullName: string | null;
  className: string | null;
}
