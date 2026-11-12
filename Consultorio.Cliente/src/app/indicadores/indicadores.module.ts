import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '../shared/ionic.module';
import { IndicadoresPageRoutingModule } from './indicadores-routing.module';
import { IndicadoresPage } from './indicadores.page';
import { PendenciasPage } from './pendencias.page';

@NgModule({
  imports: [CommonModule, IonicModule, IndicadoresPageRoutingModule],
  declarations: [IndicadoresPage, PendenciasPage]
})
export class IndicadoresPageModule {}
