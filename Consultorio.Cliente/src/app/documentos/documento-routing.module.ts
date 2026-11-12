import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DocumentoPage } from './documento.page';

const routes: Routes = [
  {
    path: ':id',
    component: DocumentoPage
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class DocumentoPageRoutingModule {}
