import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { FinanceiroPage } from './financeiro.page';

const routes: Routes = [
  { path: '', component: FinanceiroPage },
  { path: 'recibo', loadChildren: () => import('./recibo.module').then(m => m.ReciboPageModule) }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class FinanceiroPageRoutingModule {}
