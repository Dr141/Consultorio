import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { AlertController, ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../../services/api/consultorio-api.service';
import { User } from '../../types/types';
import { obterMensagemErro } from '../../extensoes/errors.api';
interface Food {
  id: number;
  name: string;
  type: string;
}

@Component({
  selector: 'app-pemissao',
  templateUrl: './pemissao.component.html',
  styleUrls: ['./pemissao.component.scss'],
})

export class PemissaoComponent  implements OnInit {
  public permissao: FormGroup | any
  @Input() api!: ConsultorioApiService
  @Input() user!: User
  foods: Food[] = [
    {
      id: 1,
      name: 'Apples',
      type: 'fruit',
    },
    {
      id: 2,
      name: 'Carrots',
      type: 'vegetable',
    },
    {
      id: 3,
      name: 'Cupcakes',
      type: 'dessert',
    },
  ];

  constructor(private modalCtrl: ModalController, private alertController: AlertController) { }

  ngOnInit() {
    this.permissao = new FormGroup({
      email: new FormControl({ value: this.user.email, disabled: true }),
      role: new FormControl('', [Validators.required])
    })
  }

  cancel() {
    return this.modalCtrl.dismiss(false, 'cancel');
  }

  async confirm() {
    if (this.permissao.valid) {
      await this.api.cadastro({
        Email: this.permissao.get('email')?.value,
        Senha: this.permissao.get('role')?.value
      })
      //  .then(async result => {
      //  if (result.sucesso) {
      //    this.presentAlertSucesso();
      //    return
      //  }

      //  await this.presentAlert(obterMensagemErro(result.error.errors ?? result.error))
      //}).catch(async erro => {
      //  await this.presentAlert(obterMensagemErro(erro.error.errors ?? erro.error))
      //})
    }

    return
  }

  async presentAlert(erro: string) {
    const alert = await this.alertController.create({
      header: 'Atenção ocorreu erro!',
      message: erro,
      buttons: ['Ok'],
    });

    await alert.present();
  }

  async presentAlertSucesso() {
    const alert = await this.alertController.create({
      header: 'Atenção!',
      message: 'Role configurada com sucesso.',
      buttons: [{
        text: 'Ok',
        handler: () => {
          this.modalCtrl.dismiss('confirm')
        }
      }],
    });

    await alert.present();
  }
}
