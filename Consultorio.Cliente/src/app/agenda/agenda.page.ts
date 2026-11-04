import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AlertController, ModalController, ToastController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { Agenda, Paciente } from '../types/types';
import { AgendaModalComponent } from './agenda-modal.component';

@Component({
  selector: 'app-agenda',
  templateUrl: './agenda.page.html',
  styleUrls: ['./agenda.page.scss'],
  standalone: false
})
export class AgendaPage implements OnInit {
  public agendas: Agenda[] = [];
  public agendasFiltradas: Agenda[] = [];
  public pacientes: Paciente[] = [];
  public termoBusca = '';
  public carregando = false;

  constructor(
    private api: ConsultorioApiService,
    private modalCtrl: ModalController,
    private alertCtrl: AlertController,
    private toastCtrl: ToastController,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  ngOnInit() {
    this.carregarAgenda();
    this.carregarPacientes();
  }

  ionViewWillEnter() {
    this.carregarAgenda();
    this.carregarPacientes();
  }

  carregarAgenda() {
    this.carregando = true;
    this.cdr.markForCheck();
    this.api.obterAgendas().subscribe({
      next: (lista) => {
        this.agendas = lista || [];
        this.filtrar();
        this.carregando = false;
        this.cdr.detectChanges();
      },
      error: async (err) => {
        this.carregando = false;
        this.cdr.detectChanges();
        if (err?.status === 401 || err?.message?.includes('401')) {
          this.router.navigate(['/login']);
          return;
        }
        const toast = await this.toastCtrl.create({
          message: err.message || 'Erro ao carregar agenda.',
          duration: 3000,
          color: 'danger'
        });
        await toast.present();
      }
    });
  }

  carregarPacientes() {
    this.api.obterPacientes().subscribe({
      next: (p) => {
        this.pacientes = p || [];
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  formatarData(dataStr: string): string {
    if (!dataStr) return '';
    const date = new Date(dataStr);
    return date.toLocaleDateString('pt-BR');
  }

  filtrar() {
    const termo = this.termoBusca.toLowerCase().trim();
    if (!termo) {
      this.agendasFiltradas = [...this.agendas];
      return;
    }

    this.agendasFiltradas = this.agendas.filter(a =>
      a.nomeMedico?.toLowerCase().includes(termo) ||
      a.nomePaciente?.toLowerCase().includes(termo)
    );
  }

  async adicionar() {
    const modal = await this.modalCtrl.create({
      component: AgendaModalComponent
    });

    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) {
      this.carregarAgenda();
    }
  }

  async editar(agenda: Agenda) {
    const modal = await this.modalCtrl.create({
      component: AgendaModalComponent,
      componentProps: { agenda }
    });

    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) {
      this.carregarAgenda();
    }
  }

  async agendarPaciente(agenda: Agenda) {
    const inputs = this.pacientes.map(p => ({
      name: 'idPaciente',
      type: 'radio' as const,
      label: p.nome,
      value: p.id
    }));

    const alert = await this.alertCtrl.create({
      header: 'Agendar Paciente',
      subHeader: `${this.formatarData(agenda.data)} às ${agenda.hora.substring(0, 5)} - ${agenda.nomeMedico}`,
      inputs,
      buttons: [
        { text: 'Cancelar', role: 'cancel' },
        {
          text: 'Confirmar',
          handler: (idPacienteEscolhido) => {
            if (!idPacienteEscolhido) return;
            this.api.agendarPaciente(agenda.id, idPacienteEscolhido).subscribe({
              next: () => this.carregarAgenda(),
              error: async (err) => {
                const toast = await this.toastCtrl.create({
                  message: err.message || 'Erro ao agendar paciente.',
                  duration: 3000,
                  color: 'danger'
                });
                await toast.present();
              }
            });
          }
        }
      ]
    });

    await alert.present();
  }

  async liberarHorario(agenda: Agenda) {
    const alert = await this.alertCtrl.create({
      header: 'Liberar Horário',
      message: `Deseja desmarcar o agendamento de ${agenda.nomePaciente}? O horário ficará disponível novamente.`,
      buttons: [
        { text: 'Cancelar', role: 'cancel' },
        {
          text: 'Liberar',
          handler: () => {
            this.api.liberarHorarioAgenda(agenda.id).subscribe({
              next: () => this.carregarAgenda(),
              error: async (err) => {
                const toast = await this.toastCtrl.create({
                  message: err.message || 'Erro ao liberar horário.',
                  duration: 3000,
                  color: 'danger'
                });
                await toast.present();
              }
            });
          }
        }
      ]
    });

    await alert.present();
  }

  async remover(agenda: Agenda) {
    const alert = await this.alertCtrl.create({
      header: 'Confirmar Exclusão',
      message: 'Deseja realmente remover este horário da agenda?',
      buttons: [
        { text: 'Cancelar', role: 'cancel' },
        {
          text: 'Excluir',
          role: 'destructive',
          handler: () => {
            this.api.removerAgenda(agenda.id).subscribe({
              next: () => this.carregarAgenda(),
              error: async (err) => {
                const toast = await this.toastCtrl.create({
                  message: err.message || 'Erro ao excluir da agenda.',
                  duration: 3000,
                  color: 'danger'
                });
                await toast.present();
              }
            });
          }
        }
      ]
    });

    await alert.present();
  }
}
