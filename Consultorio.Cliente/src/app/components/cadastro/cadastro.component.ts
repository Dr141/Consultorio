import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ModalController } from '@ionic/angular';
import { ConfirmarSenha } from '../../extensoes/confirmar-senha.validator';
import { ConsultorioApiService } from '../../services/api/consultorio-api.service';

@Component({
  selector: 'app-cadastro',
  templateUrl: './cadastro.component.html',
  styleUrls: ['./cadastro.component.scss'],
  standalone: false
})
export class CadastroComponent  implements OnInit {
  public cadastro: FormGroup | any
  public erros: [] | null = null

  constructor(private modalCtrl: ModalController, private api: ConsultorioApiService) { }

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
    return this.modalCtrl.dismiss(null, 'cancel');
  }

  confirm() {
    if (this.cadastro.valid) {
      this.api.cadastro({
        email: this.cadastro.email,
        senha: this.cadastro.senha,
        senhaConfirmacao: this.cadastro.senhaConfirmacao
      }).then(sucesso => {
        return this.modalCtrl.dismiss('confirm');
      }).catch(erro => {
        this.erros = erro ?? erro.erros
      })      
    }

    return
  }
}
