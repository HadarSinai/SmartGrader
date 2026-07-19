export interface SchoolClassResponseDto {
  id: number;
  name: string;
  academicYear: number;
  academicYearHebrew: string;
  isArchived: boolean;
  studentsCount: number;
  createdAt: string;
}

export interface CreateClassRequestDto {
  name: string | null;
  academicYear: number;
}

export interface UpdateClassRequestDto {
  name: string | null;
  academicYear: number;
}

export interface FinishYearResultDto {
  archivedCount: number;
}
