import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { AlertController, ModalController, NavController } from '@ionic/angular';
import { CadastroComponent } from '../../components/cadastro/cadastro.component';
import { ConsultorioApiService } from '../../services/api/consultorio-api.service';
import { obterMensagemErro } from '../../extensoes/errors.api';

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
      await this.api.login(this.autenticacao.get('email')?.value, this.autenticacao.get('senha')?.value)
        .then(async result => {
          if (result.sucesso) {
            this.api.salvarAutenticacao(result)
            this.navCtrl.navigateForward('home')
          }
          await this.presentAlert(obterMensagemErro(result.error.errors ?? result.error))
        })
        .catch(async erro => {
          await this.presentAlert(obterMensagemErro(erro.error.errors ?? erro.error))
        })
    }
  }

  async openModal() {
    const modal = await this.modalController.create({
      component: CadastroComponent,
      componentProps: { api: this.api }
    })

    await modal.present()
  }

  async presentAlert(erro: string) {
    const alert = await this.alertController.create({
      header: 'Atenção ocorreu erro!',
      message: erro,
      buttons: ['Ok'],
    })

    await alert.present();
  }
}
