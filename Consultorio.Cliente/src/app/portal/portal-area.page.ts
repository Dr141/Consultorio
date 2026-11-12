import { ChangeDetectorRef, Component } from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { FeedbackService } from '../services/feedback/feedback.service';
import { PortalService } from '../services/portal/portal.service';
import {
  Cobranca, DocumentoClinico, ExameAnexo, PortalAgendamento, PortalExame, PortalHorario, PortalMedicoHorarios, PortalResumo
} from '../types/types';

type Aba = 'agenda' | 'agendar' | 'exames' | 'documentos' | 'pagamentos';

/** Área do paciente no portal. Tudo é carregado sob demanda, por aba. */
@Component({
  selector: 'app-portal-area',
  templateUrl: './portal-area.page.html',
  styleUrls: ['./portal.scss'],
  standalone: false
})
export class PortalAreaPage {
  aba: Aba = 'agenda';
  carregando = false;
  resumo: PortalResumo | null = null;
  medicos: PortalMedicoHorarios[] | null = null;
  especialidade: string | null = null;
  exames: PortalExame[] | null = null;
  documentos: DocumentoClinico[] | null = null;
  cobrancas: Cobranca[] | null = null;
  processando = 0;

  constructor(
    private portal: PortalService,
    private feedback: FeedbackService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ionViewWillEnter() {
    if (!this.portal.autenticado) {
      this.router.navigate(['/portal'], { replaceUrl: true });
      return;
    }
    this.carregarResumo();
    if (this.aba !== 'agenda') this.carregarAba(this.aba, true);
  }

  mudarAba(valor: string | undefined) {
    this.aba = (valor as Aba) || 'agenda';
    this.carregarAba(this.aba);
  }

  private concluir = () => {
    this.carregando = false;
    this.cdr.detectChanges();
  };

  carregarResumo() {
    this.carregando = true;
    this.portal.resumo().pipe(finalize(this.concluir)).subscribe({
      next: (r) => this.resumo = r,
      error: (e) => this.feedback.erro(e)
    });
  }

  carregarAba(aba: Aba, forcar = false) {
    switch (aba) {
      case 'agendar':
        if (this.medicos && !forcar) return;
        this.carregando = true;
        this.portal.horarios().pipe(finalize(this.concluir)).subscribe({ next: (m) => this.medicos = m, error: (e) => this.feedback.erro(e) });
        break;
      case 'exames':
        if (this.exames && !forcar) return;
        this.carregando = true;
        this.portal.exames().pipe(finalize(this.concluir)).subscribe({ next: (x) => this.exames = x, error: (e) => this.feedback.erro(e) });
        break;
      case 'documentos':
        if (this.documentos && !forcar) return;
        this.carregando = true;
        this.portal.documentos().pipe(finalize(this.concluir)).subscribe({ next: (d) => this.documentos = d, error: (e) => this.feedback.erro(e) });
        break;
      case 'pagamentos':
        if (this.cobrancas && !forcar) return;
        this.carregando = true;
        this.portal.cobrancas().pipe(finalize(this.concluir)).subscribe({ next: (c) => this.cobrancas = c, error: (e) => this.feedback.erro(e) });
        break;
    }
  }

  atualizar(evento: Event) {
    this.carregarResumo();
    this.carregarAba(this.aba, true);
    setTimeout(() => (evento.target as HTMLIonRefresherElement | null)?.complete(), 600);
  }

  async sair() {
    this.portal.sair().subscribe({ error: () => undefined });
    this.router.navigate(['/portal'], { replaceUrl: true });
  }

  // ---------- Agenda ----------

  dataLonga(valor: string): string {
    const d = new Date(valor);
    return d.toLocaleDateString('pt-BR', { weekday: 'long', day: '2-digit', month: 'long' }) +
      ' às ' + d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
  }

  rotuloStatus(status?: string | null): string {
    switch (status) {
      case 'Agendado': return 'Agendado';
      case 'Confirmado': return 'Confirmado';
      case 'Aguardando': return 'Na recepção';
      case 'EmAtendimento': return 'Em atendimento';
      case 'Atendido': return 'Realizado';
      case 'Faltou': return 'Não compareceu';
      default: return status ?? '';
    }
  }

  corStatus(status?: string | null): string {
    switch (status) {
      case 'Confirmado': return 'success';
      case 'Agendado': return 'primary';
      case 'Atendido': return 'medium';
      case 'Faltou': return 'danger';
      default: return 'warning';
    }
  }

  confirmar(a: PortalAgendamento) {
    this.processando = a.id;
    this.portal.confirmar(a.id).pipe(finalize(() => { this.processando = 0; this.cdr.detectChanges(); })).subscribe({
      next: () => {
        this.feedback.sucesso('Presença confirmada. Obrigado!');
        this.carregarResumo();
      },
      error: (e) => this.feedback.erro(e)
    });
  }

  async desmarcar(a: PortalAgendamento) {
    const ok = await this.feedback.confirmar({
      titulo: 'Desmarcar atendimento?',
      mensagem: `${this.dataLonga(a.dataHora)}${a.nomeMedico ? ' com ' + a.nomeMedico : ''}. O horário ficará livre para outra pessoa.`,
      textoConfirmar: 'Desmarcar',
      textoCancelar: 'Manter',
      destrutivo: true
    });
    if (!ok) return;
    this.processando = a.id;
    this.portal.desmarcar(a.id).pipe(finalize(() => { this.processando = 0; this.cdr.detectChanges(); })).subscribe({
      next: () => {
        this.feedback.sucesso('Atendimento desmarcado.');
        this.medicos = null;
        this.carregarResumo();
      },
      error: (e) => this.feedback.erro(e)
    });
  }

  // ---------- Agendar ----------

  get especialidades(): string[] {
    const todas = (this.medicos ?? []).flatMap(m => m.especialidades);
    return [...new Set(todas)].sort((a, b) => a.localeCompare(b, 'pt-BR'));
  }

  get medicosFiltrados(): PortalMedicoHorarios[] {
    return (this.medicos ?? []).filter(m => !this.especialidade || m.especialidades.includes(this.especialidade));
  }

  /** Horários do médico agrupados por dia, para leitura rápida. */
  dias(m: PortalMedicoHorarios): { rotulo: string; horarios: PortalHorario[] }[] {
    const grupos = new Map<string, PortalHorario[]>();
    for (const h of m.horarios) {
      const d = new Date(h.dataHora);
      const chave = d.toLocaleDateString('pt-BR', { weekday: 'long', day: '2-digit', month: '2-digit' });
      grupos.set(chave, [...(grupos.get(chave) ?? []), h]);
    }
    return [...grupos.entries()].map(([rotulo, horarios]) => ({ rotulo, horarios }));
  }

  hora(valor: string): string {
    return new Date(valor).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
  }

  async agendar(m: PortalMedicoHorarios, h: PortalHorario) {
    const ok = await this.feedback.confirmar({
      titulo: 'Confirmar agendamento?',
      mensagem: `${h.tipo} com ${m.nome}, ${this.dataLonga(h.dataHora)}.`,
      textoConfirmar: 'Agendar'
    });
    if (!ok) return;
    this.processando = h.idAgenda;
    this.portal.agendar(h.idAgenda).pipe(finalize(() => { this.processando = 0; this.cdr.detectChanges(); })).subscribe({
      next: () => {
        this.feedback.sucesso('Agendamento realizado! Você receberá um lembrete antes do atendimento.');
        this.medicos = null;
        this.aba = 'agenda';
        this.carregarResumo();
      },
      error: (e) => {
        this.feedback.erro(e);
        this.carregarAba('agendar', true);
      }
    });
  }

  // ---------- Exames, documentos, pagamentos ----------

  rotuloExame(status: string): string {
    switch (status) {
      case 'Solicitado': return 'Solicitado';
      case 'Realizado': return 'Aguardando resultado';
      case 'Laudado': return 'Resultado disponível';
      case 'Entregue': return 'Resultado entregue';
      default: return status;
    }
  }

  abrirArquivo(exame: PortalExame, anexo: ExameAnexo) {
    const janela = window.open('', '_blank');
    this.portal.arquivoExame(exame.id, anexo.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        if (janela) janela.location.href = url;
        else {
          const link = document.createElement('a');
          link.href = url;
          link.download = anexo.nomeArquivo;
          link.click();
        }
        setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: (e) => {
        janela?.close();
        this.feedback.erro(e);
      }
    });
  }

  abrirDocumento(d: DocumentoClinico) {
    this.router.navigate(['/portal/documento', d.id]);
  }

  abrirRecibo(c: Cobranca) {
    this.router.navigate(['/portal/recibo', c.id]);
  }
}
