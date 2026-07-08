import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // If server is unreachable (0) or explicitly returns a critical 5xx server error
      if (error.status === 0 || error.status >= 500) {
        if (!router.url.startsWith('/error') && !window.location.pathname.startsWith('/error')) {
          const currentUrl = window.location.pathname + window.location.search;
          router.navigate(['/error'], { queryParams: { code: error.status || 500, returnUrl: currentUrl } });
        }
      }
      return throwError(() => error);
    })
  );
};
