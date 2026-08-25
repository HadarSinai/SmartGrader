import { HttpErrorResponse, HttpInterceptorFn } from "@angular/common/http";
import { inject } from "@angular/core";
import { MessageService } from "primeng/api";
import { catchError, throwError } from "rxjs";

export const apiErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(MessageService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      // Auth pages show inline errors (per master-spec); 401 is handled by authInterceptor.
      // register-teacher ירד מהרשימה יחד עם המסך שלו — הרשמה עצמית נסגרה.
      //
      // ⚠️ forgot-password/reset-password חייבות להיות כאן ולא רק מטעמי עיצוב: הן מסכים
      // ציבוריים ללא toast גלובלי, וטוסט שגיאה על forgot-password היה מסגיר בדיוק את מה
      // שהשרת נמנע מלומר — שהכתובת קיימת או לא קיימת.
      const isAuthRequest = [
        "/api/auth/login",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
      ].some((path) => req.url.includes(path));

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

      // 403 arrives from the API with an empty body, so the generic branch below would show
      // the raw "Http failure response for ..." string. Give it real copy instead.
      if (err.status === 403) {
        toast.add({
          severity: "error",
          summary: "אין הרשאה",
          detail: "אין לך הרשאה לצפות בתוכן הזה או לבצע את הפעולה הזו.",
          life: 5000,
        });
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
