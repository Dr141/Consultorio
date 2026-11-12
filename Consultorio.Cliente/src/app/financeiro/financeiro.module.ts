import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '../shared/ionic.module';
import { FinanceiroPageRoutingModule } from './financeiro-routing.module';
import { FinanceiroPage } from './financeiro.page';
import { ReciboPage } from './recibo.page';
import { CobrancaModalComponent } from './cobranca-modal.component';
import { ConvenioModalComponent } from './convenio-modal.component';
import { PagamentoModalModule } from './pagamento-modal.module';

@NgModule({
  imports: [CommonModule, IonicModule, FinanceiroPageRoutingModule, PagamentoModalModule],
  declarations: [FinanceiroPage, ReciboPage, CobrancaModalComponent, ConvenioModalComponent]
})
export class FinanceiroPageModule {}
