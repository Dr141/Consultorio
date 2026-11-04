import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AlertController, ModalController, ToastController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { Exame } from '../types/types';
import { ExameModalComponent } from './exame-modal.component';

@Component({
  selector: 'app-exames',
  templateUrl: './exames.page.html',
  styleUrls: ['./exames.page.scss'],
  standalone: false
})
export class ExamesPage implements OnInit {
  public exames: Exame[] = [];
  public examesFiltrados: Exame[] = [];
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
    this.carregarExames();
  }

  ionViewWillEnter() {
    this.carregarExames();
  }

  carregarExames() {
    this.carregando = true;
    this.cdr.markForCheck();
    this.api.obterExames().subscribe({
      next: (lista) => {
        this.exames = lista || [];
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
          message: err.message || 'Erro ao carregar exames.',
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
      this.examesFiltrados = [...this.exames];
      return;
    }

    this.examesFiltrados = this.exames.filter(e =>
      e.descricao?.toLowerCase().includes(termo) ||
      e.nomePaciente?.toLowerCase().includes(termo) ||
      e.resultado?.toLowerCase().includes(termo)
    );
  }

  async adicionar() {
    const modal = await this.modalCtrl.create({
      component: ExameModalComponent
    });

    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) {
      this.carregarExames();
    }
  }

  async editar(exame: Exame) {
    const modal = await this.modalCtrl.create({
      component: ExameModalComponent,
      componentProps: { exame }
    });

    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) {
      this.carregarExames();
    }
  }

  async remover(exame: Exame) {
    const alert = await this.alertCtrl.create({
      header: 'Confirmar Exclusão',
      message: `Deseja realmente remover o exame ${exame.descricao}?`,
      buttons: [
        { text: 'Cancelar', role: 'cancel' },
        {
          text: 'Excluir',
          role: 'destructive',
          handler: () => {
            this.api.removerExame(exame.id).subscribe({
              next: () => this.carregarExames(),
              error: async (err) => {
                const toast = await this.toastCtrl.create({
                  message: err.message || 'Erro ao excluir exame.',
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
