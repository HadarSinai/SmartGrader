export interface TeacherResponseDto {
  id: number;
  fullName: string;
  username: string;
  /** null במורות שנוצרו לפני עמודת המייל — הרשימה מסמנת אותן ב"חסר מייל". */
  email: string | null;
  createdAt: string;
  lessonsCount: number;
  coursesCount: number;
}

export interface CreateTeacherRequestDto {
  fullName: string | null;
  username: string | null;
  email: string | null;
  password: string | null;
}

/** שם המשתמש אינו כאן — הוא מזהה ההתחברות ואינו ניתן לשינוי. */
export interface UpdateTeacherRequestDto {
  fullName: string | null;
  email: string | null;
}

export interface ResetTeacherPasswordRequestDto {
  newPassword: string | null;
}
