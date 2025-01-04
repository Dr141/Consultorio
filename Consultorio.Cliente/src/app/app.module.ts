import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { RouteReuseStrategy, RouterModule } from '@angular/router';
import { IonicModule, IonicRouteStrategy } from '@ionic/angular';
import { ReactiveFormsModule } from '@angular/forms';
import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import { CadastroComponent } from 'src/app/components/cadastro/cadastro.component';
import { provideHttpClient } from '@angular/common/http';
import { AtualizarSenhaComponent } from './components/atualizar-senha/atualizar-senha.component';
import { AtualizarSenhaInternoComponent } from './components/atualizar-senha-interno/atualizar-senha-interno.component';
import { PemissaoComponent } from './components/pemissao/pemissao.component';

@NgModule({
  declarations: [AppComponent, CadastroComponent, AtualizarSenhaComponent, AtualizarSenhaInternoComponent, PemissaoComponent],
  imports: [BrowserModule, RouterModule.forRoot([]), IonicModule.forRoot({}), AppRoutingModule, ReactiveFormsModule],
  providers: [{ provide: RouteReuseStrategy, useClass: IonicRouteStrategy }, provideHttpClient()],
  bootstrap: [AppComponent],
})
export class AppModule {}
