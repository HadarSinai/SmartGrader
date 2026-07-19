import { Injectable } from "@angular/core";
import {
  CreateStudentRequestDto,
  ImportStudentsResultDto,
  StudentResponseDto,
  UpdateStudentRequestDto,
} from "@models/student.model";
import { Observable } from "rxjs";
import { ApiClient } from "../core/http/api-client";

@Injectable({ providedIn: "root" })
export class StudentsService {
  constructor(private api: ApiClient) {}

  getAll(includeArchived = false): Observable<StudentResponseDto[]> {
    return this.api.http.get<StudentResponseDto[]>(
      this.api.url("/api/students"),
      { params: { includeArchived } },
    );
  }

  getById(id: number): Observable<StudentResponseDto> {
    return this.api.http.get<StudentResponseDto>(
      this.api.url(`/api/students/${id}`),
    );
  }

  create(request: CreateStudentRequestDto): Observable<StudentResponseDto> {
    return this.api.http.post<StudentResponseDto>(
      this.api.url("/api/students"),
      request,
    );
  }

  update(
    id: number,
    request: UpdateStudentRequestDto,
  ): Observable<StudentResponseDto> {
    return this.api.http.put<StudentResponseDto>(
      this.api.url(`/api/students/${id}`),
      request,
    );
  }

  delete(id: number): Observable<void> {
    return this.api.http.delete<void>(this.api.url(`/api/students/${id}`));
  }

  exportExcel(): Observable<Blob> {
    return this.api.http.get(this.api.url("/api/students/export"), {
      responseType: "blob",
    });
  }

  importExcel(file: File): Observable<ImportStudentsResultDto> {
    const formData = new FormData();
    formData.append("file", file, file.name);
    return this.api.http.post<ImportStudentsResultDto>(
      this.api.url("/api/students/import"),
      formData,
    );
  }
}
