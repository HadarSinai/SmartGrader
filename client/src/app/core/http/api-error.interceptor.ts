import { HttpErrorResponse, HttpInterceptorFn } from "@angular/common/http";
import { inject } from "@angular/core";
import { MessageService } from "primeng/api";
import { catchError, throwError } from "rxjs";

export const apiErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(MessageService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
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
