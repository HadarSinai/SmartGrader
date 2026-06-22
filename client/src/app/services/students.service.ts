import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  StudentResponseDto,
  CreateStudentRequestDto,
  UpdateStudentRequestDto
} from '@models/student.model';
import { ApiClient } from '../core/http/api-client';

@Injectable({ providedIn: 'root' })
export class StudentsService {
  constructor(private api: ApiClient) {}

  getAll(): Observable<StudentResponseDto[]> {
    return this.api.http.get<StudentResponseDto[]>(this.api.url('/api/students'));
  }

  getById(id: number): Observable<StudentResponseDto> {
    return this.api.http.get<StudentResponseDto>(this.api.url(`/api/students/${id}`));
  }

  create(request: CreateStudentRequestDto): Observable<StudentResponseDto> {
    return this.api.http.post<StudentResponseDto>(this.api.url('/api/students'), request);
  }

  update(id: number, request: UpdateStudentRequestDto): Observable<StudentResponseDto> {
    return this.api.http.put<StudentResponseDto>(this.api.url(`/api/students/${id}`), request);
  }

  delete(id: number): Observable<void> {
    return this.api.http.delete<void>(this.api.url(`/api/students/${id}`));
  }
}
