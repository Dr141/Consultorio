import { Component, OnInit } from '@angular/core';
import { ModalController, NavController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { AtualizarSenhaComponent } from '../components/atualizar-senha/atualizar-senha.component';
import { RoleService } from '../services/role/role.service';
import { CacheService } from '../services/cache/cache.service';
import { ThemeService } from '../services/theme/theme.service';

@Component({
  selector: 'app-home',
  templateUrl: 'home.page.html',
  styleUrls: ['home.page.scss'],
  standalone: false
})
export class HomePage implements OnInit {

  constructor(
    private modalController: ModalController,
    private api: ConsultorioApiService,
    private role: RoleService,
    private navCtrl: NavController,
    private cache: CacheService,
    public themeService: ThemeService
  ) { }

  ngOnInit(): void {
    this.ehAdmin();
  }

  toggleTheme() {
    this.themeService.toggleTheme();
  }

  logOut() {
    this.cache.RemoveTodosCookies();
    this.api.logout().subscribe({
      next: () => {
        this.navCtrl.navigateRoot('/login');
      },
      error: () => {
        this.navCtrl.navigateRoot('/login');
      }
    });
  }

  users() {
    (document.activeElement as HTMLElement)?.blur();
    this.navCtrl.navigateForward('/users');
  }

  async atualizarSenha() {
    (document.activeElement as HTMLElement)?.blur();
    const modal = await this.modalController.create({
      component: AtualizarSenhaComponent,
      componentProps: {
        api: this.api
      }
    });

    await modal.present();
  }

  public ehAdmin(): boolean {
    const roles = this.role.getRole();
    if (roles) {
      return roles.search('Administrador') >= 0;
    }
    return false;
  }

  public paciente() {
    (document.activeElement as HTMLElement)?.blur();
    this.navCtrl.navigateForward('/pacientes');
  }

  public agenda() {
    (document.activeElement as HTMLElement)?.blur();
    this.navCtrl.navigateForward('/agenda');
  }

  public consulta() {
    (document.activeElement as HTMLElement)?.blur();
    this.navCtrl.navigateForward('/consultas');
  }

  public exame() {
    (document.activeElement as HTMLElement)?.blur();
    this.navCtrl.navigateForward('/exames');
  }

  public medico() {
    (document.activeElement as HTMLElement)?.blur();
    this.navCtrl.navigateForward('/medicos');
  }
}
