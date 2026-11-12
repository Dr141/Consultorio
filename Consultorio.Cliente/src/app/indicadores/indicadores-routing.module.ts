import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { IndicadoresPage } from './indicadores.page';
import { PendenciasPage } from './pendencias.page';

const routes: Routes = [
  { path: '', component: IndicadoresPage },
  { path: 'pendencias', component: PendenciasPage }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class IndicadoresPageRoutingModule {}
