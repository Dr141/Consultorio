import { Component, OnInit } from '@angular/core';
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
})
export class UsersPage implements OnInit {
  public users : any;

  constructor(private modalController: ModalController, private api: ConsultorioApiService, private navCtrl: NavController, private alertController: AlertController) { }

  async ngOnInit() {
    await this.obterTodos()
  }

  buttonBack() {
    this.navCtrl.back()
  }

  async atualizarSenha(pEmail: string) {
    const modal = await this.modalController.create({
      component: AtualizarSenhaInternoComponent,
      componentProps: {
        api: this.api,
        email: pEmail
      }
    })

    await modal.present()
  }

  obterTodos() {
    this.api.obterTodos<usuario>().subscribe({
      next: (resul) => {
        if (resul) {
          this.users = resul.usuarios
        }
      },
      error: (erro) => {
        this.presentAlert(erro.message)
      }
    })
  }

  async cadastro() {
    const modal = await this.modalController.create({
      component: CadastroComponent,
      componentProps: { api: this.api }
    })

    await modal.present()
    await modal.onDidDismiss()
    await this.obterTodos()
  }

  async presentAlert(erro: string) {
    const alert = await this.alertController.create({
      header: 'Atenção ocorreu erro!',
      message: erro,
      buttons: ['Ok'],
    })

    await alert.present();
  }

  async permissoes(pUser: usuario) {
    const modal = await this.modalController.create({
      component: PemissaoComponent,
      componentProps: { api: this.api, user: pUser }
    })

    await modal.present()
    await modal.onDidDismiss()
    await this.obterTodos()
  }
}
