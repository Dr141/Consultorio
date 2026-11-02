import { Injectable } from '@angular/core';
import { CacheService } from '../cache/cache.service';

@Injectable({
  providedIn: 'root'
})
export class RoleService {

  constructor(private cache: CacheService) { }

  getRole() {    
    return this.cache.recuperarCookie('Roles')
  }  
}
