import { Component } from '@angular/core';
import { ModalController, NavController } from '@ionic/angular';
import { CacheService } from '../services/cache/cache.service';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { AtualizarSenhaComponent } from '../components/atualizar-senha/atualizar-senha.component';

@Component({
    selector: 'app-home',
    templateUrl: 'home.page.html',
    styleUrls: ['home.page.scss']
})
export class HomePage {
  
  constructor(private modalController: ModalController, private api: ConsultorioApiService, private cache: CacheService, private navCtrl: NavController) {}

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
}
