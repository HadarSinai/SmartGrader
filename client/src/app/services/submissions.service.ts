import { Injectable } from "@angular/core";
import {
  CreateSubmissionRequestDto,
  GrantExtraAttemptRequestDto,
  OverrideScoreRequestDto,
  SubmissionResponseDto,
  UpdateSubmissionRequestDto,
} from "@models/submission.model";
import { Observable } from "rxjs";
import { ApiClient } from "../core/http/api-client";

/**
 * Submissions API
 *
 * Supports student-scoped routes (recommended) and a few helper routes used
 * by the dashboard.
 */
@Injectable({ providedIn: "root" })
export class SubmissionsService {
  constructor(private api: ApiClient) {}

  // Student scoped
  getByStudent(studentId: number): Observable<SubmissionResponseDto[]> {
    return this.api.http.get<SubmissionResponseDto[]>(
      this.api.url(`/api/students/${studentId}/submissions`),
    );
  }

  getById(
    studentId: number,
    submissionId: number,
  ): Observable<SubmissionResponseDto>;
  getById(id: number): Observable<SubmissionResponseDto>;
  getById(a: number, b?: number): Observable<SubmissionResponseDto> {
    if (typeof b === "number") {
      return this.api.http.get<SubmissionResponseDto>(
        this.api.url(`/api/students/${a}/submissions/${b}`),
      );
    }
    return this.api.http.get<SubmissionResponseDto>(
      this.api.url(`/api/students/submissions/${a}`),
    );
  }

  create(
    studentId: number,
    request: CreateSubmissionRequestDto,
  ): Observable<SubmissionResponseDto> {
    return this.api.http.post<SubmissionResponseDto>(
      this.api.url(`/api/students/${studentId}/submissions`),
      request,
    );
  }

  update(
    studentId: number,
    submissionId: number,
    request: UpdateSubmissionRequestDto,
  ): Observable<SubmissionResponseDto> {
    return this.api.http.put<SubmissionResponseDto>(
      this.api.url(`/api/students/${studentId}/submissions/${submissionId}`),
      request,
    );
  }

  // ── דריסות המורה ──────────────────────────────────────────────────────
  //
  // ⚠️ מורה/מנהל בלבד, ובעלוּת על השיעור נאכפת בשרת. שתי הפעולות נרשמות ביומן ביקורת
  // (מי / מתי / למי / למה) — זה מה שמחליף "לראות מי השתמשה בקוד המשותף".

  /** אישור הגשה נוספת מעל סף הציון. חד-פעמי, נצרך בהגשה הבאה. */
  grantExtraAttempt(
    studentId: number,
    submissionId: number,
    request: GrantExtraAttemptRequestDto,
  ): Observable<SubmissionResponseDto> {
    return this.api.http.post<SubmissionResponseDto>(
      this.api.url(
        `/api/students/${studentId}/submissions/${submissionId}/extra-attempt`,
      ),
      request,
    );
  }

  /** דריסת הציון בידי המורה — רשת ביטחון, לא חלק מהמסלול הרגיל. */
  overrideScore(
    studentId: number,
    submissionId: number,
    request: OverrideScoreRequestDto,
  ): Observable<SubmissionResponseDto> {
    return this.api.http.put<SubmissionResponseDto>(
      this.api.url(
        `/api/students/${studentId}/submissions/${submissionId}/score`,
      ),
      request,
    );
  }

  delete(studentId: number, submissionId: number): Observable<void> {
    return this.api.http.delete<void>(
      this.api.url(`/api/students/${studentId}/submissions/${submissionId}`),
    );
  }

  // Dashboard helper
  getRecent(limit: number): Observable<SubmissionResponseDto[]> {
    // If your backend uses a different query name, adjust here.
    return this.api.http.get<SubmissionResponseDto[]>(
      this.api.url(`/api/students/submissions/recent?limit=${limit}`),
    );
  }

  // Flat endpoints (optional)
  getAll(studentId: number): Observable<SubmissionResponseDto[]> {
    return this.api.http.get<SubmissionResponseDto[]>(
      this.api.url(`/api/students/${studentId}/submissions`),
    );
  }
}
