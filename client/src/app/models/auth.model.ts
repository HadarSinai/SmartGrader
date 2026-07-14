export interface LoginRequestDto {
  email: string | null;
  password: string | null;
}

export interface RegisterTeacherRequestDto {
  fullName: string | null;
  email: string | null;
  password: string | null;
}

export interface CreateStudentAccountRequestDto {
  fullName: string | null;
  className: string | null;
  email: string | null;
  password: string | null;
}

export interface CreateAccountForStudentRequestDto {
  email: string | null;
  password: string | null;
}

export interface AuthResponseDto {
  token: string;
  fullName: string;
  role: "Teacher" | "Student";
  studentId: number | null;
}

export interface CurrentUserDto {
  userId: number;
  fullName: string;
  role: "Teacher" | "Student";
  studentId: number | null;
}
