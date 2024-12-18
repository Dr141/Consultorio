import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ModalController } from '@ionic/angular';
import { CadastroComponent } from '../../components/cadastro/cadastro.component';

@Component({
  selector: 'app-login',
  templateUrl: './login.page.html',
  styleUrls: ['./login.page.scss'],
  standalone: false
})
export class LoginPage implements OnInit {
  public autenticacao: FormGroup | any

  constructor(private modalController: ModalController) { }

  ngOnInit() {
    this.autenticacao = new FormGroup({
      email: new FormControl('', [Validators.required, Validators.email]),
      senha: new FormControl('', [Validators.required, Validators.minLength(6)])
    })
  }

  onSubmit() {
    if (this.autenticacao.valid) {
      const loginData = this.autenticacao.value; console.log('Login data:', loginData); // Adicione a lógica para autenticar o usuário aqui
    }
  }

  async openModal() {
    const modal = await this.modalController.create({
      component: CadastroComponent
    })

    await modal.present()   
  }
}
