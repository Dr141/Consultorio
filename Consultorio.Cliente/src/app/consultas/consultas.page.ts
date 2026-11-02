import { Component, OnInit } from '@angular/core';
import { AlertController, ModalController, ToastController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { Consulta } from '../types/types';
import { ConsultaModalComponent } from './consulta-modal.component';

@Component({
  selector: 'app-consultas',
  templateUrl: './consultas.page.html',
  styleUrls: ['./consultas.page.scss'],
  standalone: false
})
export class ConsultasPage implements OnInit {
  public consultas: Consulta[] = [];
  public consultasFiltradas: Consulta[] = [];
  public termoBusca = '';
  public carregando = false;

  constructor(
    private api: ConsultorioApiService,
    private modalCtrl: ModalController,
    private alertCtrl: AlertController,
    private toastCtrl: ToastController
  ) {}

  ngOnInit() {
    this.carregarConsultas();
  }

  carregarConsultas() {
    this.carregando = true;
    this.api.obterConsultas().subscribe({
      next: (lista) => {
        this.consultas = lista;
        this.filtrar();
        this.carregando = false;
      },
      error: async (err) => {
        this.carregando = false;
        const toast = await this.toastCtrl.create({
          message: err.message || 'Erro ao carregar consultas.',
          duration: 3000,
          color: 'danger'
        });
        await toast.present();
      }
    });
  }

  formatarData(dataStr?: string): string {
    if (!dataStr) return '';
    const date = new Date(dataStr);
    return date.toLocaleDateString('pt-BR');
  }

  filtrar() {
    const termo = this.termoBusca.toLowerCase().trim();
    if (!termo) {
      this.consultasFiltradas = [...this.consultas];
      return;
    }

    this.consultasFiltradas = this.consultas.filter(c =>
      c.nomePaciente?.toLowerCase().includes(termo) ||
      c.nomeMedico?.toLowerCase().includes(termo) ||
      c.observacao?.toLowerCase().includes(termo)
    );
  }

  async adicionar() {
    const modal = await this.modalCtrl.create({
      component: ConsultaModalComponent
    });

    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) {
      this.carregarConsultas();
    }
  }

  async editar(consulta: Consulta) {
    const modal = await this.modalCtrl.create({
      component: ConsultaModalComponent,
      componentProps: { consulta }
    });

    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) {
      this.carregarConsultas();
    }
  }

  async remover(consulta: Consulta) {
    const alert = await this.alertCtrl.create({
      header: 'Confirmar Exclusão',
      message: 'Deseja realmente remover o registro desta consulta?',
      buttons: [
        { text: 'Cancelar', role: 'cancel' },
        {
          text: 'Excluir',
          role: 'destructive',
          handler: () => {
            this.api.removerConsulta(consulta.id).subscribe({
              next: () => this.carregarConsultas(),
              error: async (err) => {
                const toast = await this.toastCtrl.create({
                  message: err.message || 'Erro ao excluir consulta.',
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
