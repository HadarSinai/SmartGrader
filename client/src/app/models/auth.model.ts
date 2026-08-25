export interface LoginRequestDto {
  username: string | null;
  password: string | null;
}

// ⚠️ אין כאן RegisterTeacherRequestDto. הרשמה עצמית נסגרה — יצירת מורה עברה
// ל-CreateTeacherRequestDto ב-teacher.model.ts, שם המייל הוא שדה חובה.
export interface CreateStudentAccountRequestDto {
  fullName: string | null;
  classId: number | null;
  username: string | null;
  password: string | null;
}

export interface CreateAccountForStudentRequestDto {
  username: string | null;
  password: string | null;
}

export interface ForgotPasswordRequestDto {
  email: string | null;
}

export interface ResetPasswordRequestDto {
  token: string | null;
  newPassword: string | null;
}

// ⚠️ אין כאן טיפוס תשובה לשתי הנקודות האלה. שתיהן מחזירות גוף ריק במכוון — כל שדה
// שהיה חוזר מ-forgot-password היה מספר לקוראת אם הכתובת רשומה במערכת.

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

// ── האזור האישי ──

/**
 * פרטי החשבון כפי שהם במסד, לטעינת הטופס ב-/profile.
 *
 * ⚠️ נפרד מ-CurrentUserDto ולא הרחבה שלו: זה מגיע מ-GET /api/auth/me/profile שקורא
 * למסד, בעוד CurrentUserDto מגיע מה-claims של הטוקן בלבד. המייל אינו claim במכוון —
 * הטוקן יושב ב-localStorage וקריא לכל מי שמגיעה אליו.
 *
 * email הוא null לתלמידה (אין לה מייל) ולשורות מורות שנוצרו לפני עמודת המייל.
 */
export interface MyProfileResponseDto {
  userId: number;
  username: string;
  fullName: string;
  email: string | null;
  role: "Teacher" | "Student" | "Admin";
}

/** ⚠️ אין כאן שדה מזהה. השרת לוקח אותו מהטוקן — ר' ההערה ב-AuthDtos.cs. */
export interface UpdateMyProfileRequestDto {
  fullName: string | null;
  email: string | null;
}

export interface ChangeMyPasswordRequestDto {
  currentPassword: string | null;
  newPassword: string | null;
}
