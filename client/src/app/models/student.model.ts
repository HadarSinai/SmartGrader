export interface StudentResponseDto {
  id: number;
  fullName: string | null;
  classId: number;
  className: string | null;
  classIsArchived: boolean;
  createdAt: string;
  submissionsCount: number;
  lessonResultsCount: number;
  hasAccount: boolean;
}

export interface CreateStudentRequestDto {
  fullName: string | null;
  classId: number | null;
}

export interface UpdateStudentRequestDto {
  fullName: string | null;
  classId: number | null;
}

export interface ImportRowErrorDto {
  rowNumber: number;
  message: string;
}

export interface ImportStudentsResultDto {
  createdCount: number;
  errors: ImportRowErrorDto[];
}
