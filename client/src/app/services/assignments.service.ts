import { Injectable } from "@angular/core";
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
 * Supports both flat endpoints ("/api/assignments") and lesson-scoped endpoints
 * ("/api/lessons/{lessonId}/assignments") so the UI can compile cleanly.
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
  ): Observable<AssignmentResponseDto>;
  getById(id: number): Observable<AssignmentResponseDto>;
  getById(a: number, b?: number): Observable<AssignmentResponseDto> {
    if (typeof b === "number") {
      return this.api.http.get<AssignmentResponseDto>(
        this.api.url(`/api/lessons/${a}/assignments/${b}`),
      );
    }
    return this.api.http.get<AssignmentResponseDto>(
      this.api.url(`/api/lessons/assignments/${a}`),
    );
  }

  create(
    lessonId: number,
    request: CreateAssignmentRequestDto,
  ): Observable<AssignmentResponseDto>;
  create(
    request: CreateAssignmentRequestDto,
  ): Observable<AssignmentResponseDto>;
  create(
    a: number | CreateAssignmentRequestDto,
    b?: CreateAssignmentRequestDto,
  ): Observable<AssignmentResponseDto> {
    if (typeof a === "number") {
      return this.api.http.post<AssignmentResponseDto>(
        this.api.url(`/api/lessons/${a}/assignments`),
        b,
      );
    }
    return this.api.http.post<AssignmentResponseDto>(
      this.api.url("/api/lessons/assignments"),
      a,
    );
  }

  update(
    lessonId: number,
    assignmentId: number,
    request: UpdateAssignmentRequestDto,
  ): Observable<AssignmentResponseDto>;
  update(
    id: number,
    request: UpdateAssignmentRequestDto,
  ): Observable<AssignmentResponseDto>;
  update(
    a: number,
    b: number | UpdateAssignmentRequestDto,
    c?: UpdateAssignmentRequestDto,
  ): Observable<AssignmentResponseDto> {
    if (typeof b === "number") {
      return this.api.http.put<AssignmentResponseDto>(
        this.api.url(`/api/lessons/${a}/assignments/${b}`),
        c,
      );
    }
    return this.api.http.put<AssignmentResponseDto>(
      this.api.url(`/api/lessons/assignments/${a}`),
      b,
    );
  }

  delete(lessonId: number, assignmentId: number): Observable<void>;
  delete(id: number): Observable<void>;
  delete(a: number, b?: number): Observable<void> {
    if (typeof b === "number") {
      return this.api.http.delete<void>(
        this.api.url(`/api/lessons/${a}/assignments/${b}`),
      );
    }
    return this.api.http.delete<void>(
      this.api.url(`/api/lessons/assignments/${a}`),
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

  // --------------------
  // Flat endpoints (optional)
  // --------------------

  getAll(lessonId: number): Observable<AssignmentResponseDto[]> {
    return this.api.http.get<AssignmentResponseDto[]>(
      this.api.url(`/api/lessons/${lessonId}/assignments`),
    );
  }
}
