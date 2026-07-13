import { Injectable } from "@angular/core";
import { LessonResultResponseDto } from "@models/lesson-result.model";
import { Observable } from "rxjs";
import { ApiClient } from "../core/http/api-client";

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
}
