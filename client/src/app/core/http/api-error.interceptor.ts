import { HttpErrorResponse, HttpInterceptorFn } from "@angular/common/http";
import { inject } from "@angular/core";
import { MessageService } from "primeng/api";
import { catchError, throwError } from "rxjs";

export const apiErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(MessageService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      // Auth pages show inline errors (per master-spec); 401 is handled by authInterceptor
      const isAuthRequest =
        req.url.includes("/api/auth/login") ||
        req.url.includes("/api/auth/register-teacher");

      // 404 on a lesson-result lookup is an expected "no result yet" state
      // (student area shows "בתהליך") — not an error worth a toast.
      const isExpectedMissingLessonResult =
        err.status === 404 && /\/api\/lesson-results\/\d+\/\d+/.test(req.url);

      if (
        isAuthRequest ||
        err.status === 401 ||
        isExpectedMissingLessonResult
      ) {
        return throwError(() => err);
      }

      const detail =
        err.error?.detail ||
        err.error?.message ||
        (typeof err.error === "string" ? err.error : null) ||
        err.message ||
        "Server error";

      toast.add({
        severity: "error",
        summary: `HTTP ${err.status || ""}`.trim(),
        detail,
        life: 5000,
      });

      return throwError(() => err);
    }),
  );
};
