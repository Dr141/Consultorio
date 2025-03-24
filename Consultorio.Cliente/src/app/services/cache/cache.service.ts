import { Injectable } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';

@Injectable({
  providedIn: 'root'
})
export class CacheService {

  constructor(private cookieService: CookieService) { }

  salvarCookie(token: string, value: string, expiracao?: Date): void {
    this.cookieService.set(token, value, expiracao ?? 1);
  }

  recuperarCookie(token: string): string | undefined {
    return this.cookieService.get(token);
  }

  removerCookie(token: string) {
    this.cookieService.delete(token);
  }

  RemoveTodosCookies() {
    this.cookieService.deleteAll()
  }
}
