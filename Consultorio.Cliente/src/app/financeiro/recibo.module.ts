import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { IonicModule } from '../shared/ionic.module';
import { ReciboPage } from './recibo.page';

/** Recibo em módulo próprio: usado pelo Financeiro (/financeiro/recibo/:id) e pelo portal (/portal/recibo/:id). */
@NgModule({
  imports: [CommonModule, IonicModule, RouterModule.forChild([{ path: ':id', component: ReciboPage }])],
  declarations: [ReciboPage]
})
export class ReciboPageModule {}
