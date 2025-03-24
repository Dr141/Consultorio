import { Injectable } from '@angular/core';
import { CacheService } from '../cache/cache.service';

@Injectable({
  providedIn: 'root'
})
export class RoleService {

  constructor(private cache: CacheService) { }

  getRole() {
    return this.obterPayload()
  }
  private obterPayload() {
    var payloadBase64 = this.cache.recuperarCookie('AccessToken')?.split('.')[1]
    if (payloadBase64) {
      return JSON.parse(window.atob(payloadBase64)).role
    }
  }
}
