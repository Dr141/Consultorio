import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '../shared/ionic.module';
import { PacientesPageRoutingModule } from './pacientes-routing.module';
import { PacientesPage } from './pacientes.page';
import { PacienteModalComponent } from './paciente-modal.component';
import { DocumentoModalModule } from '../documentos/documento-modal.module';

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    IonicModule,
    PacientesPageRoutingModule,
    DocumentoModalModule
  ],
  declarations: [PacientesPage, PacienteModalComponent]
})
export class PacientesPageModule {}
