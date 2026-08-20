import { Injectable } from "@angular/core";
import {
  CompleteLessonRequestDto,
  LessonResultResponseDto,
  LessonScoreSuggestionDto,
  StudentGradesSummaryDto,
} from "@models/lesson-result.model";
import { Observable } from "rxjs";
import { ApiClient } from "../core/http/api-client";
import { HebrewDateValue } from "@components/hebrew-date-picker/hebrew-date-picker.component";

@Injectable({ providedIn: "root" })
export class LessonResultsService {
  constructor(private api: ApiClient) {}

  getResult(
    studentId: number,
    lessonId: number,
  ): Observable<LessonResultResponseDto> {
    return this.api.http.get<LessonResultResponseDto>(
      this.api.url(`/api/lesson-results/${studentId}/${lessonId}`),
    );
  }

  complete(
    request: CompleteLessonRequestDto,
  ): Observable<LessonResultResponseDto> {
    return this.api.http.post<LessonResultResponseDto>(
      this.api.url("/api/lesson-results/complete"),
      request,
    );
  }

  /**
   * הציון שכל תרגיל קיבל, והממוצע כהצעה לדיאלוג "סיום שיעור".
   * ⚠️ מורה/מנהל בלבד — התלמידה אינה אמורה לראות את הציון הסופי לפני שנקבע.
   */
  getScoreSuggestion(
    studentId: number,
    lessonId: number,
  ): Observable<LessonScoreSuggestionDto> {
    return this.api.http.get<LessonScoreSuggestionDto>(
      this.api.url(`/api/lesson-results/${studentId}/${lessonId}/suggestion`),
    );
  }

  /**
   * פתיחה מחדש של ציון סופי שכבר נקבע.
   * ⚠️ עד כה ציון סופי שגוי לא היה ניתן לתיקון בשום דרך — CompleteWith זרק "Already completed".
   * הפתיחה גם משחררת את ההגשות של אותה תלמידה באותו שיעור.
   */
  reopen(
    studentId: number,
    lessonId: number,
  ): Observable<LessonResultResponseDto> {
    return this.api.http.post<LessonResultResponseDto>(
      this.api.url(`/api/lesson-results/${studentId}/${lessonId}/reopen`),
      {},
    );
  }

  exportExcel(lessonId: number): Observable<Blob> {
    return this.api.http.get(
      this.api.url(`/api/lesson-results/lesson/${lessonId}/export`),
      { responseType: "blob" },
    );
  }

  exportPeriodReport(
    from: HebrewDateValue,
    to: HebrewDateValue,
  ): Observable<Blob> {
    return this.api.http.get(this.api.url("/api/lesson-results/export-report"), {
      params: {
        fromYear: from.hebrewYear,
        fromMonth: from.hebrewMonth,
        fromDay: from.hebrewDay,
        toYear: to.hebrewYear,
        toMonth: to.hebrewMonth,
        toDay: to.hebrewDay,
      },
      responseType: "blob",
    });
  }

  getStudentSummary(studentId: number): Observable<StudentGradesSummaryDto> {
    return this.api.http.get<StudentGradesSummaryDto>(
      this.api.url(`/api/lesson-results/student/${studentId}/summary`),
    );
  }
}
