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
    const accessToken = this.cache.recuperarCookie('accessToken')
    const headers = new HttpHeaders({ 'Authorization': `Bearer ${accessToken}` })

    var request = this.http.post('/api/Autenticacao/AtualizarToken', { headers: headers })
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
    var result = this.http.post('/api/Autenticacao/Login', {
      Email: email,
      Senha: senha
    })

    return await this.observable(result)
  }

  async atualizarSenha(newSenha: any): Promise<any> {
    var result = this.http.put('/api/Autenticacao/AtualizarSenha', newSenha)

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
      this.cache.salvarCookie('accessToken', usuarioAut.refreshToken, this.dataExpiracao(usuarioAut.refreshToken.split('.')[1]))
    }
  }

  private dataExpiracao(payload: string): Date {
    var payloadToJson = JSON.parse(atob(payload))
    return new Date(payloadToJson.exp * 1000)
  }
}
