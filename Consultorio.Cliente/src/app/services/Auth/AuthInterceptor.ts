import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { ConsultorioApiService } from '../api/consultorio-api.service';

export const AuthInterceptor: HttpInterceptorFn = (req: HttpRequest<any>, next: HttpHandlerFn): Observable<HttpEvent<any>> => {
  const consultorioService = inject(ConsultorioApiService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        return consultorioService.refreshToken().pipe(
          switchMap(() => {            
            return next(req); // Após o refresh, reenvia a requisição original
          }),
          catchError(refreshError => {
            return throwError(() => refreshError);
          })
        );
      }

      return throwError(() => error);
    })
  );
};
