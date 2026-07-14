import { Injectable, computed, signal } from "@angular/core";
import { Observable, tap } from "rxjs";
import { ApiClient } from "../core/http/api-client";
import {
    AuthResponseDto,
    CreateAccountForStudentRequestDto,
    CreateStudentAccountRequestDto,
    LoginRequestDto,
    RegisterTeacherRequestDto,
} from "../models/auth.model";
import { StudentResponseDto } from "../models/student.model";

const TOKEN_KEY = "sg_token";
const USER_KEY = "sg_user";

interface StoredUser {
  fullName: string;
  role: "Teacher" | "Student";
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

  constructor(private api: ApiClient) {}

  get token(): string | null {
    return this._token();
  }

  login(dto: LoginRequestDto): Observable<AuthResponseDto> {
    return this.api.http
      .post<AuthResponseDto>(this.api.url("/api/auth/login"), dto)
      .pipe(tap((res) => this.storeSession(res)));
  }

  registerTeacher(dto: RegisterTeacherRequestDto): Observable<AuthResponseDto> {
    return this.api.http
      .post<AuthResponseDto>(this.api.url("/api/auth/register-teacher"), dto)
      .pipe(tap((res) => this.storeSession(res)));
  }

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

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._token.set(null);
    this._user.set(null);
  }

  /** Default landing route by role (teacher → dashboard, student → the student area). */
  homeRoute(): string[] {
    if (this.isStudent() && this.studentId() !== null) {
      return ["/my", "lessons"];
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
