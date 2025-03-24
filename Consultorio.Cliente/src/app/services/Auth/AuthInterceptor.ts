import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { ConsultorioApiService } from '../api/consultorio-api.service';
import { CacheService } from '../cache/cache.service';

export const AuthInterceptor: HttpInterceptorFn = (req: HttpRequest<any>, next: HttpHandlerFn): Observable<HttpEvent<any>> => {
  const consultorioService = inject(ConsultorioApiService);
  const cacheService = inject(CacheService);
  const authToken = cacheService.recuperarCookie('AccessToken');

  // Clona a requisição e adiciona o token no header
  const authReq = authToken
    ? req.clone({ setHeaders: { Authorization: `Bearer ${authToken}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        return consultorioService.refreshToken().pipe(
          switchMap(() => {
            // Após o refresh, pega o novo token atualizado no localStorage
            const newToken = cacheService.recuperarCookie('AccessToken');

            if (!newToken) {
              return throwError(() => new Error('Falha ao renovar token.'));
            }

            // Clona a requisição original e adiciona o novo token
            const newAuthReq = req.clone({
              setHeaders: { Authorization: `Bearer ${newToken}` }
            });

            return next(newAuthReq); // Reenvia a requisição original
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
