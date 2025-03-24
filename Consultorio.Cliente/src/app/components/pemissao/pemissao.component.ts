import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { AlertController, ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../../services/api/consultorio-api.service';
import { usuario } from '../../types/types';

@Component({
  selector: 'app-pemissao',
  templateUrl: './pemissao.component.html',
  styleUrls: ['./pemissao.component.scss'],
})

export class PemissaoComponent  implements OnInit {
  public permissao: FormGroup | any
  @Input() api!: ConsultorioApiService
  @Input() user!: usuario
  public roles: any

  constructor(private modalCtrl: ModalController, private alertController: AlertController) { }

  ngOnInit() {
    this.permissao = new FormGroup({
      email: new FormControl({ value: this.user.email, disabled: true }),
      role: new FormControl('', [Validators.required])
    })
    this.obterRoles()
  }

  cancel() {
    return this.modalCtrl.dismiss(false, 'cancel');
  }

  confirm() {
    if (this.permissao.valid) {
      this.api.adicionarRole({
        Email: this.permissao.get('email')?.value,
        Roles: [ this.permissao.get('role')?.value ]
      }).subscribe({
        next: () => {
          this.presentAlertSucesso()
        },
        error: (erro) => {
          this.presentAlert(erro.message)
        }
      })
    }
  }

  private obterRoles() {
    this.api.obterRoles().subscribe({
      next: (result) => {
        this.roles = result
      },
      error: (erro) => {
        this.presentAlert(erro.message)
      }
    })
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

  remover(role: string) {
    this.api.removerRole({
      Email: this.permissao.get('email')?.value,
      Roles: [ role ]
    }).subscribe({
      next: () => {
        this.presentAlertSucesso()
      },
      error: (erro) => {
        this.presentAlert(erro.message)
      }
    })
  }
}
