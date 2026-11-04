import { Component } from '@angular/core';
import { ThemeService } from '../../services/theme/theme.service';

@Component({
  selector: 'app-theme-toggle',
  template: `
    <ion-button (click)="themeService.toggleTheme()" [title]="themeService.isDark() ? 'Alternar para Modo Claro' : 'Alternar para Modo Escuro'" [attr.aria-label]="themeService.isDark() ? 'Alternar para Modo Claro' : 'Alternar para Modo Escuro'">
      <ion-icon slot="icon-only" [name]="themeService.isDark() ? 'sunny-outline' : 'moon-outline'"></ion-icon>
    </ion-button>
  `,
  styles: [`
    :host {
      display: inline-flex;
      align-items: center;
    }
    ion-button {
      --color: inherit;
    }
  `],
  standalone: false
})
export class ThemeToggleComponent {
  constructor(public themeService: ThemeService) {}
}
