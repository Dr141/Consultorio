import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { AlertController, ModalController } from '@ionic/angular';
import { ConfirmarSenha } from '../../extensoes/confirmar-senha.validator';
import { ConsultorioApiService } from '../../services/api/consultorio-api.service';

@Component({
  selector: 'app-cadastro',
  templateUrl: './cadastro.component.html',
  styleUrls: ['./cadastro.component.scss']
})
export class CadastroComponent  implements OnInit {
  public cadastro: FormGroup | any
  @Input() api!: ConsultorioApiService

  constructor(private modalCtrl: ModalController, private alertController: AlertController) { }

  ngOnInit(): void {
    this.cadastro = new FormGroup({
      email: new FormControl('', [Validators.required, Validators.email]),
      senha: new FormControl('', [Validators.required, Validators.minLength(6)]),
      confirmarSenha: new FormControl('', [Validators.required])
    }, {
      validators: [ConfirmarSenha('senha', 'confirmarSenha')]
    })
  }

  cancel() {
    return this.modalCtrl.dismiss(false, 'cancel');
  }

  async confirm() {
    if (this.cadastro.valid) {
      await this.api.cadastro({
        Email: this.cadastro.get('email')?.value,
        Senha: this.cadastro.get('senha')?.value,
        SenhaConfirmacao: this.cadastro.get('confirmarSenha')?.value
      }).then(async result => {
        if (result.sucesso) {
          return this.modalCtrl.dismiss('confirm');
        }
        await this.presentAlert(this.obterErro(result.errors))
        return
      }).catch(async erro => {
        await this.presentAlert(this.obterErro(erro.errors))
      })      
    }

    return
  }

  async presentAlert(erro: string) {
    const alert = await this.alertController.create({
      header: 'Erro',
      message: erro,
      buttons: ['Ok'],
    });

    await alert.present();
  }

  private obterErro(errors: { [key: string]: string[] }) : string {
    let errorString = '';
    for (const field in errors) {
      if (errors.hasOwnProperty(field)) {
        errors[field].forEach((message: string) => {
          errorString += `${message}\n`;
        });
      }
    }

    return errorString
  }
}
