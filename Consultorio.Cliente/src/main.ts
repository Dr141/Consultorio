import { addIcons, setAssetPath } from 'ionicons';
import * as allIcons from 'ionicons/icons';

// Define o asset path para resolver qualquer SVG na raiz da aplicação
setAssetPath(window.location.origin + '/');

// Registra globalmente todos os ícones da biblioteca Ionicons v8
addIcons(allIcons);

import { provideZoneChangeDetection } from '@angular/core';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { AppModule } from './app/app.module';

// O Angular 21+ é zoneless por padrão. As telas atualizam estado em callbacks do Ionic (ionViewWillEnter)
// e dentro de subscribe, então a detecção de mudanças por zone.js (carregado nos polyfills) é ligada aqui.
// Com bootstrapModule, o provider precisa ir em applicationProviders: nos providers do NgModule ele é ignorado.
platformBrowserDynamic().bootstrapModule(AppModule, {
  applicationProviders: [provideZoneChangeDetection({ eventCoalescing: true })]
})
  .catch(err => console.log(err));
