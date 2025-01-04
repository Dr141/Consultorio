import { Component, OnInit } from '@angular/core';
import { AlertController, ModalController, NavController } from '@ionic/angular';
import { ConsultorioApiService } from '../../services/api/consultorio-api.service';
import { AtualizarSenhaInternoComponent } from '../../components/atualizar-senha-interno/atualizar-senha-interno.component';
import { User } from '../../types/types';
import { CadastroComponent } from '../../components/cadastro/cadastro.component';
import { obterMensagemErro } from '../../extensoes/errors.api';
import { PemissaoComponent } from '../../components/pemissao/pemissao.component';

@Component({
  selector: 'app-users',
  templateUrl: './users.page.html',
  styleUrls: ['./users.page.scss'],
})
export class UsersPage implements OnInit {
  public users: User[] = [
    {
      email: "teste@.com",
      claims: [],
      roles: ["admin"],
      emailConfirmado: true
    },
    {
      email: "teste1@.com",
      claims: [],
      roles: ["auxiliar"],
      emailConfirmado: true
    }
  ]

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

  async obterTodos() {
    await this.api.obterTodos()
      .then(async result => {
        if (result.sucesso) {
          console.log(result.usuarios)
          this.users = result.usuarios
        }
        else {
          await this.presentAlert(obterMensagemErro(result.error.errors ?? result.error))
        }
      })
      .catch(async erro => {
        await this.presentAlert(obterMensagemErro(erro.error.errors ?? erro.error))
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

  async permissoes(pUser: User) {
    const modal = await this.modalController.create({
      component: PemissaoComponent,
      componentProps: { api: this.api, user: pUser }
    })

    await modal.present()
    await modal.onDidDismiss()
    await this.obterTodos()
  }
}
