import { Component, OnInit } from '@angular/core';
import { ModalController, NavController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { AtualizarSenhaComponent } from '../components/atualizar-senha/atualizar-senha.component';
import { RoleService } from '../services/role/role.service';

@Component({
    selector: 'app-home',
    templateUrl: 'home.page.html',
    styleUrls: ['home.page.scss']
})
export class HomePage implements OnInit {
  public admin: boolean = false

  constructor(private modalController: ModalController, private api: ConsultorioApiService, private role: RoleService, private navCtrl: NavController) { }
  ngOnInit(): void {
    this.ehAdmin()
  }

  logOut() {
    this.api.logout().subscribe({
      next: () => {
        this.navCtrl.navigateForward('login')
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

  private ehAdmin() {
    var roles = this.role.getRole()
    this.admin = false

    if (typeof (roles) == 'string') {
      this.admin = roles == 'Administrador'
      return
    }

    if (typeof (roles) == typeof ([])) {
      roles.forEach((element: string) =>
      {
        if (element == 'Administrador') {
          this.admin = true
          return
        }
      });
    }

    
  }
}
