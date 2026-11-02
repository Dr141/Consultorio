import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '../shared/ionic.module';
import { MedicosPageRoutingModule } from './medicos-routing.module';
import { MedicosPage } from './medicos.page';
import { MedicoModalComponent } from './medico-modal.component';

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    IonicModule,
    MedicosPageRoutingModule
  ],
  declarations: [MedicosPage, MedicoModalComponent]
})
export class MedicosPageModule {}
