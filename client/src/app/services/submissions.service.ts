import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClient } from '../core/http/api-client';
import {
  SubmissionResponseDto,
  CreateSubmissionRequestDto,
  UpdateSubmissionRequestDto
} from '@models/submission.model';

/**
 * Submissions API
 *
 * Supports student-scoped routes (recommended) and a few helper routes used
 * by the dashboard.
 */
@Injectable({ providedIn: 'root' })
export class SubmissionsService {
  constructor(private api: ApiClient) {}

  // Student scoped
  getByStudent(studentId: number): Observable<SubmissionResponseDto[]> {
    return this.api.http.get<SubmissionResponseDto[]>(
      this.api.url(`/api/students/${studentId}/submissions`)
    );
  }

  getById(studentId: number, submissionId: number): Observable<SubmissionResponseDto>;
  getById(id: number): Observable<SubmissionResponseDto>;
  getById(a: number, b?: number): Observable<SubmissionResponseDto> {
    if (typeof b === 'number') {
      return this.api.http.get<SubmissionResponseDto>(
        this.api.url(`/api/students/${a}/submissions/${b}`)
      );
    }
    return this.api.http.get<SubmissionResponseDto>(this.api.url(`/api/students/submissions/${a}`));
  }

  create(studentId: number, request: CreateSubmissionRequestDto): Observable<SubmissionResponseDto> {
    return this.api.http.post<SubmissionResponseDto>(
      this.api.url(`/api/students/${studentId}/submissions`),
      request
    );
  }

  update(
    studentId: number,
    submissionId: number,
    request: UpdateSubmissionRequestDto
  ): Observable<SubmissionResponseDto> {
    return this.api.http.put<SubmissionResponseDto>(
      this.api.url(`/api/students/${studentId}/submissions/${submissionId}`),
      request
    );
  }

  // Dashboard helper
  getRecent(limit: number): Observable<SubmissionResponseDto[]> {
    // If your backend uses a different query name, adjust here.
    return this.api.http.get<SubmissionResponseDto[]>(
      this.api.url(`/api/students/submissions/recent?limit=${limit}`)
    );
  }

  // Flat endpoints (optional)
  getAll(studentId: number): Observable<SubmissionResponseDto[]> {
    return this.api.http.get<SubmissionResponseDto[]>(this.api.url(`/api/students/${studentId}/submissions`));
  }
}
