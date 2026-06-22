import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { environment } from "../../../environments/environment";

@Injectable({ providedIn: "root" })
export class ApiClient {
  constructor(public http: HttpClient) {}

  url(path: string) {
    const base = (environment.apiBaseUrl || "").replace(/\/$/, "");
    const p = path.startsWith("/") ? path : `/${path}`;
    return base ? `${base}${p}` : p;
  }
}
