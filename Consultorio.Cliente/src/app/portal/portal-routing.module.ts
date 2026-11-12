import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PortalEntradaPage } from './portal-entrada.page';
import { PortalAreaPage } from './portal-area.page';

const routes: Routes = [
  { path: '', component: PortalEntradaPage },
  { path: 'inicio', component: PortalAreaPage },
  // Mesmas telas de impressão da equipe, em modo portal (dados vindos da sessão do paciente).
  { path: 'documento', loadChildren: () => import('../documentos/documento.module').then(m => m.DocumentoPageModule) },
  { path: 'recibo', loadChildren: () => import('../financeiro/recibo.module').then(m => m.ReciboPageModule) }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PortalPageRoutingModule {}
