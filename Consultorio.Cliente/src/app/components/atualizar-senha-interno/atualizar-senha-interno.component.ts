import { Component, Input, OnInit } from '@angular/core';
import { ConsultorioApiService } from '../../services/api/consultorio-api.service';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { AlertController, ModalController } from '@ionic/angular';
import { ConfirmarSenha } from '../../extensoes/confirmar-senha.validator';

@Component({
  selector: 'app-atualizar-senha-interno',
  templateUrl: './atualizar-senha-interno.component.html',
  styleUrls: ['./atualizar-senha-interno.component.scss'],
})
export class AtualizarSenhaInternoComponent implements OnInit {
  public cadastro: FormGroup | any
  @Input() api!: ConsultorioApiService
  @Input() email!: string

  constructor(private modalCtrl: ModalController, private alertController: AlertController) { }

  ngOnInit() {
    this.cadastro = new FormGroup({
      email: new FormControl({ value: this.email, disabled: true }),
      senha: new FormControl('', [Validators.required, Validators.minLength(6)]),
      confirmarSenha: new FormControl('', [Validators.required])
    }, {
      validators: [ConfirmarSenha('senha', 'confirmarSenha')]
    })
  }

  cancel() {
    return this.modalCtrl.dismiss(false, 'cancel');
  }

  confirm() {
    if (this.cadastro.valid) {
      (this.api.atualizarSenhaInterno({
        Email: this.cadastro.get('email')?.value,
        Senha: this.cadastro.get('senha')?.value,
        SenhaConfirmacao: this.cadastro.get('confirmarSenha')?.value
      })).subscribe({
        next: () => {
          this.presentAlertSucesso()
        },
        error: (erro) => {
          this.presentAlert(erro.message)
        }
      })
    }
  }

  async presentAlertSucesso() {
    const alert = await this.alertController.create({
      header: 'Atenção!',
      message: 'Senha atualizada com sucesso.',
      buttons: [{
        text: 'Ok',
        handler: () => {
          this.modalCtrl.dismiss('confirm')
        }
      }],
    });

    await alert.present();
  }

  async presentAlert(erro: string) {
    const alert = await this.alertController.create({
      header: 'Atenção ocorreu erro!',
      message: erro,
      buttons: ['Ok'],
    });

    await alert.present();
  }
}
