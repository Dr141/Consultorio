import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '../shared/ionic.module';
import { AgendaPageRoutingModule } from './agenda-routing.module';
import { AgendaPage } from './agenda.page';
import { AgendaModalComponent } from './agenda-modal.component';
import { GradeModalComponent } from './grade-modal.component';
import { BloqueiosModalComponent } from './bloqueios-modal.component';

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    IonicModule,
    AgendaPageRoutingModule
  ],
  declarations: [AgendaPage, AgendaModalComponent, GradeModalComponent, BloqueiosModalComponent]
})
export class AgendaPageModule {}
