import { Component, OnDestroy, OnInit } from '@angular/core';
import { ModalController, NavController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { AtualizarSenhaComponent } from '../components/atualizar-senha/atualizar-senha.component';
import { RoleService } from '../services/role/role.service';
import { CacheService } from '../services/cache/cache.service';

@Component({
    selector: 'app-home',
    templateUrl: 'home.page.html',
    styleUrls: ['home.page.scss']
})
export class HomePage implements OnInit, OnDestroy {

  constructor(private modalController: ModalController, private api: ConsultorioApiService, private role: RoleService, private navCtrl: NavController, private cache: CacheService) { }
  
  ngOnInit(): void {
    this.ehAdmin()
  }

  ngOnDestroy(): void {
    this.cache.RemoveTodosCookies()
  }

  logOut() {
    this.api.logout().subscribe({
      next: () => {
        this.navCtrl.back()
      },
      error: () => { }
    })
  }

  users() {
    this.navCtrl.navigateForward('users')
  }

  async atualizarSenha() {
    const modal = await this.modalController.create({
      component: AtualizarSenhaComponent,
      componentProps: {
        api: this.api
      }
    })

    await modal.present()
  }

  public ehAdmin() {
    var roles = this.role.getRole()

    if (roles) {
      return roles.search("Administrador") >= 0
    }

    return false
  }

  public paciente() { }

  public agenda() { }

  public consulta() { }

  public exame() { }

  public medico() { }
}
