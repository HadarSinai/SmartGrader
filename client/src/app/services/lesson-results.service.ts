import { Injectable } from "@angular/core";
import {
  CompleteLessonRequestDto,
  LessonResultResponseDto,
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
