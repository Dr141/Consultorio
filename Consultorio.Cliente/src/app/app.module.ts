import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { RouteReuseStrategy, RouterModule } from '@angular/router';
import { IonicModule, IonicRouteStrategy } from '@ionic/angular';
import { ReactiveFormsModule } from '@angular/forms';
import { AppComponent } from './app.component';
import { AppRoutingModule } from './app-routing.module';
import { CadastroComponent } from 'src/app/components/cadastro/cadastro.component';
import { HTTP_INTERCEPTORS, provideHttpClient, withFetch } from '@angular/common/http';
import { AtualizarSenhaComponent } from './components/atualizar-senha/atualizar-senha.component';
import { AtualizarSenhaInternoComponent } from './components/atualizar-senha-interno/atualizar-senha-interno.component';
import { PemissaoComponent } from './components/pemissao/pemissao.component';
import { AuthInterceptorService } from './services/Auth/auth-interceptor.service';

@NgModule({
  declarations: [AppComponent, CadastroComponent, AtualizarSenhaComponent, AtualizarSenhaInternoComponent, PemissaoComponent],
  imports: [BrowserModule, RouterModule.forRoot([]), IonicModule.forRoot({}), AppRoutingModule, ReactiveFormsModule],
  providers: [{ provide: RouteReuseStrategy, useClass: IonicRouteStrategy },
    provideHttpClient(
      withFetch()
    ),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptorService,
      multi: true
    }],
  bootstrap: [AppComponent],
})
export class AppModule {}
