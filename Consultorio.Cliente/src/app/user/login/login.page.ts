import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { AlertController, ModalController, NavController } from '@ionic/angular';
import { CadastroComponent } from '../../components/cadastro/cadastro.component';
import { ConsultorioApiService } from '../../services/api/consultorio-api.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.page.html',
  styleUrls: ['./login.page.scss']
})
export class LoginPage implements OnInit {
  public autenticacao: FormGroup | any

  constructor(private modalController: ModalController, private api: ConsultorioApiService, private alertController: AlertController, private navCtrl: NavController) { }

  ngOnInit() {
    this.autenticacao = new FormGroup({
      email: new FormControl('', [Validators.required, Validators.email]),
      senha: new FormControl('', [Validators.required, Validators.minLength(6)])
    })
  }

  async onSubmit() {
    if (this.autenticacao.valid) {
      await this.api.login(this.autenticacao.email, this.autenticacao.senha)
        .then(async result => {
          if (result.sucesso) {
            this.navCtrl.navigateForward('home')
          }
          await this.presentAlert(result.erro ?? result.erros)
        })
        .catch(async erro => {
          await this.presentAlert(erro ?? erro.erros)
        })
    }
  }

  async openModal() {
    const modal = await this.modalController.create({
      component: CadastroComponent,
      componentProps: { api: this.api }
    })

    await modal.present()

    const { data } = await modal.onWillDismiss()
    if (data) {
      await this.presentAlert('Cadastros realizado com sucesso.', 'Cadastro')
    }
  }

  async presentAlert(erro: string, head: string | null = null) {
    const alert = await this.alertController.create({
      header: head ?? 'Ocorreu um erro',
      message: erro,
      buttons: ['Ok'],
    });

    await alert.present();
  }
}
