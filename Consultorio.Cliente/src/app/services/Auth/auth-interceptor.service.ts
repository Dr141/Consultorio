import { HttpErrorResponse, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { ConsultorioApiService } from '../api/consultorio-api.service';

@Injectable({
  providedIn: 'root'
})
export class AuthInterceptorService implements HttpInterceptor {

  constructor(private consultorioService: ConsultorioApiService) { }

  intercept(req: HttpRequest<any>, next: HttpHandler) {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          return this.consultorioService.refreshToken().pipe(
            switchMap(() => {
              return next.handle(req); // Reenvia a requisição original
            })
          );
        }
        return throwError(() => error);
      })
    );
  }
}
