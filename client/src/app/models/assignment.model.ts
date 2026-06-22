export interface TestCaseDto {
  input: string | null;
  expected: string | null;
}

export interface AssignmentResponseDto {
  id: number;
  lessonId: number;
  title: string | null;
  description: string | null;
  isBonus: boolean;
  bonusValue: number;
  createdAt: string;
  submissionsCount: number;
  tests: TestCaseDto[] | null;
}

export interface CreateAssignmentRequestDto {
  title: string | null;
  description: string | null;
  isBonus: boolean;
  bonusValue: number;
  tests: TestCaseDto[] | null;
}

export interface UpdateAssignmentRequestDto {
  title: string | null;
  description: string | null;
  isBonus: boolean;
  bonusValue: number;
  tests: TestCaseDto[] | null;
}
