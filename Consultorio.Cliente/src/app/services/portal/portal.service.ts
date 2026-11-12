import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, tap, throwError } from 'rxjs';
import {
  Cobranca, DocumentoClinico, PortalAgendamento, PortalExame, PortalMedicoHorarios, PortalResumo, PortalSessao, Recibo
} from '../../types/types';

const CHAVE_SESSAO = 'portal-paciente';

/**
 * Cliente da API do portal do paciente. A sessão (token curto) fica no sessionStorage da aba:
 * fechar a aba encerra o acesso, o que é o comportamento esperado em computadores compartilhados.
 */
@Injectable({ providedIn: 'root' })
export class PortalService {
  constructor(private http: HttpClient, private router: Router) {}

  // ---------- Sessão ----------

  get sessao(): PortalSessao | null {
    try {
      const bruto = sessionStorage.getItem(CHAVE_SESSAO);
      if (!bruto) return null;
      const sessao = JSON.parse(bruto) as PortalSessao;
      if (!sessao?.token || new Date(sessao.expiraEm).getTime() <= Date.now()) {
        sessionStorage.removeItem(CHAVE_SESSAO);
        return null;
      }
      return sessao;
    } catch {
      return null;
    }
  }

  get autenticado(): boolean {
    return this.sessao !== null;
  }

  private salvarSessao(sessao: PortalSessao) {
    try {
      sessionStorage.setItem(CHAVE_SESSAO, JSON.stringify(sessao));
    } catch {
      // Navegação privada sem storage: a sessão dura só enquanto a página estiver aberta.
    }
    this.sessaoMemoria = sessao;
  }

  private sessaoMemoria: PortalSessao | null = null;

  private get token(): string {
    return this.sessao?.token ?? this.sessaoMemoria?.token ?? '';
  }

  private get headers(): HttpHeaders {
    return new HttpHeaders({ 'X-Portal-Token': this.token });
  }

  limparSessao() {
    this.sessaoMemoria = null;
    try { sessionStorage.removeItem(CHAVE_SESSAO); } catch { /* sem storage */ }
  }

  solicitarCodigo(cpf: string, dtNasc: string): Observable<{ chave: string; mensagem: string }> {
    return this.tratar(this.http.post<{ chave: string; mensagem: string }>('/api/portal/acesso/codigo', { cpf, dtNasc }));
  }

  validarCodigo(chave: string, codigo: string): Observable<PortalSessao> {
    return this.tratar(this.http.post<PortalSessao>('/api/portal/acesso/sessao', { chave, codigo }))
      .pipe(tap(sessao => this.salvarSessao(sessao)));
  }

  sair(): Observable<void> {
    const pedido = this.http.delete<void>('/api/portal/acesso/sessao', { headers: this.headers });
    this.limparSessao();
    return pedido.pipe(catchError(() => throwError(() => new Error('Sessão encerrada.'))));
  }

  // ---------- Área do paciente ----------

  resumo(): Observable<PortalResumo> {
    return this.get<PortalResumo>('/api/portal/resumo');
  }

  horarios(): Observable<PortalMedicoHorarios[]> {
    return this.get<PortalMedicoHorarios[]>('/api/portal/horarios');
  }

  agendar(idAgenda: number): Observable<PortalAgendamento> {
    return this.tratar(this.http.post<PortalAgendamento>(`/api/portal/agendamentos/${idAgenda}`, {}, { headers: this.headers }));
  }

  confirmar(idAgenda: number): Observable<PortalAgendamento> {
    return this.tratar(this.http.patch<PortalAgendamento>(`/api/portal/agendamentos/${idAgenda}/confirmar`, {}, { headers: this.headers }));
  }

  desmarcar(idAgenda: number): Observable<void> {
    return this.tratar(this.http.delete<void>(`/api/portal/agendamentos/${idAgenda}`, { headers: this.headers }));
  }

  exames(): Observable<PortalExame[]> {
    return this.get<PortalExame[]>('/api/portal/exames');
  }

  arquivoExame(idExame: number, idAnexo: number): Observable<Blob> {
    return this.tratar(this.http.get(`/api/portal/exames/${idExame}/anexos/${idAnexo}`, { headers: this.headers, responseType: 'blob' }));
  }

  documentos(): Observable<DocumentoClinico[]> {
    return this.get<DocumentoClinico[]>('/api/portal/documentos');
  }

  documento(id: number): Observable<DocumentoClinico> {
    return this.get<DocumentoClinico>(`/api/portal/documentos/${id}`);
  }

  pdfDocumento(id: number): Observable<Blob> {
    return this.tratar(this.http.get(`/api/portal/documentos/${id}/pdf`, { headers: this.headers, responseType: 'blob' }));
  }

  cobrancas(): Observable<Cobranca[]> {
    return this.get<Cobranca[]>('/api/portal/cobrancas');
  }

  recibo(id: number): Observable<Recibo> {
    return this.get<Recibo>(`/api/portal/cobrancas/${id}/recibo`);
  }

  private get<T>(url: string): Observable<T> {
    return this.tratar(this.http.get<T>(url, { headers: this.headers }));
  }

  /** Mensagens claras e, em 401, volta para a tela de entrada do portal. */
  private tratar<T>(pedido: Observable<T>): Observable<T> {
    return pedido.pipe(
      catchError((erro: HttpErrorResponse) => {
        if (erro.status === 401) {
          this.limparSessao();
          this.router.navigate(['/portal'], { queryParams: { expirou: 1 } });
        }
        const corpo = erro.error;
        let mensagem = 'Não foi possível concluir. Tente novamente.';
        if (corpo && typeof corpo === 'object' && typeof corpo.message === 'string') mensagem = corpo.message;
        else if (corpo?.errors) {
          const primeira = Object.values(corpo.errors as Record<string, unknown>).flat().find(v => typeof v === 'string');
          if (primeira) mensagem = String(primeira);
        } else if (erro.status === 0) mensagem = 'Sem conexão com a clínica. Verifique sua internet.';
        else if (erro.status === 404) mensagem = 'Não encontrado.';
        else if (erro.status === 429) mensagem = 'Muitas tentativas em pouco tempo. Aguarde alguns minutos.';
        return throwError(() => Object.assign(new Error(mensagem), { status: erro.status }));
      })
    );
  }
}
