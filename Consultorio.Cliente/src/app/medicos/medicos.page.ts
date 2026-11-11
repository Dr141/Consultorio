import { ChangeDetectorRef, Component } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { finalize } from 'rxjs';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Especialidade, Medico } from '../types/types';
import { correspondeBusca } from '../shared/utils/formatadores';
import { NOMES_ESPECIALIDADES } from '../shared/utils/especialidades';
import { MedicoModalComponent } from './medico-modal.component';

@Component({
  selector: 'app-medicos',
  templateUrl: './medicos.page.html',
  styleUrls: ['./medicos.page.scss'],
  standalone: false
})
export class MedicosPage {
  public medicos: Medico[] = [];
  public medicosFiltrados: Medico[] = [];
  public termoBusca = '';
  public carregando = false;

  constructor(
    private api: ConsultorioApiService,
    private modalCtrl: ModalController,
    private feedback: FeedbackService,
    private cdr: ChangeDetectorRef
  ) {}

  ionViewWillEnter() {
    this.carregarMedicos();
  }

  atualizar(evento: Event) {
    this.carregarMedicos(() => (evento.target as HTMLIonRefresherElement | null)?.complete());
  }

  carregarMedicos(aoConcluir?: () => void) {
    this.carregando = true;
    this.cdr.markForCheck();
    this.api.obterMedicos()
      .pipe(finalize(() => {
        this.carregando = false;
        aoConcluir?.();
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (lista) => {
          this.medicos = [...(lista || [])].sort((a, b) => (a.nome ?? '').localeCompare(b.nome ?? '', 'pt-BR'));
          this.filtrar();
        },
        error: (err) => this.feedback.erro(err, 'Erro ao carregar médicos.')
      });
  }

  formatarEspecialidades(especialidades: Especialidade[]): string[] {
    if (!especialidades) return [];
    return especialidades.map(e => NOMES_ESPECIALIDADES[e] || e.toString());
  }

  filtrar() {
    this.medicosFiltrados = this.medicos.filter(m =>
      correspondeBusca(this.termoBusca, m.nome, m.crm, ...this.formatarEspecialidades(m.especialidades))
    );
  }

  limparBusca() {
    this.termoBusca = '';
    this.filtrar();
  }

  async adicionar() {
    await this.abrirModal();
  }

  async editar(medico: Medico) {
    await this.abrirModal(medico);
  }

  private async abrirModal(medico?: Medico) {
    const modal = await this.modalCtrl.create({
      component: MedicoModalComponent,
      componentProps: { medico }
    });

    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) {
      this.feedback.sucesso(medico ? 'Dados do médico atualizados.' : 'Médico cadastrado com sucesso.');
      this.carregarMedicos();
    }
  }

  async remover(medico: Medico) {
    const confirmado = await this.feedback.confirmar({
      titulo: 'Excluir médico?',
      mensagem: `O cadastro de ${medico.nome} será removido. Esta ação não pode ser desfeita.`,
      textoConfirmar: 'Excluir',
      destrutivo: true
    });
    if (!confirmado) return;

    this.api.removerMedico(medico.id).subscribe({
      next: () => {
        this.feedback.sucesso('Médico excluído.');
        this.carregarMedicos();
      },
      error: (err) => this.feedback.erro(err, 'Erro ao excluir médico.')
    });
  }
}
