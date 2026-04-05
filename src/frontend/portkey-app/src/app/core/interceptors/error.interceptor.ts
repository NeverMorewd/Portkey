import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { MessageService } from 'primeng/api';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const msg = inject(MessageService);

  return next(req).pipe(
    catchError(err => {
      if (err.status === 0) {
        msg.add({
          severity: 'error',
          summary: 'Connection Error',
          detail: 'Cannot reach the backend server. Is it running?',
          life: 6000,
        });
      } else if (err.status >= 500) {
        const detail = err.error?.message ?? err.error ?? 'An unexpected server error occurred.';
        msg.add({
          severity: 'error',
          summary: `Server Error (${err.status})`,
          detail: typeof detail === 'string' ? detail : JSON.stringify(detail),
          life: 6000,
        });
      }
      // 4xx errors are handled locally by each component
      return throwError(() => err);
    })
  );
};
