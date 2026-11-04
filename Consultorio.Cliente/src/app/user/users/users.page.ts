import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AlertController, ModalController, NavController } from '@ionic/angular';
import { ConsultorioApiService } from '../../services/api/consultorio-api.service';
import { AtualizarSenhaInternoComponent } from '../../components/atualizar-senha-interno/atualizar-senha-interno.component';
import { usuario } from '../../types/types';
import { CadastroComponent } from '../../components/cadastro/cadastro.component';
import { PemissaoComponent } from '../../components/pemissao/pemissao.component';

@Component({
  selector: 'app-users',
  templateUrl: './users.page.html',
  styleUrls: ['./users.page.scss'],
  standalone: false
})
export class UsersPage implements OnInit {
  public users: any = [];
  public carregando = false;

  constructor(
    private modalController: ModalController,
    private api: ConsultorioApiService,
    private navCtrl: NavController,
    private alertController: AlertController,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) { }

  ngOnInit() {
    this.obterTodos();
  }

  ionViewWillEnter() {
    this.obterTodos();
  }

  buttonBack() {
    this.navCtrl.back();
  }

  async atualizarSenha(pEmail: string) {
    const modal = await this.modalController.create({
      component: AtualizarSenhaInternoComponent,
      componentProps: {
        api: this.api,
        email: pEmail
      }
    });

    await modal.present();
  }

  obterTodos() {
    this.carregando = true;
    this.cdr.markForCheck();
    this.api.obterTodos<any>().subscribe({
      next: (resul) => {
        if (resul) {
          this.users = resul.usuarios || [];
        }
        this.carregando = false;
        this.cdr.detectChanges();
      },
      error: (erro) => {
        this.carregando = false;
        this.cdr.detectChanges();
        if (erro?.status === 401 || erro?.message?.includes('401')) {
          this.router.navigate(['/login']);
          return;
        }
        this.presentAlert(erro.message);
      }
    });
  }

  async cadastro() {
    const modal = await this.modalController.create({
      component: CadastroComponent,
      componentProps: { api: this.api }
    });

    await modal.present();
    await modal.onDidDismiss();
    await this.obterTodos();
  }

  async presentAlert(erro: string) {
    const alert = await this.alertController.create({
      header: 'Atenção ocorreu erro!',
      message: erro,
      buttons: ['Ok'],
    });

    await alert.present();
  }

  async permissoes(pUser: usuario) {
    const modal = await this.modalController.create({
      component: PemissaoComponent,
      componentProps: { api: this.api, user: pUser }
    });

    await modal.present();
    await modal.onDidDismiss();
    await this.obterTodos();
  }
}
