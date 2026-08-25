import { Injectable } from "@angular/core";
import {
  CreateTeacherRequestDto,
  ResetTeacherPasswordRequestDto,
  TeacherResponseDto,
  UpdateTeacherRequestDto,
} from "@models/teacher.model";
import { Observable } from "rxjs";
import { ApiClient } from "../core/http/api-client";

@Injectable({ providedIn: "root" })
export class TeachersService {
  constructor(private api: ApiClient) {}

  getAll(): Observable<TeacherResponseDto[]> {
    return this.api.http.get<TeacherResponseDto[]>(
      this.api.url("/api/teachers"),
    );
  }

  getById(id: number): Observable<TeacherResponseDto> {
    return this.api.http.get<TeacherResponseDto>(
      this.api.url(`/api/teachers/${id}`),
    );
  }

  create(request: CreateTeacherRequestDto): Observable<TeacherResponseDto> {
    return this.api.http.post<TeacherResponseDto>(
      this.api.url("/api/teachers"),
      request,
    );
  }

  update(
    id: number,
    request: UpdateTeacherRequestDto,
  ): Observable<TeacherResponseDto> {
    return this.api.http.put<TeacherResponseDto>(
      this.api.url(`/api/teachers/${id}`),
      request,
    );
  }

  resetPassword(
    id: number,
    request: ResetTeacherPasswordRequestDto,
  ): Observable<void> {
    return this.api.http.post<void>(
      this.api.url(`/api/teachers/${id}/password`),
      request,
    );
  }

  delete(id: number): Observable<void> {
    return this.api.http.delete<void>(this.api.url(`/api/teachers/${id}`));
  }
}
