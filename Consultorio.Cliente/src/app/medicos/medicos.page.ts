import { ChangeDetectorRef, Component } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { finalize } from 'rxjs';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { RoleService } from '../services/role/role.service';
import { Especialidade, Medico } from '../types/types';
import { correspondeBusca } from '../shared/utils/formatadores';
import { NOMES_ESPECIALIDADES } from '../shared/utils/especialidades';
import { MedicoModalComponent } from './medico-modal.component';
import { CertificadoModalComponent } from './certificado-modal.component';

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
  public mostrarInativos = false;
  public podeGerenciar = false;
  /** Administrador gerencia o certificado de qualquer médico; o médico, só o próprio. */
  public ehAdmin = false;
  public idMeuMedico: number | null = null;

  constructor(
    private api: ConsultorioApiService,
    private modalCtrl: ModalController,
    private feedback: FeedbackService,
    private cdr: ChangeDetectorRef,
    private role: RoleService
  ) {}

  ionViewWillEnter() {
    this.podeGerenciar = this.role.ehGestao();
    this.ehAdmin = this.role.ehAdmin();
    if (!this.ehAdmin && this.role.possui('Medico')) {
      this.api.obterMeuMedico().subscribe({
        next: (medico) => {
          this.idMeuMedico = medico?.id ?? null;
          this.cdr.detectChanges();
        },
        error: () => (this.idMeuMedico = null)
      });
    }
    this.carregarMedicos();
  }

  alternarInativos() {
    this.mostrarInativos = !this.mostrarInativos;
    this.carregarMedicos();
  }

  atualizar(evento: Event) {
    this.carregarMedicos(() => (evento.target as HTMLIonRefresherElement | null)?.complete());
  }

  carregarMedicos(aoConcluir?: () => void) {
    this.carregando = true;
    this.cdr.markForCheck();
    this.api.obterMedicos(this.mostrarInativos)
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

  podeGerenciarCertificado(medico: Medico): boolean {
    return medico.ativo !== false && (this.ehAdmin || medico.id === this.idMeuMedico);
  }

  async abrirCertificado(medico: Medico) {
    const modal = await this.modalCtrl.create({
      component: CertificadoModalComponent,
      componentProps: { medico }
    });
    await modal.present();
  }

  async remover(medico: Medico) {
    const confirmado = await this.feedback.confirmar({
      titulo: 'Inativar médico?',
      mensagem: `${medico.nome} deixará de aparecer na agenda e nos cadastros de consulta. Todo o histórico é preservado e o cadastro pode ser reativado.`,
      textoConfirmar: 'Inativar',
      destrutivo: true
    });
    if (!confirmado) return;

    this.api.removerMedico(medico.id).subscribe({
      next: () => {
        this.feedback.sucesso('Médico inativado.');
        this.carregarMedicos();
      },
      error: (err) => this.feedback.erro(err, 'Erro ao inativar médico.')
    });
  }

  reativar(medico: Medico) {
    this.api.reativarMedico(medico.id).subscribe({
      next: () => {
        this.feedback.sucesso(`${medico.nome} foi reativado(a).`);
        this.carregarMedicos();
      },
      error: (err) => this.feedback.erro(err, 'Erro ao reativar médico.')
    });
  }
}
