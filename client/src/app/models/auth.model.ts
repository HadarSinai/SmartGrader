export interface LoginRequestDto {
  username: string | null;
  password: string | null;
}

export interface RegisterTeacherRequestDto {
  fullName: string | null;
  username: string | null;
  password: string | null;
}

export interface CreateStudentAccountRequestDto {
  fullName: string | null;
  className: string | null;
  username: string | null;
  password: string | null;
}

export interface CreateAccountForStudentRequestDto {
  username: string | null;
  password: string | null;
}

export interface AuthResponseDto {
  token: string;
  fullName: string;
  role: "Teacher" | "Student" | "Admin";
  studentId: number | null;
}

export interface CurrentUserDto {
  userId: number;
  fullName: string;
  role: "Teacher" | "Student" | "Admin";
  studentId: number | null;
}
