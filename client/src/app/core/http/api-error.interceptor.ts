import { Injectable } from '@angular/core';
import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { MessageService } from 'primeng/api';

@Injectable()
export class ApiErrorInterceptor implements HttpInterceptor {
  constructor(private toast: MessageService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((err: HttpErrorResponse) => {
        const detail =
          err.error?.detail ||
          err.error?.message ||
          (typeof err.error === 'string' ? err.error : null) ||
          err.message ||
          'Server error';

        this.toast.add({
          severity: 'error',
          summary: `HTTP ${err.status || ''}`.trim(),
          detail,
          life: 5000
        });

        return throwError(() => err);
      })
    );
  }
}
