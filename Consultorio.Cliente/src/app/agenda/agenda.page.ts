import { ChangeDetectorRef, Component } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { finalize } from 'rxjs';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Agenda, BloqueioAgenda, Paciente, StatusAgendamento } from '../types/types';
import { RoleService } from '../services/role/role.service';
import { GradeModalComponent } from './grade-modal.component';
import { BloqueiosModalComponent } from './bloqueios-modal.component';
import { combinarDataHora, correspondeBusca, mesmoDia, paraDataLocal, paraInputData, rotuloDia } from '../shared/utils/formatadores';
import { SelecionarPacienteComponent } from '../shared/selecionar-paciente/selecionar-paciente.component';
import { AgendaModalComponent } from './agenda-modal.component';

type FiltroStatus = 'todos' | 'disponiveis' | 'ocupados';

interface GrupoAgenda {
  chave: string;
  rotulo: string;
  hoje: boolean;
  itens: Agenda[];
  bloqueio?: BloqueioAgenda;
}

@Component({
  selector: 'app-agenda',
  templateUrl: './agenda.page.html',
  styleUrls: ['./agenda.page.scss'],
  standalone: false
})
export class AgendaPage {
  public agendas: Agenda[] = [];
  public grupos: GrupoAgenda[] = [];
  public pacientes: Paciente[] = [];
  public termoBusca = '';
  public carregando = false;
  public filtroStatus: FiltroStatus = 'todos';
  /** Por padrão a agenda mostra de hoje em diante, que é o que a recepção consulta no dia a dia. */
  public incluirAnteriores = false;
  public totais = { todos: 0, disponiveis: 0, ocupados: 0 };
  public bloqueios: BloqueioAgenda[] = [];
  public podeGerenciar = false;

  constructor(
    private api: ConsultorioApiService,
    private modalCtrl: ModalController,
    private feedback: FeedbackService,
    private cdr: ChangeDetectorRef,
    private role: RoleService
  ) {}

  ionViewWillEnter() {
    this.podeGerenciar = this.role.ehGestao();
    this.carregarAgenda();
    this.carregarPacientes();
    this.carregarBloqueios();
  }

  carregarBloqueios() {
    this.api.obterBloqueiosAgenda().subscribe({
      next: (lista) => {
        this.bloqueios = lista ?? [];
        this.filtrar();
        this.cdr.detectChanges();
      },
      error: () => undefined
    });
  }

  rotuloStatus(status?: StatusAgendamento | null): string {
    switch (status) {
      case 'Confirmado': return 'Confirmado';
      case 'Aguardando': return 'Aguardando';
      case 'EmAtendimento': return 'Em atendimento';
      case 'Atendido': return 'Atendido';
      case 'Faltou': return 'Faltou';
      default: return 'Agendado';
    }
  }

  corStatus(status?: StatusAgendamento | null): string {
    switch (status) {
      case 'Confirmado': return 'success';
      case 'Aguardando': return 'warning';
      case 'EmAtendimento': return 'primary';
      case 'Atendido': return 'medium';
      case 'Faltou': return 'danger';
      default: return 'warning';
    }
  }

  async abrirGrade() {
    const modal = await this.modalCtrl.create({ component: GradeModalComponent });
    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) this.carregarAgenda();
  }

  async abrirBloqueios() {
    const modal = await this.modalCtrl.create({ component: BloqueiosModalComponent });
    await modal.present();
    await modal.onDidDismiss();
    this.carregarAgenda();
    this.carregarBloqueios();
  }

  atualizar(evento: Event) {
    this.carregarAgenda(() => (evento.target as HTMLIonRefresherElement | null)?.complete());
  }

  carregarAgenda(aoConcluir?: () => void) {
    this.carregando = true;
    this.cdr.markForCheck();
    this.api.obterAgendas()
      .pipe(finalize(() => {
        this.carregando = false;
        aoConcluir?.();
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (lista) => {
          this.agendas = [...(lista || [])].sort((a, b) => this.instante(a) - this.instante(b));
          this.filtrar();
        },
        error: (err) => this.feedback.erro(err, 'Erro ao carregar agenda.')
      });
  }

  carregarPacientes() {
    this.api.obterPacientes().subscribe({
      next: (p) => {
        this.pacientes = p || [];
        this.cdr.detectChanges();
      },
      error: () => undefined
    });
  }

  private instante(a: Agenda): number {
    return combinarDataHora(a.data, a.hora)?.getTime() ?? 0;
  }

  ehPassado(a: Agenda): boolean {
    return this.instante(a) < Date.now();
  }

  descricaoHorario(a: Agenda): string {
    const dia = paraDataLocal(a.data);
    const rotulo = dia ? rotuloDia(dia) : '';
    return `${rotulo} às ${a.hora?.substring(0, 5) ?? ''}${a.nomeMedico ? ' · ' + a.nomeMedico : ''}`;
  }

  mudarFiltroStatus(valor: FiltroStatus | string | undefined) {
    this.filtroStatus = (valor as FiltroStatus) || 'todos';
    this.filtrar();
  }

  alternarAnteriores() {
    this.incluirAnteriores = !this.incluirAnteriores;
    this.filtrar();
  }

  limparFiltros() {
    this.termoBusca = '';
    this.filtroStatus = 'todos';
    this.incluirAnteriores = true;
    this.filtrar();
  }

  filtrar() {
    const inicioDeHoje = new Date();
    inicioDeHoje.setHours(0, 0, 0, 0);

    const noPeriodoEBusca = this.agendas.filter(a => {
      const dia = paraDataLocal(a.data);
      if (!this.incluirAnteriores && dia && dia < inicioDeHoje) return false;
      return correspondeBusca(this.termoBusca, a.nomeMedico, a.nomePaciente);
    });

    this.totais = {
      todos: noPeriodoEBusca.length,
      disponiveis: noPeriodoEBusca.filter(a => a.disponivel).length,
      ocupados: noPeriodoEBusca.filter(a => !a.disponivel).length
    };

    const visiveis = noPeriodoEBusca.filter(a =>
      this.filtroStatus === 'todos' ||
      (this.filtroStatus === 'disponiveis' ? a.disponivel : !a.disponivel)
    );

    const hoje = new Date();
    const mapa = new Map<string, GrupoAgenda>();
    for (const a of visiveis) {
      const dia = paraDataLocal(a.data);
      const chave = paraInputData(dia) || 'sem-data';
      if (!mapa.has(chave)) {
        mapa.set(chave, {
          chave,
          rotulo: dia ? rotuloDia(dia) : 'Sem data',
          hoje: !!dia && mesmoDia(dia, hoje),
          itens: [],
          bloqueio: this.bloqueios.find(b =>
            paraInputData(b.dataInicio) <= chave && paraInputData(b.dataFim) >= chave && (!b.idMedico || b.idMedico === a.idMedico))
        });
      }
      mapa.get(chave)!.itens.push(a);
    }
    this.grupos = Array.from(mapa.values());
  }

  async adicionar() {
    await this.abrirModal();
  }

  async editar(agenda: Agenda) {
    await this.abrirModal(agenda);
  }

  private async abrirModal(agenda?: Agenda) {
    const modal = await this.modalCtrl.create({
      component: AgendaModalComponent,
      componentProps: { agenda },
      id: 'agenda-modal'
    });

    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) {
      this.feedback.sucesso(agenda ? 'Horário atualizado.' : 'Horário adicionado à agenda.');
      this.carregarAgenda();
    }
  }

  async agendarPaciente(agenda: Agenda) {
    if (this.pacientes.length === 0) {
      this.feedback.aviso('Cadastre um paciente antes de agendar.');
      return;
    }

    const modal = await this.modalCtrl.create({
      component: SelecionarPacienteComponent,
      componentProps: {
        pacientes: this.pacientes,
        titulo: 'Agendar paciente',
        subtitulo: this.descricaoHorario(agenda)
      }
    });

    await modal.present();
    const { data: idPaciente, role } = await modal.onDidDismiss<number>();
    if (role !== 'selecionado' || !idPaciente) return;

    const paciente = this.pacientes.find(p => p.id === idPaciente);
    this.api.agendarPaciente(agenda.id, idPaciente).subscribe({
      next: () => {
        this.feedback.sucesso(`${paciente?.nome ?? 'Paciente'} agendado(a) para ${this.descricaoHorario(agenda)}.`);
        this.carregarAgenda();
      },
      error: (err) => this.feedback.erro(err, 'Erro ao agendar paciente.')
    });
  }

  async liberarHorario(agenda: Agenda) {
    const confirmado = await this.feedback.confirmar({
      titulo: 'Liberar horário?',
      mensagem: `O agendamento de ${agenda.nomePaciente ?? 'paciente'} será desmarcado e o horário ficará disponível novamente.`,
      textoConfirmar: 'Liberar'
    });
    if (!confirmado) return;

    this.api.liberarHorarioAgenda(agenda.id).subscribe({
      next: () => {
        this.feedback.sucesso('Horário liberado.');
        this.carregarAgenda();
      },
      error: (err) => this.feedback.erro(err, 'Erro ao liberar horário.')
    });
  }

  async remover(agenda: Agenda) {
    const confirmado = await this.feedback.confirmar({
      titulo: 'Excluir horário?',
      mensagem: `O horário de ${this.descricaoHorario(agenda)} será removido da agenda.`,
      textoConfirmar: 'Excluir',
      destrutivo: true
    });
    if (!confirmado) return;

    this.api.removerAgenda(agenda.id).subscribe({
      next: () => {
        this.feedback.sucesso('Horário removido da agenda.');
        this.carregarAgenda();
      },
      error: (err) => this.feedback.erro(err, 'Erro ao excluir da agenda.')
    });
  }
}
