import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '../shared/ionic.module';
import { ExamesPageRoutingModule } from './exames-routing.module';
import { ExamesPage } from './exames.page';
import { ExameModalComponent } from './exame-modal.component';
import { AnexosModalComponent } from './anexos-modal.component';

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    IonicModule,
    ExamesPageRoutingModule
  ],
  declarations: [ExamesPage, ExameModalComponent, AnexosModalComponent]
})
export class ExamesPageModule {}
