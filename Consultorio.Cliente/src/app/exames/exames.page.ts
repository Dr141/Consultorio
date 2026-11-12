import { ChangeDetectorRef, Component } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { finalize } from 'rxjs';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { RoleService } from '../services/role/role.service';
import { Exame, StatusExame } from '../types/types';
import { correspondeBusca } from '../shared/utils/formatadores';
import { ExameModalComponent } from './exame-modal.component';
import { AnexosModalComponent } from './anexos-modal.component';

type FiltroExame = 'todos' | 'pendentes' | 'laudados' | 'cancelados';

@Component({
  selector: 'app-exames',
  templateUrl: './exames.page.html',
  styleUrls: ['./exames.page.scss'],
  standalone: false
})
export class ExamesPage {
  public exames: Exame[] = [];
  public examesFiltrados: Exame[] = [];
  public termoBusca = '';
  public carregando = false;
  public filtroStatus: FiltroExame = 'todos';
  public totais = { todos: 0, pendentes: 0, laudados: 0, cancelados: 0 };
  public podeLaudar = false;
  public readonly etapas: { status: StatusExame; rotulo: string }[] = [
    { status: 'Solicitado', rotulo: 'Solicitado' },
    { status: 'Realizado', rotulo: 'Realizado' },
    { status: 'Laudado', rotulo: 'Laudo' },
    { status: 'Entregue', rotulo: 'Entregue' }
  ];

  constructor(
    private api: ConsultorioApiService,
    private modalCtrl: ModalController,
    private feedback: FeedbackService,
    private cdr: ChangeDetectorRef,
    private role: RoleService
  ) {}

  ionViewWillEnter() {
    this.podeLaudar = this.role.ehClinico();
    this.carregarExames();
  }

  atualizar(evento: Event) {
    this.carregarExames(() => (evento.target as HTMLIonRefresherElement | null)?.complete());
  }

  carregarExames(aoConcluir?: () => void) {
    this.carregando = true;
    this.cdr.markForCheck();
    this.api.obterExames()
      .pipe(finalize(() => {
        this.carregando = false;
        aoConcluir?.();
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (lista) => {
          const instante = (e: Exame) => new Date(e.dataHora || e.dtCriacao || 0).getTime() || 0;
          this.exames = [...(lista || [])].sort((a, b) => instante(b) - instante(a));
          this.filtrar();
        },
        error: (err) => this.feedback.erro(err, 'Erro ao carregar exames.')
      });
  }

  mudarFiltroStatus(valor: FiltroExame | string | undefined) {
    this.filtroStatus = (valor as FiltroExame) || 'todos';
    this.filtrar();
  }

  limparFiltros() {
    this.termoBusca = '';
    this.filtroStatus = 'todos';
    this.filtrar();
  }

  filtrar() {
    const encontrados = this.exames.filter(e =>
      correspondeBusca(this.termoBusca, e.descricao, e.nomePaciente, e.nomeMedico, e.resultado, e.status)
    );

    const ativos = encontrados.filter(e => !e.cancelado);
    this.totais = {
      todos: ativos.length,
      pendentes: ativos.filter(e => !e.resultado).length,
      laudados: ativos.filter(e => !!e.resultado).length,
      cancelados: encontrados.length - ativos.length
    };

    // "Todos" mostra os exames válidos; os cancelados ficam em uma aba própria.
    switch (this.filtroStatus) {
      case 'pendentes': this.examesFiltrados = ativos.filter(e => !e.resultado); break;
      case 'laudados': this.examesFiltrados = ativos.filter(e => !!e.resultado); break;
      case 'cancelados': this.examesFiltrados = encontrados.filter(e => e.cancelado); break;
      default: this.examesFiltrados = ativos;
    }
  }

  async adicionar() {
    await this.abrirModal();
  }

  async editar(exame: Exame) {
    await this.abrirModal(exame);
  }

  private async abrirModal(exame?: Exame) {
    const modal = await this.modalCtrl.create({
      component: ExameModalComponent,
      componentProps: { exame }
    });

    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) {
      this.feedback.sucesso(exame ? 'Laudo salvo com sucesso.' : 'Exame solicitado com sucesso.');
      this.carregarExames();
    }
  }

  /** Status com fallback para registros antigos (antes do ciclo do exame existir). */
  statusDe(e: Exame): StatusExame {
    return e.status ?? (e.resultado ? 'Laudado' : 'Solicitado');
  }

  etapaConcluida(e: Exame, etapa: StatusExame): boolean {
    const ordem: StatusExame[] = ['Solicitado', 'Realizado', 'Laudado', 'Entregue'];
    return ordem.indexOf(this.statusDe(e)) >= ordem.indexOf(etapa);
  }

  dataEtapa(e: Exame, etapa: StatusExame): string {
    const valor = etapa === 'Solicitado' ? (e.dataHora || e.dtCriacao)
      : etapa === 'Realizado' ? e.dtRealizacao
      : etapa === 'Laudado' ? e.dtLaudo
      : e.dtEntrega;
    if (!valor) return '';
    const d = new Date(valor);
    return isNaN(d.getTime()) ? '' : d.toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  registrarRealizacao(exame: Exame) {
    this.api.registrarRealizacaoExame(exame.id).subscribe({
      next: () => {
        this.feedback.sucesso('Exame marcado como realizado.');
        this.carregarExames();
      },
      error: (err) => this.feedback.erro(err, 'Não foi possível registrar a realização.')
    });
  }

  async registrarEntrega(exame: Exame) {
    const confirmado = await this.feedback.confirmar({
      titulo: 'Registrar entrega?',
      mensagem: `Confirma que o resultado de "${exame.descricao}" foi entregue a ${exame.nomePaciente ?? 'paciente'}?`,
      textoConfirmar: 'Registrar entrega'
    });
    if (!confirmado) return;
    this.api.registrarEntregaExame(exame.id).subscribe({
      next: () => {
        this.feedback.sucesso('Entrega registrada.');
        this.carregarExames();
      },
      error: (err) => this.feedback.erro(err, 'Não foi possível registrar a entrega.')
    });
  }

  async abrirAnexos(exame: Exame) {
    const modal = await this.modalCtrl.create({
      component: AnexosModalComponent,
      componentProps: { exame }
    });
    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) this.carregarExames();
  }

  async cancelar(exame: Exame) {
    const motivo = await this.feedback.solicitarTexto({
      titulo: 'Cancelar exame?',
      mensagem: `"${exame.descricao}" de ${exame.nomePaciente ?? 'paciente'} ficará registrado como cancelado, com o motivo informado.`,
      placeholder: 'Motivo do cancelamento (ex.: paciente desistiu, pedido duplicado)',
      textoConfirmar: 'Cancelar exame',
      destrutivo: true
    });
    if (!motivo) return;

    this.api.cancelarExame(exame.id, motivo).subscribe({
      next: () => {
        this.feedback.sucesso('Exame cancelado.');
        this.carregarExames();
      },
      error: (err) => this.feedback.erro(err, 'Erro ao cancelar exame.')
    });
  }
}
