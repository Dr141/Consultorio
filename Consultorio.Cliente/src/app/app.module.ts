import { NgModule, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';
import { RouteReuseStrategy, RouterModule } from '@angular/router';
import { IonicRouteStrategy, provideIonicAngular } from '@ionic/angular';
import { ReactiveFormsModule } from '@angular/forms';
import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import { CadastroComponent } from './components/cadastro/cadastro.component';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { AtualizarSenhaComponent } from './components/atualizar-senha/atualizar-senha.component';
import { AtualizarSenhaInternoComponent } from './components/atualizar-senha-interno/atualizar-senha-interno.component';
import { PemissaoComponent } from './components/pemissao/pemissao.component';
import { AuthInterceptor } from './services/Auth/AuthInterceptor';
import { IonicModule } from './shared/ionic.module';

@NgModule({
  declarations: [AppComponent, CadastroComponent, AtualizarSenhaComponent, AtualizarSenhaInternoComponent, PemissaoComponent],
  imports: [BrowserModule, CommonModule, RouterModule.forRoot([]), IonicModule, AppRoutingModule, ReactiveFormsModule],
  providers: [
    { provide: RouteReuseStrategy, useClass: IonicRouteStrategy },
    provideIonicAngular(),
    provideHttpClient(withFetch(), withInterceptors([AuthInterceptor]))
  ],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  bootstrap: [AppComponent],
})
export class AppModule {}
