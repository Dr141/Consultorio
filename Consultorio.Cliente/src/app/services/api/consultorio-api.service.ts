import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { CacheService } from '../cache/cache.service';
import { Observable, firstValueFrom } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ConsultorioApiService {

  constructor(private http: HttpClient, private cache: CacheService) { }

  async refreshLogin() {
    var request = this.http.post('/api/Autenticacao/AtualizarToken', this.obterHeaders())

    await this.observable(request)
      .then(result => {
        this.salvarAutenticacao(result)
      })
      .catch(error => {
        console.error(error)
      })
  }

  async cadastro(user: any): Promise<any> {
    var result = this.http.post('/api/Autenticacao/Cadastro', user)

    return await this.observable(result)
  }

  async login(email: string, senha: string): Promise<any> {
    var result = this.http.post('autenticacao', {
      Email: email,
      Senha: senha
    })

    return await this.observable(result)
  }

  async atualizarSenha(newSenha: any): Promise<any> {
    var result = this.http.put('/api/Autenticacao/AtualizarSenha', newSenha, this.obterHeaders())

    return await this.observable(result)
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

  private async observable(resultado: Observable<any>): Promise<any> {
    try {
      return await firstValueFrom(resultado)
    }
    catch (error) {
      return error
    }
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
