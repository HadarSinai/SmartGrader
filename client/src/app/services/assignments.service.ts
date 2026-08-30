import { Injectable } from "@angular/core";
import {
  BulkDeleteRequestDto,
  BulkDeleteResultDto,
} from "@models/bulk-delete.model";
import {
  AssignmentResponseDto,
  CreateAssignmentRequestDto,
  SuggestTestCasesRequestDto,
  SuggestTestCasesResultDto,
  UpdateAssignmentRequestDto,
  VerifyTestCasesRequestDto,
  VerifyTestCasesResultDto,
} from "@models/assignment.model";
import { Observable } from "rxjs";
import { ApiClient } from "../core/http/api-client";

/**
 * Assignments API
 *
 * ⚠️ כל תרגיל חי מתחת לשיעור: `/api/lessons/{lessonId}/assignments/...`. אין נתיב שטוח.
 * לכל מתודה כאן הייתה בעבר גם העמסה בארגומנט אחד שפנתה ל-`/api/lessons/assignments/{id}` —
 * כתובת שאינה תואמת לשום route, כי `{lessonId:int}` לעולם לא יתאים למחרוזת "assignments".
 * אף קורא לא השתמש בהן; הן הוסרו ולא "תוקנו", כי אין למה לתקן אותן.
 */
@Injectable({ providedIn: "root" })
export class AssignmentsService {
  constructor(private api: ApiClient) {}

  // --------------------
  // Lesson scoped
  // --------------------

  getByLesson(lessonId: number): Observable<AssignmentResponseDto[]> {
    return this.api.http.get<AssignmentResponseDto[]>(
      this.api.url(`/api/lessons/${lessonId}/assignments`),
    );
  }

  getById(
    lessonId: number,
    assignmentId: number,
  ): Observable<AssignmentResponseDto> {
    return this.api.http.get<AssignmentResponseDto>(
      this.api.url(`/api/lessons/${lessonId}/assignments/${assignmentId}`),
    );
  }

  create(
    lessonId: number,
    request: CreateAssignmentRequestDto,
  ): Observable<AssignmentResponseDto> {
    return this.api.http.post<AssignmentResponseDto>(
      this.api.url(`/api/lessons/${lessonId}/assignments`),
      request,
    );
  }

  update(
    lessonId: number,
    assignmentId: number,
    request: UpdateAssignmentRequestDto,
  ): Observable<AssignmentResponseDto> {
    return this.api.http.put<AssignmentResponseDto>(
      this.api.url(`/api/lessons/${lessonId}/assignments/${assignmentId}`),
      request,
    );
  }

  delete(lessonId: number, assignmentId: number): Observable<void> {
    return this.api.http.delete<void>(
      this.api.url(`/api/lessons/${lessonId}/assignments/${assignmentId}`),
    );
  }

  /** ר' bulkDelete בשירות השיעורים: אותם שומרים בדיוק, שורה אחת בכל פעם. */
  bulkDelete(
    lessonId: number,
    ids: number[],
  ): Observable<BulkDeleteResultDto> {
    return this.api.http.post<BulkDeleteResultDto>(
      this.api.url(`/api/lessons/${lessonId}/assignments/bulk-delete`),
      { ids } as BulkDeleteRequestDto,
    );
  }

  // --------------------
  // כלי כתיבת מקרי בדיקה
  // --------------------
  //
  // שני אלה שולחים את תוכן *הטופס* ולא מזהה תרגיל: המורה משתמשת בהם בזמן הכתיבה,
  // לפעמים לפני שהתרגיל נשמר בכלל. לכן הם תלויים רק ב-lessonId.

  /** מריץ את הפתרון לדוגמה מול מקרי הבדיקה שבטופס. לא שומר דבר. */
  verifyTests(
    lessonId: number,
    request: VerifyTestCasesRequestDto,
  ): Observable<VerifyTestCasesResultDto> {
    return this.api.http.post<VerifyTestCasesResultDto>(
      this.api.url(`/api/lessons/${lessonId}/assignments/verify-tests`),
      request,
    );
  }

  /** מבקש הצעות מה-AI. השרת מריץ כל הצעה מול הפתרון לדוגמה לפני שהיא חוזרת לכאן. */
  suggestTests(
    lessonId: number,
    request: SuggestTestCasesRequestDto,
  ): Observable<SuggestTestCasesResultDto> {
    return this.api.http.post<SuggestTestCasesResultDto>(
      this.api.url(`/api/lessons/${lessonId}/assignments/suggest-tests`),
      request,
    );
  }

}
