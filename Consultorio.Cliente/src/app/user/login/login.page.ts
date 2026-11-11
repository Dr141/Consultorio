import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
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
  public entrando = false;

  constructor(
    private modalController: ModalController,
    private api: ConsultorioApiService,
    private navCtrl: NavController,
    public themeService: ThemeService,
    private cdr: ChangeDetectorRef
  ) { }

  ionViewWillEnter() {
    this.erro = '';
    this.entrando = false;
    this.cdr.markForCheck();
  }

  ngOnInit() {
    this.autenticacao = new FormGroup({
      email: new FormControl('', [Validators.required, Validators.email]),
      senha: new FormControl('', [Validators.required, Validators.minLength(6)])
    });
  }

  onSubmit() {
    if (this.autenticacao.valid && !this.entrando) {
      this.erro = '';
      this.entrando = true;
      const email = (this.autenticacao.get('email')?.value ?? '').trim();
      this.api.login(email, this.autenticacao.get('senha')?.value).subscribe({
        next: () => {
          this.entrando = false;
          this.navCtrl.navigateRoot('home');
        },
        error: (error) => {
          this.entrando = false;
          this.erro = error?.message || 'E-mail ou senha inválidos.';
          this.cdr.detectChanges();
        }
      });
    }
  }

  limparErro() {
    if (this.erro) {
      this.erro = '';
      this.cdr.markForCheck();
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
