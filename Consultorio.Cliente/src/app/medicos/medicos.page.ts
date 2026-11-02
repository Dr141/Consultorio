import { Component, OnInit } from '@angular/core';
import { AlertController, ModalController, ToastController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { Especialidade, Medico } from '../types/types';
import { MedicoModalComponent } from './medico-modal.component';

@Component({
  selector: 'app-medicos',
  templateUrl: './medicos.page.html',
  styleUrls: ['./medicos.page.scss'],
  standalone: false
})
export class MedicosPage implements OnInit {
  public medicos: Medico[] = [];
  public medicosFiltrados: Medico[] = [];
  public termoBusca = '';
  public carregando = false;

  private mapaEspecialidades: Record<number, string> = {
    1: 'Clínico Geral',
    2: 'Cardiologista',
    3: 'Urologista',
    4: 'Ginecologista',
    5: 'Ortopedista',
    6: 'Pediatra',
    7: 'Dermatologista',
    8: 'Neurologista',
    9: 'Oftalmologista'
  };

  constructor(
    private api: ConsultorioApiService,
    private modalCtrl: ModalController,
    private alertCtrl: AlertController,
    private toastCtrl: ToastController
  ) {}

  ngOnInit() {
    this.carregarMedicos();
  }

  carregarMedicos() {
    this.carregando = true;
    this.api.obterMedicos().subscribe({
      next: (lista) => {
        this.medicos = lista;
        this.filtrar();
        this.carregando = false;
      },
      error: async (err) => {
        this.carregando = false;
        const toast = await this.toastCtrl.create({
          message: err.message || 'Erro ao carregar médicos.',
          duration: 3000,
          color: 'danger'
        });
        await toast.present();
      }
    });
  }

  formatarEspecialidades(especialidades: Especialidade[]): string[] {
    if (!especialidades) return [];
    return especialidades.map(e => this.mapaEspecialidades[e] || e.toString());
  }

  filtrar() {
    const termo = this.termoBusca.toLowerCase().trim();
    if (!termo) {
      this.medicosFiltrados = [...this.medicos];
      return;
    }

    this.medicosFiltrados = this.medicos.filter(m =>
      m.nome?.toLowerCase().includes(termo) ||
      m.crm?.toLowerCase().includes(termo) ||
      m.especialidades?.some(e => (this.mapaEspecialidades[e] || '').toLowerCase().includes(termo))
    );
  }

  async adicionar() {
    const modal = await this.modalCtrl.create({
      component: MedicoModalComponent
    });

    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) {
      this.carregarMedicos();
    }
  }

  async editar(medico: Medico) {
    const modal = await this.modalCtrl.create({
      component: MedicoModalComponent,
      componentProps: { medico }
    });

    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) {
      this.carregarMedicos();
    }
  }

  async remover(medico: Medico) {
    const alert = await this.alertCtrl.create({
      header: 'Confirmar Exclusão',
      message: `Deseja realmente remover o médico ${medico.nome}?`,
      buttons: [
        { text: 'Cancelar', role: 'cancel' },
        {
          text: 'Excluir',
          role: 'destructive',
          handler: () => {
            this.api.removerMedico(medico.id).subscribe({
              next: () => {
                this.carregarMedicos();
              },
              error: async (err) => {
                const toast = await this.toastCtrl.create({
                  message: err.message || 'Erro ao excluir médico.',
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
