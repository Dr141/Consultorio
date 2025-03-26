import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { CacheService } from '../cache/cache.service';
import { Observable, catchError, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ConsultorioApiService {

  constructor(private http: HttpClient, private cache: CacheService) { }

  refreshToken() {
    //const refreshToken = this.cache.recuperarCookie('RefreshToken');
    //const headers = new HttpHeaders().set('Authorization', `Bearer ${refreshToken}`);
    console.log('oi')
    return this.http.get('autenticacao');    
  }

  cadastro(user: any) {
    return this.observable(this.http.post('cadastroUsuario', user))
  }

  login(email: string, senha: string) {       
    return this.observable(this.http.post('autenticacao', { Email: email, Senha: senha }))
  }

  logout() {
    return this.observable(this.http.delete(`autenticacao`, {}));
  }

  atualizarSenha(newSenha: any) {
    return this.observable(this.http.put('cadastroUsuario', newSenha))
  }

  atualizarSenhaInterno(userNewSenha: any) {
    return this.observable(this.http.post('usuario', userNewSenha))
  }

  obterTodos<t>() {   
    return this.observable(this.http.get<t>('usuario'))
  }

  obterRoles() {
    return this.observable(this.http.get('role'))
  }

  adicionarRole(userRole: any) {
    return this.observable(this.http.post('role', userRole))
  }

  removerRole(userRole: any) {
    return this.observable(this.http.put('role', userRole))
  }

  adicionarClaim(userClaim: any){
    return this.observable(this.http.post('claim', userClaim))
  }

  removerClaim(userClaim: any) {
    return this.observable(this.http.delete('claim', userClaim))
  }

  private observable(resultado: Observable<any>) {
    return resultado.pipe(
      catchError((error: HttpErrorResponse) => {
        return throwError(() => new Error(error.error.message));
    }))
  }
}
