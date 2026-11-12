import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '../shared/ionic.module';
import { ProntuarioPageRoutingModule } from './prontuario-routing.module';
import { ProntuarioPage } from './prontuario.page';
import { DocumentoModalModule } from '../documentos/documento-modal.module';

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    IonicModule,
    ProntuarioPageRoutingModule,
    DocumentoModalModule
  ],
  declarations: [ProntuarioPage]
})
export class ProntuarioPageModule {}
