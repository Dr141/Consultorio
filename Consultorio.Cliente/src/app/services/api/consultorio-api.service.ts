import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { CacheService } from '../cache/cache.service';
import { Observable, catchError, throwError } from 'rxjs';
import { Agenda, Consulta, Especialidade, Exame, Medico, Paciente, Pessoa, ProntuarioRelatorio } from '../../types/types';

@Injectable({
  providedIn: 'root'
})
export class ConsultorioApiService {

  constructor(private http: HttpClient, private cache: CacheService) { }

  // Autenticação & Usuários
  refreshToken() {
    return this.http.get('/autenticacao');    
  }

  cadastro(user: any) {
    return this.observable(this.http.post('/cadastroUsuario', user));
  }

  login(email: string, senha: string) {       
    return this.observable(this.http.post('/autenticacao', { Email: email, Senha: senha }));
  }

  logout() {
    return this.observable(this.http.delete('/autenticacao', {}));
  }

  atualizarSenha(newSenha: any) {
    return this.observable(this.http.put('/cadastroUsuario', newSenha));
  }

  atualizarSenhaInterno(userNewSenha: any) {
    return this.observable(this.http.post('/usuario', userNewSenha));
  }

  obterTodos<T>() {   
    return this.observable(this.http.get<T>('/usuario'));
  }

  obterRoles() {
    return this.observable(this.http.get<string[]>('/role'));
  }

  adicionarRole(userRole: any) {
    return this.observable(this.http.post('/role', userRole));
  }

  removerRole(userRole: any) {
    return this.observable(this.http.put('/role', userRole));
  }

  adicionarClaim(userClaim: any){
    return this.observable(this.http.post('/claim', userClaim));
  }

  removerClaim(userClaim: any) {
    return this.observable(this.http.delete('/claim', userClaim));
  }

  // Pacientes
  obterPacientes(): Observable<Paciente[]> {
    return this.observable(this.http.get<Paciente[]>('/api/paciente'));
  }

  obterPacientePorId(id: number): Observable<Paciente> {
    return this.observable(this.http.get<Paciente>(`/api/paciente/${id}`));
  }

  criarPaciente(paciente: any): Observable<Paciente> {
    return this.observable(this.http.post<Paciente>('/api/paciente', paciente));
  }

  atualizarPaciente(id: number, paciente: any): Observable<Paciente> {
    return this.observable(this.http.put<Paciente>(`/api/paciente/${id}`, paciente));
  }

  removerPaciente(id: number): Observable<void> {
    return this.observable(this.http.delete<void>(`/api/paciente/${id}`));
  }

  // Médicos
  obterMedicos(): Observable<Medico[]> {
    return this.observable(this.http.get<Medico[]>('/api/medico'));
  }

  obterMedicoPorId(id: number): Observable<Medico> {
    return this.observable(this.http.get<Medico>(`/api/medico/${id}`));
  }

  obterEspecialidades(): Observable<{ id: number; nome: string }[]> {
    return this.observable(this.http.get<{ id: number; nome: string }[]>('/api/medico/especialidades'));
  }

  criarMedico(medico: any): Observable<Medico> {
    return this.observable(this.http.post<Medico>('/api/medico', medico));
  }

  atualizarMedico(id: number, medico: any): Observable<Medico> {
    return this.observable(this.http.put<Medico>(`/api/medico/${id}`, medico));
  }

  removerMedico(id: number): Observable<void> {
    return this.observable(this.http.delete<void>(`/api/medico/${id}`));
  }

  // Agenda
  obterAgendas(): Observable<Agenda[]> {
    return this.observable(this.http.get<Agenda[]>('/api/agenda'));
  }

  obterAgendaPorId(id: number): Observable<Agenda> {
    return this.observable(this.http.get<Agenda>(`/api/agenda/${id}`));
  }

  obterAgendasPorMedicoEData(idMedico: number, data?: string): Observable<Agenda[]> {
    const params = data ? `?data=${data}` : '';
    return this.observable(this.http.get<Agenda[]>(`/api/agenda/medico/${idMedico}${params}`));
  }

  obterAgendasPorPaciente(idPaciente: number): Observable<Agenda[]> {
    return this.observable(this.http.get<Agenda[]>(`/api/agenda/paciente/${idPaciente}`));
  }

  criarAgenda(agenda: any): Observable<Agenda> {
    return this.observable(this.http.post<Agenda>('/api/agenda', agenda));
  }

  atualizarAgenda(id: number, agenda: any): Observable<Agenda> {
    return this.observable(this.http.put<Agenda>(`/api/agenda/${id}`, agenda));
  }

  agendarPaciente(id: number, idPaciente: number): Observable<Agenda> {
    return this.observable(this.http.patch<Agenda>(`/api/agenda/${id}/agendar/${idPaciente}`, {}));
  }

  liberarHorarioAgenda(id: number): Observable<Agenda> {
    return this.observable(this.http.patch<Agenda>(`/api/agenda/${id}/liberar`, {}));
  }

  removerAgenda(id: number): Observable<void> {
    return this.observable(this.http.delete<void>(`/api/agenda/${id}`));
  }

  // Consultas
  obterConsultas(): Observable<Consulta[]> {
    return this.observable(this.http.get<Consulta[]>('/api/consulta'));
  }

  obterConsultaPorId(id: number): Observable<Consulta> {
    return this.observable(this.http.get<Consulta>(`/api/consulta/${id}`));
  }

  obterConsultasPorPaciente(idPaciente: number): Observable<Consulta[]> {
    return this.observable(this.http.get<Consulta[]>(`/api/consulta/paciente/${idPaciente}`));
  }

  obterConsultasPorMedico(idMedico: number): Observable<Consulta[]> {
    return this.observable(this.http.get<Consulta[]>(`/api/consulta/medico/${idMedico}`));
  }

  criarConsulta(consulta: any): Observable<Consulta> {
    return this.observable(this.http.post<Consulta>('/api/consulta', consulta));
  }

  atualizarObservacaoConsulta(id: number, observacao: string): Observable<Consulta> {
    return this.observable(this.http.put<Consulta>(`/api/consulta/${id}/observacao`, JSON.stringify(observacao), {
      headers: { 'Content-Type': 'application/json' }
    }));
  }

  removerConsulta(id: number): Observable<void> {
    return this.observable(this.http.delete<void>(`/api/consulta/${id}`));
  }

  // Exames
  obterExames(): Observable<Exame[]> {
    return this.observable(this.http.get<Exame[]>('/api/exame'));
  }

  obterExamePorId(id: number): Observable<Exame> {
    return this.observable(this.http.get<Exame>(`/api/exame/${id}`));
  }

  obterExamesPorPaciente(idPaciente: number): Observable<Exame[]> {
    return this.observable(this.http.get<Exame[]>(`/api/exame/paciente/${idPaciente}`));
  }

  criarExame(exame: any): Observable<Exame> {
    return this.observable(this.http.post<Exame>('/api/exame', exame));
  }

  atualizarExame(id: number, exame: any): Observable<Exame> {
    return this.observable(this.http.put<Exame>(`/api/exame/${id}`, exame));
  }

  registrarResultadoExame(id: number, resultado: string, idMedico?: number | null): Observable<Exame> {
    return this.observable(this.http.patch<Exame>(`/api/exame/${id}/resultado`, { resultado, idMedico }));
  }

  removerExame(id: number): Observable<void> {
    return this.observable(this.http.delete<void>(`/api/exame/${id}`));
  }

  // Prontuário do Paciente
  obterProntuario(
    idPaciente: number,
    filtros?: { idMedico?: number | null; dataInicio?: string | null; dataFim?: string | null }
  ): Observable<ProntuarioRelatorio> {
    const params: string[] = [];
    if (filtros?.idMedico) {
      params.push(`idMedico=${filtros.idMedico}`);
    }
    if (filtros?.dataInicio) {
      params.push(`dataInicio=${encodeURIComponent(filtros.dataInicio)}`);
    }
    if (filtros?.dataFim) {
      params.push(`dataFim=${encodeURIComponent(filtros.dataFim)}`);
    }
    const query = params.length > 0 ? `?${params.join('&')}` : '';
    return this.observable(this.http.get<ProntuarioRelatorio>(`/api/prontuario/paciente/${idPaciente}${query}`));
  }

  // Pessoas
  obterPessoas(): Observable<Pessoa[]> {
    return this.observable(this.http.get<Pessoa[]>('/api/pessoa'));
  }

  obterPessoaPorId(id: number): Observable<Pessoa> {
    return this.observable(this.http.get<Pessoa>(`/api/pessoa/${id}`));
  }

  private observable<T>(resultado: Observable<T>): Observable<T> {
    return resultado.pipe(
      catchError((error: HttpErrorResponse) => {
        // Mantém o status HTTP no erro para que as telas possam reagir (ex.: 401 → login).
        const erro = Object.assign(new Error(this.mensagemErro(error)), { status: error?.status ?? 0 });
        return throwError(() => erro);
      })
    );
  }

  /** Traduz falhas HTTP em mensagens claras para o usuário final. */
  private mensagemErro(error: HttpErrorResponse): string {
    if (!error || error.status === 0) {
      return 'Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.';
    }

    const corpo = error.error;
    if (corpo && typeof corpo === 'object') {
      if (typeof corpo.message === 'string' && corpo.message.trim()) return corpo.message;
      if (corpo.errors && typeof corpo.errors === 'object') {
        const primeira = Object.values(corpo.errors as Record<string, unknown>)
          .flatMap(v => (Array.isArray(v) ? v : [v]))
          .find(v => typeof v === 'string' && v.trim());
        if (primeira) return String(primeira);
      }
    }
    if (typeof corpo === 'string' && corpo.trim() && corpo.length <= 300 && !corpo.trim().startsWith('<')) {
      return corpo;
    }

    switch (error.status) {
      case 400: return 'Os dados enviados são inválidos. Revise os campos e tente novamente.';
      case 401: return 'Sua sessão expirou. Faça login novamente.';
      case 403: return 'Você não tem permissão para realizar esta ação.';
      case 404: return 'Registro não encontrado. Ele pode ter sido removido.';
      case 409: return 'Não foi possível salvar porque o registro conflita com dados existentes.';
      default:
        return error.status >= 500
          ? 'O servidor encontrou um problema. Tente novamente em instantes.'
          : 'Não foi possível concluir a requisição. Tente novamente.';
    }
  }
}
