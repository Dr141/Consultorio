import { ChangeDetectorRef, Component } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { finalize } from 'rxjs';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { RoleService } from '../services/role/role.service';
import { Agenda, Medico, StatusAgendamento } from '../types/types';
import { hojeInputData, paraDataLocal, paraInputData, rotuloDia } from '../shared/utils/formatadores';
import { ConsultaModalComponent } from '../consultas/consulta-modal.component';

type AcaoAgenda = 'confirmar' | 'chegada' | 'iniciar' | 'concluir' | 'falta';

interface ColunaFila {
  chave: string;
  titulo: string;
  icone: string;
  vazio: string;
  itens: Agenda[];
}

/**
 * Fila do dia: acompanha cada paciente desde o agendamento até o fim do atendimento.
 * Atualiza sozinha a cada minuto enquanto a tela está aberta.
 */
@Component({
  selector: 'app-recepcao',
  templateUrl: './recepcao.page.html',
  styleUrls: ['./recepcao.page.scss'],
  standalone: false
})
export class RecepcaoPage {
  public data = hojeInputData();
  public idMedico: number | null = null;
  public medicos: Medico[] = [];
  public agendamentos: Agenda[] = [];
  public colunas: ColunaFila[] = [];
  public carregando = false;
  public processando = new Set<number>();
  public ehClinico = false;
  public agora = Date.now();

  private atualizacaoAutomatica?: ReturnType<typeof setInterval>;
  private relogio?: ReturnType<typeof setInterval>;

  constructor(
    private api: ConsultorioApiService,
    private feedback: FeedbackService,
    private role: RoleService,
    private modalCtrl: ModalController,
    private cdr: ChangeDetectorRef
  ) {}

  ionViewWillEnter() {
    this.ehClinico = this.role.ehClinico();
    this.carregarMedicos();
    this.carregar();
    this.atualizacaoAutomatica = setInterval(() => this.carregar(true), 60_000);
    this.relogio = setInterval(() => { this.agora = Date.now(); this.cdr.detectChanges(); }, 30_000);
  }

  ionViewWillLeave() {
    clearInterval(this.atualizacaoAutomatica);
    clearInterval(this.relogio);
  }

  get rotuloData(): string {
    const d = paraDataLocal(this.data);
    return d ? rotuloDia(d) : '';
  }

  get ehHoje(): boolean {
    return this.data === hojeInputData();
  }

  carregarMedicos() {
    this.api.obterMedicos().subscribe({
      next: (lista) => {
        this.medicos = [...(lista ?? [])].sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR'));
        this.cdr.detectChanges();
      },
      error: () => undefined
    });

    // Médico logado vê a própria fila por padrão.
    if (this.idMedico === null && this.role.possui('Medico')) {
      this.api.obterMeuMedico().subscribe({
        next: (meu) => {
          if (meu && this.idMedico === null) {
            this.idMedico = meu.id;
            this.carregar();
          }
        },
        error: () => undefined
      });
    }
  }

  carregar(silencioso = false, aoConcluir?: () => void) {
    if (!silencioso) this.carregando = true;
    this.api.obterAgendaDoDia(this.data, this.idMedico)
      .pipe(finalize(() => {
        this.carregando = false;
        aoConcluir?.();
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (lista) => {
          this.agendamentos = lista ?? [];
          this.montarColunas();
        },
        error: (err) => { if (!silencioso) this.feedback.erro(err, 'Erro ao carregar a fila do dia.'); }
      });
  }

  atualizar(evento: Event) {
    this.carregar(false, () => (evento.target as HTMLIonRefresherElement | null)?.complete());
  }

  mudarDia(dias: number) {
    const d = paraDataLocal(this.data) ?? new Date();
    d.setDate(d.getDate() + dias);
    this.data = paraInputData(d);
    this.carregar();
  }

  irParaHoje() {
    this.data = hojeInputData();
    this.carregar();
  }

  private montarColunas() {
    const por = (...status: StatusAgendamento[]) => this.agendamentos.filter(a => a.status && status.includes(a.status));
    this.colunas = [
      { chave: 'chegar', titulo: 'A chegar', icone: 'calendar-outline', vazio: 'Ninguém a chegar.', itens: por('Agendado', 'Confirmado') },
      { chave: 'aguardando', titulo: 'Aguardando', icone: 'hourglass-outline', vazio: 'Sala de espera vazia.', itens: por('Aguardando') },
      { chave: 'atendimento', titulo: 'Em atendimento', icone: 'medkit-outline', vazio: 'Nenhum atendimento em curso.', itens: por('EmAtendimento') },
      { chave: 'encerrados', titulo: 'Encerrados', icone: 'checkmark-done-outline', vazio: 'Nenhum atendimento encerrado.', itens: por('Atendido', 'Faltou') }
    ];
  }

  get totalFaltas(): number {
    return this.agendamentos.filter(a => a.status === 'Faltou').length;
  }

  get esperaMedia(): number | null {
    const aguardando = this.agendamentos.filter(a => a.status === 'Aguardando' && a.dtChegada);
    if (aguardando.length === 0) return null;
    const total = aguardando.reduce((soma, a) => soma + this.minutosDesde(a.dtChegada), 0);
    return Math.round(total / aguardando.length);
  }

  minutosDesde(instante?: string | null): number {
    if (!instante) return 0;
    return Math.max(0, Math.round((this.agora - new Date(instante).getTime()) / 60_000));
  }

  rotuloStatus(status?: StatusAgendamento | null): string {
    switch (status) {
      case 'Agendado': return 'Agendado';
      case 'Confirmado': return 'Confirmado';
      case 'Aguardando': return 'Aguardando';
      case 'EmAtendimento': return 'Em atendimento';
      case 'Atendido': return 'Atendido';
      case 'Faltou': return 'Faltou';
      default: return 'Livre';
    }
  }

  corStatus(status?: StatusAgendamento | null): string {
    switch (status) {
      case 'Confirmado': return 'success';
      case 'Aguardando': return 'warning';
      case 'EmAtendimento': return 'primary';
      case 'Atendido': return 'medium';
      case 'Faltou': return 'danger';
      default: return 'tertiary';
    }
  }

  /** Atrasado: passou do horário e ainda não chegou. */
  atrasado(a: Agenda): boolean {
    if (!this.ehHoje || !(a.status === 'Agendado' || a.status === 'Confirmado')) return false;
    const [h, m] = (a.hora ?? '00:00').split(':').map(Number);
    const marcado = new Date();
    marcado.setHours(h || 0, m || 0, 0, 0);
    return this.agora > marcado.getTime() + 10 * 60_000;
  }

  async executar(a: Agenda, acao: AcaoAgenda) {
    if (acao === 'falta') {
      const ok = await this.feedback.confirmar({
        titulo: 'Registrar falta?',
        mensagem: `${a.nomePaciente} não compareceu ao horário das ${a.hora.substring(0, 5)}.`,
        textoConfirmar: 'Registrar falta',
        destrutivo: true
      });
      if (!ok) return;
    }

    this.processando.add(a.id);
    this.api.alterarSituacaoAgenda(a.id, acao)
      .pipe(finalize(() => { this.processando.delete(a.id); this.cdr.detectChanges(); }))
      .subscribe({
        next: (atualizada) => {
          const mensagens: Record<AcaoAgenda, string> = {
            confirmar: 'Presença confirmada.',
            chegada: `${a.nomePaciente} está na sala de espera.`,
            iniciar: 'Atendimento iniciado.',
            concluir: 'Atendimento concluído.',
            falta: 'Falta registrada.'
          };
          this.feedback.sucesso(mensagens[acao]);
          this.substituir(atualizada);
          if (acao === 'iniciar' && this.ehClinico && !atualizada.idConsulta) {
            this.registrarConsulta(atualizada);
          }
        },
        error: (err) => this.feedback.erro(err, 'Não foi possível atualizar o agendamento.')
      });
  }

  async registrarConsulta(a: Agenda) {
    const modal = await this.modalCtrl.create({
      component: ConsultaModalComponent,
      componentProps: {
        idAgenda: a.id,
        idPacienteInicial: a.idPaciente,
        idMedicoInicial: a.idMedico,
        descricaoAtendimento: `${a.nomePaciente} · ${a.hora.substring(0, 5)}${a.nomeMedico ? ' · ' + a.nomeMedico : ''}`
      }
    });
    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) {
      this.feedback.sucesso('Consulta registrada.');
      this.carregar(true);
    }
  }

  enviarLembrete(a: Agenda) {
    this.processando.add(a.id);
    this.api.enviarLembreteAgenda(a.id)
      .pipe(finalize(() => { this.processando.delete(a.id); this.cdr.detectChanges(); }))
      .subscribe({
        next: () => {
          this.feedback.sucesso(`Lembrete enviado para ${a.nomePaciente}.`);
          this.carregar(true);
        },
        error: (err) => this.feedback.erro(err, 'Não foi possível enviar o lembrete.')
      });
  }

  private substituir(atualizada: Agenda) {
    this.agendamentos = this.agendamentos.map(a => a.id === atualizada.id ? atualizada : a);
    this.montarColunas();
  }
}
