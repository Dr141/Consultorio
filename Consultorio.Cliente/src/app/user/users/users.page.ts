import { Component, OnInit } from '@angular/core';
import { ModalController, NavController } from '@ionic/angular';
import { ConsultorioApiService } from '../../services/api/consultorio-api.service';
import { AtualizarSenhaInternoComponent } from '../../components/atualizar-senha-interno/atualizar-senha-interno.component';

@Component({
  selector: 'app-users',
  templateUrl: './users.page.html',
  styleUrls: ['./users.page.scss'],
})
export class UsersPage implements OnInit {

  constructor(private modalController: ModalController, private api: ConsultorioApiService, private navCtrl: NavController) { }

  ngOnInit() {
  }

  buttonBack() {
    this.navCtrl.back()
  }

  async atualizarSenha(pEmail: string) {
    const modal = await this.modalController.create({
      component: AtualizarSenhaInternoComponent,
      componentProps: {
        api: this.api,
        email: pEmail
      }
    })

    await modal.present()
  }
}
