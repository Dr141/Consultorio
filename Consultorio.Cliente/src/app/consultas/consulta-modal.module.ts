import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '../shared/ionic.module';
import { ConsultaModalComponent } from './consulta-modal.component';

/**
 * Modal de consulta em módulo próprio para ser reutilizado pela tela de Consultas
 * e pela fila do dia (Recepção), que abre o atendimento já vinculado ao horário.
 */
@NgModule({
  imports: [CommonModule, FormsModule, ReactiveFormsModule, IonicModule],
  declarations: [ConsultaModalComponent],
  exports: [ConsultaModalComponent]
})
export class ConsultaModalModule {}
