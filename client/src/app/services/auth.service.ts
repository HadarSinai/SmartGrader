import { Injectable, computed, signal } from "@angular/core";
import { Observable, tap } from "rxjs";
import { ApiClient } from "../core/http/api-client";
import {
    AuthResponseDto,
    ChangeMyPasswordRequestDto,
    CreateAccountForStudentRequestDto,
    CreateStudentAccountRequestDto,
    ForgotPasswordRequestDto,
    LoginRequestDto,
    MyProfileResponseDto,
    ResetPasswordRequestDto,
    UpdateMyProfileRequestDto,
} from "../models/auth.model";
import { StudentResponseDto } from "../models/student.model";

const TOKEN_KEY = "sg_token";
const USER_KEY = "sg_user";

interface StoredUser {
  fullName: string;
  role: "Teacher" | "Student" | "Admin";
  studentId: number | null;
}

@Injectable({ providedIn: "root" })
export class AuthService {
  private readonly _token = signal<string | null>(
    localStorage.getItem(TOKEN_KEY),
  );
  private readonly _user = signal<StoredUser | null>(readStoredUser());

  readonly isLoggedIn = computed(() => this._token() !== null);
  readonly fullName = computed(() => this._user()?.fullName ?? "");
  readonly role = computed(() => this._user()?.role ?? null);
  readonly studentId = computed(() => this._user()?.studentId ?? null);
  readonly isTeacher = computed(() => this.role() === "Teacher");
  readonly isStudent = computed(() => this.role() === "Student");
  readonly isAdmin = computed(() => this.role() === "Admin");

  constructor(private api: ApiClient) {}

  get token(): string | null {
    return this._token();
  }

  login(dto: LoginRequestDto): Observable<AuthResponseDto> {
    return this.api.http
      .post<AuthResponseDto>(this.api.url("/api/auth/login"), dto)
      .pipe(tap((res) => this.storeSession(res)));
  }

  /**
   * בקשת קישור לאיפוס סיסמה.
   * ⚠️ מצליחה תמיד, גם כשהכתובת אינה רשומה — השרת מחזיר 200 זהה בכל מקרה, ולכן אין
   * למסך שום דרך (ואין לו כוונה) לדעת אם נשלח מייל בפועל.
   */
  forgotPassword(dto: ForgotPasswordRequestDto): Observable<void> {
    return this.api.http.post<void>(
      this.api.url("/api/auth/forgot-password"),
      dto,
    );
  }

  /** קביעת סיסמה חדשה באמצעות הטוקן החד-פעמי מהקישור שבמייל. */
  resetPassword(dto: ResetPasswordRequestDto): Observable<void> {
    return this.api.http.post<void>(
      this.api.url("/api/auth/reset-password"),
      dto,
    );
  }

  // ⚠️ אין כאן registerTeacher. חשבון מורה נוצר רק בידי המנהלת דרך TeachersService,
  // והוא אינו מחליף את ה-session שלה: המנהלת נשארת מחוברת כמנהלת.
  createStudentAccount(
    dto: CreateStudentAccountRequestDto,
  ): Observable<StudentResponseDto> {
    return this.api.http.post<StudentResponseDto>(
      this.api.url("/api/auth/students"),
      dto,
    );
  }

  createAccountForStudent(
    studentId: number,
    dto: CreateAccountForStudentRequestDto,
  ): Observable<StudentResponseDto> {
    return this.api.http.post<StudentResponseDto>(
      this.api.url(`/api/auth/students/${studentId}/account`),
      dto,
    );
  }

  // ── האזור האישי: המשתמשת מתחזקת את החשבון של עצמה ──

  /** פרטי החשבון מהמסד, לטעינת הטופס ב-/profile. */
  getMyProfile(): Observable<MyProfileResponseDto> {
    return this.api.http.get<MyProfileResponseDto>(
      this.api.url("/api/auth/me/profile"),
    );
  }

  /**
   * שינוי השם המלא והמייל של המשתמשת המחוברת.
   *
   * ⚠️ ה-tap אינו קישוט. השם המלא הוא claim בתוך ה-JWT, והשרת מחזיר כאן טוקן **חדש**
   * בדיוק בשביל זה. בלי storeSession, הדפדפן היה ממשיך להחזיק את הטוקן הישן ואת
   * sg_user הישן — כלומר השם בסרגל העליון היה נשאר כפי שהיה עד הכניסה הבאה.
   */
  updateMyProfile(dto: UpdateMyProfileRequestDto): Observable<AuthResponseDto> {
    return this.api.http
      .put<AuthResponseDto>(this.api.url("/api/auth/me"), dto)
      .pipe(tap((res) => this.storeSession(res)));
  }

  /**
   * שינוי הסיסמה של המשתמשת המחוברת, מול הסיסמה הנוכחית שלה.
   *
   * ⚠️ אין כאן storeSession ואין ניתוק. הסיסמה אינה claim בטוקן, ולכן הטוקן הקיים
   * נשאר נכון לחלוטין אחרי ההחלפה — בשונה משינוי שם.
   */
  changeMyPassword(dto: ChangeMyPasswordRequestDto): Observable<void> {
    return this.api.http.post<void>(
      this.api.url("/api/auth/me/password"),
      dto,
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._token.set(null);
    this._user.set(null);
  }

  /** Default landing route by role (teacher → dashboard, student → the student area). */
  homeRoute(): string[] {
    if (this.isStudent()) {
      // ⚠️ A student with no studentId must not fall through to "/" — that route is
      // teacherGuard-protected and redirects straight back here, which was an infinite loop.
      return this.studentId() !== null ? ["/my", "lessons"] : ["/login"];
    }
    return ["/"];
  }

  private storeSession(res: AuthResponseDto): void {
    const user: StoredUser = {
      fullName: res.fullName,
      role: res.role,
      studentId: res.studentId,
    };
    localStorage.setItem(TOKEN_KEY, res.token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this._token.set(res.token);
    this._user.set(user);
  }
}

function readStoredUser(): StoredUser | null {
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as StoredUser;
  } catch {
    return null;
  }
}
