import { NgModule } from '@angular/core';
import { PreloadAllModules, RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  {
    path: 'home',
    loadChildren: () => import('./home/home.module').then(m => m.HomePageModule)
  },
  {
    path: 'login',
    loadChildren: () => import('./user/login/login.module').then(m => m.LoginPageModule)
  },
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: 'users',
    loadChildren: () => import('./user/users/users.module').then(m => m.UsersPageModule)
  },
  {
    path: 'pacientes',
    loadChildren: () => import('./pacientes/pacientes.module').then(m => m.PacientesPageModule)
  },
  {
    path: 'medicos',
    loadChildren: () => import('./medicos/medicos.module').then(m => m.MedicosPageModule)
  },
  {
    path: 'agenda',
    loadChildren: () => import('./agenda/agenda.module').then(m => m.AgendaPageModule)
  },
  {
    path: 'consultas',
    loadChildren: () => import('./consultas/consultas.module').then(m => m.ConsultasPageModule)
  },
  {
    path: 'exames',
    loadChildren: () => import('./exames/exames.module').then(m => m.ExamesPageModule)
  },
  {
    path: 'recepcao',
    loadChildren: () => import('./recepcao/recepcao.module').then(m => m.RecepcaoPageModule)
  },
  {
    path: 'auditoria',
    loadChildren: () => import('./auditoria/auditoria.module').then(m => m.AuditoriaPageModule)
  },
  {
    path: 'prontuario',
    loadChildren: () => import('./prontuario/prontuario.module').then(m => m.ProntuarioPageModule)
  },
  {
    path: 'documento',
    loadChildren: () => import('./documentos/documento.module').then(m => m.DocumentoPageModule)
  }
];

@NgModule({
  imports: [
    RouterModule.forRoot(routes, { preloadingStrategy: PreloadAllModules })
  ],
  exports: [RouterModule]
})
export class AppRoutingModule { }
