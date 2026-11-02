import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ModalController, NavController } from '@ionic/angular';
import { CadastroComponent } from '../../components/cadastro/cadastro.component';
import { ConsultorioApiService } from '../../services/api/consultorio-api.service';
import { ThemeService } from '../../services/theme/theme.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.page.html',
  styleUrls: ['./login.page.scss'],
  standalone: false
})
export class LoginPage implements OnInit {
  public autenticacao: FormGroup | any;
  public erro: string = '';

  constructor(
    private modalController: ModalController,
    private api: ConsultorioApiService,
    private navCtrl: NavController,
    public themeService: ThemeService
  ) { }

  ionViewWillEnter() {
    this.erro = '';
  }

  ngOnInit() {
    this.autenticacao = new FormGroup({
      email: new FormControl('', [Validators.required, Validators.email]),
      senha: new FormControl('', [Validators.required, Validators.minLength(6)])
    });
  }

  onSubmit() {
    if (this.autenticacao.valid) {
      this.api.login(this.autenticacao.get('email')?.value, this.autenticacao.get('senha')?.value).subscribe({
        next: () => {
          this.navCtrl.navigateForward('home');
        },
        error: (error) => {
          this.erro = error.message;
        }
      });
    }
  }

  toggleTheme() {
    this.themeService.toggleTheme();
  }

  async openModal() {
    const modal = await this.modalController.create({
      component: CadastroComponent,
      componentProps: { api: this.api }
    });

    await modal.present();
  }
}
