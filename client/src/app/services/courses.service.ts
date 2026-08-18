import { Injectable } from "@angular/core";
import {
  CourseResponseDto,
  CreateCourseRequestDto,
  UpdateCourseRequestDto,
} from "@models/course.model";
import { Observable } from "rxjs";
import { ApiClient } from "../core/http/api-client";

@Injectable({ providedIn: "root" })
export class CoursesService {
  constructor(private api: ApiClient) {}

  getAll(): Observable<CourseResponseDto[]> {
    return this.api.http.get<CourseResponseDto[]>(this.api.url("/api/courses"));
  }

  getById(id: number): Observable<CourseResponseDto> {
    return this.api.http.get<CourseResponseDto>(
      this.api.url(`/api/courses/${id}`),
    );
  }

  create(request: CreateCourseRequestDto): Observable<CourseResponseDto> {
    return this.api.http.post<CourseResponseDto>(
      this.api.url("/api/courses"),
      request,
    );
  }

  update(
    id: number,
    request: UpdateCourseRequestDto,
  ): Observable<CourseResponseDto> {
    return this.api.http.put<CourseResponseDto>(
      this.api.url(`/api/courses/${id}`),
      request,
    );
  }

  delete(id: number): Observable<void> {
    return this.api.http.delete<void>(this.api.url(`/api/courses/${id}`));
  }
}
