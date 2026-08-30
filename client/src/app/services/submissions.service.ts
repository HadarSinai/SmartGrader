import { Injectable } from "@angular/core";
import {
  BulkDeleteRequestDto,
  BulkDeleteResultDto,
} from "@models/bulk-delete.model";
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
 * ⚠️ כל הגשה חיה מתחת לתלמידה: `/api/students/{studentId}/submissions/...` (הנתיבים
 * נמצאים ב-StudentsController; SubmissionController כולו בהערה). אין נתיב שטוח.
 * `getById` נשא בעבר גם העמסה בארגומנט אחד שפנתה ל-`/api/students/submissions/{id}` —
 * כתובת שאינה תואמת לשום route, כי `{studentId:int}` לעולם לא יתאים למחרוזת
 * "submissions". זו אותה טעות בדיוק שהשביתה את הדשבורד (ר' getRecent למטה).
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
  ): Observable<SubmissionResponseDto> {
    return this.api.http.get<SubmissionResponseDto>(
      this.api.url(`/api/students/${studentId}/submissions/${submissionId}`),
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

  /**
   * ר' bulkDelete בשירות השיעורים.
   * ⚠️ דווקא כאן רוב השורות ייענו בסירוב: הגשה שנבדקה נושאת ציון ואינה נמחקת, וזה
   * המסך שבו הצגת הסיבות היא כל ההבדל בין תשובה למסך שנראה תקוע.
   */
  bulkDelete(
    studentId: number,
    ids: number[],
  ): Observable<BulkDeleteResultDto> {
    return this.api.http.post<BulkDeleteResultDto>(
      this.api.url(`/api/students/${studentId}/submissions/bulk-delete`),
      { ids } as BulkDeleteRequestDto,
    );
  }

  // Dashboard helper
  //
  // ⚠️ הכתובת היא /api/notifications/graded-submissions ולא משהו תחת /api/students.
  // הנתיב שהיה כאן קודם ניסה לקרוא ל-recent מתחת ל-students, ולא התאים לשום route
  // בשרת (המקטע "submissions" אינו int ב-{studentId:int}) — הוא החזיר 404 בתוך
  // ה-forkJoin של הדשבורד והפיל איתו את כל ארבעת כרטיסי ה-KPI.
  //
  // ⚠️ השרת חוסם limit ב-InclusiveBetween(1, 100) — ערך גדול יותר יחזור כ-400.
  getRecent(limit: number): Observable<SubmissionResponseDto[]> {
    return this.api.http.get<SubmissionResponseDto[]>(
      this.api.url(`/api/notifications/graded-submissions?limit=${limit}`),
    );
  }
}
