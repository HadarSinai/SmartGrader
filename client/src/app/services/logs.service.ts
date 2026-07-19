import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { ApiClient } from "../core/http/api-client";
import { DeleteOldLogsResultDto, LogResponseDto } from "../models/log.model";

@Injectable({ providedIn: "root" })
export class LogsService {
  constructor(private api: ApiClient) {}

  getAll(): Observable<LogResponseDto[]> {
    return this.api.http.get<LogResponseDto[]>(this.api.url("/api/logs"));
  }

  deleteOld(days: number): Observable<DeleteOldLogsResultDto> {
    return this.api.http.delete<DeleteOldLogsResultDto>(
      this.api.url(`/api/logs/old?days=${days}`),
    );
  }
}
