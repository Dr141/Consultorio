import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonicModule } from '../shared/ionic.module';
import { ConsultaModalModule } from '../consultas/consulta-modal.module';
import { RecepcaoPageRoutingModule } from './recepcao-routing.module';
import { RecepcaoPage } from './recepcao.page';

@NgModule({
  imports: [CommonModule, FormsModule, IonicModule, ConsultaModalModule, RecepcaoPageRoutingModule],
  declarations: [RecepcaoPage]
})
export class RecepcaoPageModule {}
