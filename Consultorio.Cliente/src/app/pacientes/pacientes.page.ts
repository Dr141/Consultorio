import { Component, OnInit } from '@angular/core';
import { AlertController, ModalController, ToastController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { Paciente } from '../types/types';
import { PacienteModalComponent } from './paciente-modal.component';

@Component({
  selector: 'app-pacientes',
  templateUrl: './pacientes.page.html',
  styleUrls: ['./pacientes.page.scss'],
  standalone: false
})
export class PacientesPage implements OnInit {
  public pacientes: Paciente[] = [];
  public pacientesFiltrados: Paciente[] = [];
  public termoBusca = '';
  public carregando = false;

  constructor(
    private api: ConsultorioApiService,
    private modalCtrl: ModalController,
    private alertCtrl: AlertController,
    private toastCtrl: ToastController
  ) {}

  ngOnInit() {
    this.carregarPacientes();
  }

  carregarPacientes() {
    this.carregando = true;
    this.api.obterPacientes().subscribe({
      next: (lista) => {
        this.pacientes = lista;
        this.filtrar();
        this.carregando = false;
      },
      error: async (err) => {
        this.carregando = false;
        const toast = await this.toastCtrl.create({
          message: err.message || 'Erro ao carregar pacientes.',
          duration: 3000,
          color: 'danger'
        });
        await toast.present();
      }
    });
  }

  filtrar() {
    const termo = this.termoBusca.toLowerCase().trim();
    if (!termo) {
      this.pacientesFiltrados = [...this.pacientes];
      return;
    }

    this.pacientesFiltrados = this.pacientes.filter(p =>
      p.nome?.toLowerCase().includes(termo) ||
      p.cpf?.includes(termo)
    );
  }

  async adicionar() {
    const modal = await this.modalCtrl.create({
      component: PacienteModalComponent
    });

    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) {
      this.carregarPacientes();
    }
  }

  async editar(paciente: Paciente) {
    const modal = await this.modalCtrl.create({
      component: PacienteModalComponent,
      componentProps: { paciente }
    });

    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) {
      this.carregarPacientes();
    }
  }

  async remover(paciente: Paciente) {
    const alert = await this.alertCtrl.create({
      header: 'Confirmar Exclusão',
      message: `Deseja realmente remover o paciente ${paciente.nome}?`,
      buttons: [
        { text: 'Cancelar', role: 'cancel' },
        {
          text: 'Excluir',
          role: 'destructive',
          handler: () => {
            this.api.removerPaciente(paciente.id).subscribe({
              next: () => {
                this.carregarPacientes();
              },
              error: async (err) => {
                const toast = await this.toastCtrl.create({
                  message: err.message || 'Erro ao excluir paciente.',
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
