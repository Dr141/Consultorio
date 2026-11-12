import { ChangeDetectorRef, Component } from '@angular/core';
import { Router } from '@angular/router';
import { ModalController } from '@ionic/angular';
import { finalize } from 'rxjs';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { RoleService } from '../services/role/role.service';
import { Paciente } from '../types/types';
import { correspondeBusca, hojeInputData, paraInputData } from '../shared/utils/formatadores';
import { PacienteModalComponent } from './paciente-modal.component';
import { ConvenioPacienteModalComponent } from './convenio-paciente-modal.component';
import { AlertasModalComponent } from '../shared/alertas-clinicos/alertas-modal.component';
import { DocumentoModalComponent } from '../documentos/documento-modal.component';

@Component({
  selector: 'app-pacientes',
  templateUrl: './pacientes.page.html',
  styleUrls: ['./pacientes.page.scss'],
  standalone: false
})
export class PacientesPage {
  public pacientes: Paciente[] = [];
  public pacientesFiltrados: Paciente[] = [];
  public termoBusca = '';
  public carregando = false;
  public mostrarInativos = false;
  public podeVerProntuario = false;
  public podeGerenciar = false;

  constructor(
    private api: ConsultorioApiService,
    private modalCtrl: ModalController,
    private feedback: FeedbackService,
    private cdr: ChangeDetectorRef,
    private router: Router,
    private role: RoleService
  ) {}

  // O Ionic dispara ionViewWillEnter também na primeira entrada, então não é preciso carregar no ngOnInit.
  ionViewWillEnter() {
    this.podeVerProntuario = this.role.ehClinico();
    this.podeGerenciar = this.role.ehGestao();
    this.carregarPacientes();
  }

  alternarInativos() {
    this.mostrarInativos = !this.mostrarInativos;
    this.carregarPacientes();
  }

  atualizar(evento: Event) {
    this.carregarPacientes(() => (evento.target as HTMLIonRefresherElement | null)?.complete());
  }

  carregarPacientes(aoConcluir?: () => void) {
    this.carregando = true;
    this.cdr.markForCheck();
    this.api.obterPacientes(this.mostrarInativos)
      .pipe(finalize(() => {
        this.carregando = false;
        aoConcluir?.();
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (lista) => {
          this.pacientes = [...(lista || [])].sort((a, b) => (a.nome ?? '').localeCompare(b.nome ?? '', 'pt-BR'));
          this.filtrar();
        },
        error: (err) => this.feedback.erro(err, 'Erro ao carregar pacientes.')
      });
  }

  filtrar() {
    this.pacientesFiltrados = this.pacientes.filter(p =>
      correspondeBusca(this.termoBusca, p.nome, p.cpf, p.telefone)
    );
  }

  limparBusca() {
    this.termoBusca = '';
    this.filtrar();
  }

  iniciais(nome: string | undefined): string {
    const partes = (nome ?? '').trim().split(/\s+/).filter(Boolean);
    if (partes.length === 0) return '?';
    return ((partes[0][0] ?? '') + (partes.length > 1 ? partes[partes.length - 1][0] : '')).toUpperCase();
  }

  async adicionar() {
    await this.abrirModal();
  }

  async editar(paciente: Paciente) {
    await this.abrirModal(paciente);
  }

  private async abrirModal(paciente?: Paciente) {
    const modal = await this.modalCtrl.create({
      component: PacienteModalComponent,
      componentProps: { paciente }
    });

    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) {
      this.feedback.sucesso(paciente ? 'Dados do paciente atualizados.' : 'Paciente cadastrado com sucesso.');
      this.carregarPacientes();
    }
  }

  async editarConvenio(paciente: Paciente) {
    const modal = await this.modalCtrl.create({
      component: ConvenioPacienteModalComponent,
      componentProps: { paciente }
    });
    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) this.carregarPacientes();
  }

  carteirinhaVencida(p: Paciente): boolean {
    return !!p.idConvenio && !!p.validadeCarteirinha && paraInputData(p.validadeCarteirinha) < hojeInputData();
  }

  async editarAlertas(paciente: Paciente) {
    const modal = await this.modalCtrl.create({
      component: AlertasModalComponent,
      componentProps: { paciente }
    });
    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) this.carregarPacientes();
  }

  async abrirDocumentos(paciente: Paciente) {
    const modal = await this.modalCtrl.create({
      component: DocumentoModalComponent,
      componentProps: { idPaciente: paciente.id, nomePaciente: paciente.nome }
    });
    await modal.present();
  }

  abrirProntuario(paciente: Paciente) {
    this.router.navigate(['/prontuario'], { queryParams: { idPaciente: paciente.id } });
  }

  async remover(paciente: Paciente) {
    const confirmado = await this.feedback.confirmar({
      titulo: 'Inativar paciente?',
      mensagem: `${paciente.nome} deixará de aparecer nas listas e não poderá ser agendado. O cadastro e o prontuário são preservados e podem ser reativados a qualquer momento.`,
      textoConfirmar: 'Inativar',
      destrutivo: true
    });
    if (!confirmado) return;

    this.api.removerPaciente(paciente.id).subscribe({
      next: () => {
        this.feedback.sucesso('Paciente inativado.');
        this.carregarPacientes();
      },
      error: (err) => this.feedback.erro(err, 'Erro ao inativar paciente.')
    });
  }

  reativar(paciente: Paciente) {
    this.api.reativarPaciente(paciente.id).subscribe({
      next: () => {
        this.feedback.sucesso(`${paciente.nome} foi reativado(a).`);
        this.carregarPacientes();
      },
      error: (err) => this.feedback.erro(err, 'Erro ao reativar paciente.')
    });
  }
}
