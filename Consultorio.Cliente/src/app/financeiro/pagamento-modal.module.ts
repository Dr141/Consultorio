import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '../shared/ionic.module';
import { PagamentoModalComponent } from './pagamento-modal.component';

/** Modal de recebimento reutilizado pelo Financeiro e pela fila do dia (Recepção). */
@NgModule({
  imports: [CommonModule, IonicModule],
  declarations: [PagamentoModalComponent],
  exports: [PagamentoModalComponent]
})
export class PagamentoModalModule {}
