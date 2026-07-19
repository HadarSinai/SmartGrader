import { Injectable } from "@angular/core";
import {
  CreateClassRequestDto,
  FinishYearResultDto,
  SchoolClassResponseDto,
  UpdateClassRequestDto,
} from "@models/class.model";
import { Observable } from "rxjs";
import { ApiClient } from "../core/http/api-client";

@Injectable({ providedIn: "root" })
export class ClassesService {
  constructor(private api: ApiClient) {}

  getAll(includeArchived = false): Observable<SchoolClassResponseDto[]> {
    return this.api.http.get<SchoolClassResponseDto[]>(
      this.api.url("/api/classes"),
      { params: { includeArchived } },
    );
  }

  getById(id: number): Observable<SchoolClassResponseDto> {
    return this.api.http.get<SchoolClassResponseDto>(
      this.api.url(`/api/classes/${id}`),
    );
  }

  create(request: CreateClassRequestDto): Observable<SchoolClassResponseDto> {
    return this.api.http.post<SchoolClassResponseDto>(
      this.api.url("/api/classes"),
      request,
    );
  }

  update(
    id: number,
    request: UpdateClassRequestDto,
  ): Observable<SchoolClassResponseDto> {
    return this.api.http.put<SchoolClassResponseDto>(
      this.api.url(`/api/classes/${id}`),
      request,
    );
  }

  delete(id: number): Observable<void> {
    return this.api.http.delete<void>(this.api.url(`/api/classes/${id}`));
  }

  finishYear(): Observable<FinishYearResultDto> {
    return this.api.http.post<FinishYearResultDto>(
      this.api.url("/api/classes/finish-year"),
      {},
    );
  }
}
