import { ChangeDetectorRef, Component } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { finalize } from 'rxjs';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Exame } from '../types/types';
import { correspondeBusca } from '../shared/utils/formatadores';
import { ExameModalComponent } from './exame-modal.component';

type FiltroExame = 'todos' | 'pendentes' | 'laudados';

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
  public totais = { todos: 0, pendentes: 0, laudados: 0 };

  constructor(
    private api: ConsultorioApiService,
    private modalCtrl: ModalController,
    private feedback: FeedbackService,
    private cdr: ChangeDetectorRef
  ) {}

  ionViewWillEnter() {
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
      correspondeBusca(this.termoBusca, e.descricao, e.nomePaciente, e.nomeMedico, e.resultado)
    );

    this.totais = {
      todos: encontrados.length,
      pendentes: encontrados.filter(e => !e.resultado).length,
      laudados: encontrados.filter(e => !!e.resultado).length
    };

    this.examesFiltrados = encontrados.filter(e =>
      this.filtroStatus === 'todos' || (this.filtroStatus === 'pendentes' ? !e.resultado : !!e.resultado)
    );
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

  async remover(exame: Exame) {
    const confirmado = await this.feedback.confirmar({
      titulo: 'Excluir exame?',
      mensagem: `O exame "${exame.descricao}" de ${exame.nomePaciente ?? 'paciente'} será removido${exame.resultado ? ', incluindo o laudo' : ''}. Esta ação não pode ser desfeita.`,
      textoConfirmar: 'Excluir',
      destrutivo: true
    });
    if (!confirmado) return;

    this.api.removerExame(exame.id).subscribe({
      next: () => {
        this.feedback.sucesso('Exame excluído.');
        this.carregarExames();
      },
      error: (err) => this.feedback.erro(err, 'Erro ao excluir exame.')
    });
  }
}
