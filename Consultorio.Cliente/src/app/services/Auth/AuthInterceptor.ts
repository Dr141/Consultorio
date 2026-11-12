import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { ConsultorioApiService } from '../api/consultorio-api.service';

export const AuthInterceptor: HttpInterceptorFn = (req: HttpRequest<any>, next: HttpHandlerFn): Observable<HttpEvent<any>> => {
  const consultorioService = inject(ConsultorioApiService);
  const router = inject(Router);

  // O portal do paciente tem sessão própria (cabeçalho X-Portal-Token) e trata o 401 por conta própria.
  if (req.url.includes('/api/portal')) {
    return next(req);
  }

  // Garante envio de cookies de sessão em todas as requisições
  const authReq = req.clone({ withCredentials: true });

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // Se não autenticado em chamadas regulares, tenta refresh
      if (error.status === 401 && !req.url.includes('/autenticacao') && !req.url.includes('/cadastroUsuario')) {
        return consultorioService.refreshToken().pipe(
          switchMap(() => {            
            return next(authReq); // Após o refresh, reenvia a requisição original
          }),
          catchError(refreshError => {
            // Se o refresh também falhou, redireciona para login e encerra o fluxo
            router.navigate(['/login']);
            return throwError(() => refreshError);
          })
        );
      }

      // Se 401 em refresh ou outro endpoint crítico, redireciona ao login
      if (error.status === 401 && req.url.includes('/autenticacao') && req.method === 'GET') {
        router.navigate(['/login']);
      }

      return throwError(() => error);
    })
  );
};
