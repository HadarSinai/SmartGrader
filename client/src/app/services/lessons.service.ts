import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClient } from '../core/http/api-client';
import { LessonResponseDto, CreateLessonRequestDto, UpdateLessonRequestDto } from '@models/lesson.model';
import { BulkDeleteRequestDto, BulkDeleteResultDto } from '@models/bulk-delete.model';

@Injectable({ providedIn: 'root' })
export class LessonsService {
  constructor(private api: ApiClient) {}

  getAll(): Observable<LessonResponseDto[]> {
    return this.api.http.get<LessonResponseDto[]>(this.api.url('/api/lessons'));
  }

  getById(id: number): Observable<LessonResponseDto> {
    return this.api.http.get<LessonResponseDto>(this.api.url(`/api/lessons/${id}`));
  }

  create(request: CreateLessonRequestDto): Observable<LessonResponseDto> {
    return this.api.http.post<LessonResponseDto>(this.api.url('/api/lessons'), request);
  }

  update(id: number, request: UpdateLessonRequestDto): Observable<LessonResponseDto> {
    return this.api.http.put<LessonResponseDto>(this.api.url(`/api/lessons/${id}`), request);
  }

  delete(id: number): Observable<void> {
    return this.api.http.delete<void>(this.api.url(`/api/lessons/${id}`));
  }

  /**
   * ⚠️ POST ולא DELETE: גוף בקשה ב-DELETE אינו מובטח לעבור בכל שרת ומתווך.
   * מחזיר תוצאה לכל שורה — הצלחה חלקית היא התוצאה הרגילה, ר' B-55.
   */
  bulkDelete(ids: number[]): Observable<BulkDeleteResultDto> {
    return this.api.http.post<BulkDeleteResultDto>(
      this.api.url('/api/lessons/bulk-delete'),
      { ids } as BulkDeleteRequestDto,
    );
  }
}
