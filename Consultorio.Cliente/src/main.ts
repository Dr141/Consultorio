import { addIcons, setAssetPath } from 'ionicons';
import * as allIcons from 'ionicons/icons';

// Define o asset path para resolver qualquer SVG na raiz da aplicação
setAssetPath(window.location.origin + '/');

// Registra globalmente todos os ícones da biblioteca Ionicons v8
addIcons(allIcons);

import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { AppModule } from './app/app.module';

platformBrowserDynamic().bootstrapModule(AppModule)
  .catch(err => console.log(err));
