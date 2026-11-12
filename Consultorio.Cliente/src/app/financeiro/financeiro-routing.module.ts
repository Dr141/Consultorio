import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { FinanceiroPage } from './financeiro.page';
import { ReciboPage } from './recibo.page';

const routes: Routes = [
  { path: '', component: FinanceiroPage },
  { path: 'recibo/:id', component: ReciboPage }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class FinanceiroPageRoutingModule {}
