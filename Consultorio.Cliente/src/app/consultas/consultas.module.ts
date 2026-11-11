import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '../shared/ionic.module';
import { ConsultasPageRoutingModule } from './consultas-routing.module';
import { ConsultasPage } from './consultas.page';
import { ConsultaModalModule } from './consulta-modal.module';

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    IonicModule,
    ConsultasPageRoutingModule,
    ConsultaModalModule
  ],
  declarations: [ConsultasPage]
})
export class ConsultasPageModule {}
