import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '../shared/ionic.module';
import { PortalPageRoutingModule } from './portal-routing.module';
import { PortalEntradaPage } from './portal-entrada.page';
import { PortalAreaPage } from './portal-area.page';

@NgModule({
  imports: [CommonModule, ReactiveFormsModule, IonicModule, PortalPageRoutingModule],
  declarations: [PortalEntradaPage, PortalAreaPage]
})
export class PortalPageModule {}
