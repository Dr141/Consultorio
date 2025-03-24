import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { CacheService } from '../cache/cache.service';
import { Observable, catchError, firstValueFrom, throwError } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ConsultorioApiService {

  constructor(private http: HttpClient, private cache: CacheService) { }

  refreshToken() {
    return this.http.get('Autenticacao', { withCredentials: true });    
  }

  cadastro(user: any) {
    return this.observable(this.http.post('cadastroUsuario', user, { withCredentials: true }))
  }

  login(email: string, senha: string) {       
    return this.observable(this.http.post('autenticacao', { Email: email, Senha: senha },{ withCredentials: true }))
  }

  logout() {
    return this.observable(this.http.put(`autenticacao`, {}, { withCredentials: true }));
  }

  atualizarSenha(newSenha: any) {
    return this.observable(this.http.put('cadastroUsuario', newSenha, { withCredentials: true }))
  }

  async atualizarSenhaInterno(userNewSenha: any): Promise<any> {
    var result = this.http.put('/api/Usuario/AtualizarSenha', userNewSenha, this.obterHeaders())

    return await this.observable(result)
  }

  async obterTodos(): Promise<any> {
    var result = this.http.get('/api/Usuario/ObterTodosUsuarios', this.obterHeaders())

    return await this.observable(result)
  }

  async adicionarRole(userRole: any): Promise<any>{
    var result = this.http.post('/api/Usuario/AdicionarRole', userRole, this.obterHeaders())

    return await this.observable(result)
  }

  async removerRole(userRole: any): Promise<any> {
    var result = this.http.put('/api/Usuario/RemoverRole', userRole, this.obterHeaders())

    return await this.observable(result)
  }

  async adicionarClaim(userClaim: any): Promise<any> {
    var result = this.http.post('/api/Usuario/AdicionarClaim', userClaim, this.obterHeaders())

    return await this.observable(result)
  }

  async removerClaim(userClaim: any): Promise<any> {
    var result = this.http.put('/api/Usuario/RemoverClaim', userClaim, this.obterHeaders())

    return await this.observable(result)
  }

  private observable(resultado: Observable<any>) {
    return resultado.pipe(
      catchError((error: HttpErrorResponse) => {
        return throwError(() => new Error(error.error.message));
    }))
  }

  salvarAutenticacao(usuarioAut: any) {
    if (usuarioAut ?? undefined) {
      this.cache.salvarCookie('accessToken', usuarioAut.accessToken, this.dataExpiracao(usuarioAut.accessToken.split('.')[1]))
      this.cache.salvarCookie('refreshToken', usuarioAut.refreshToken, this.dataExpiracao(usuarioAut.refreshToken.split('.')[1]))
    }
  }

  private dataExpiracao(payload: string): Date {
    var payloadToJson = JSON.parse(atob(payload))
    return new Date(payloadToJson.exp * 1000)
  }

  private obterHeaders(refresh: boolean = false) {
    const accessToken = this.cache.recuperarCookie(refresh ? 'refreshToken' : 'accessToken')
    const header = new HttpHeaders({ 'Authorization': `Bearer ${accessToken}` })
    return { headers: header }
  }
}
