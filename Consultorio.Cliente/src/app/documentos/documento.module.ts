import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IonicModule } from '../shared/ionic.module';
import { DocumentoPageRoutingModule } from './documento-routing.module';
import { DocumentoPage } from './documento.page';

@NgModule({
  imports: [CommonModule, IonicModule, DocumentoPageRoutingModule],
  declarations: [DocumentoPage]
})
export class DocumentoPageModule {}
