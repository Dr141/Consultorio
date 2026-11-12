import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '../shared/ionic.module';
import { DocumentoModalComponent } from './documento-modal.component';

/** Modal de documentos reutilizado por Consultas, Prontuário e Pacientes. */
@NgModule({
  imports: [CommonModule, FormsModule, ReactiveFormsModule, IonicModule],
  declarations: [DocumentoModalComponent],
  exports: [DocumentoModalComponent]
})
export class DocumentoModalModule {}
