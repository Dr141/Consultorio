import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ModalController } from '@ionic/angular';
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
  public erro: string = ''
  constructor(private modalCtrl: ModalController) { }

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
      }).subscribe({
        next: () => {
          this.modalCtrl.dismiss('confirm')
        },
        error: error => {
          this.erro = error.message
        }
      })    
    }

    return
  }  
}
