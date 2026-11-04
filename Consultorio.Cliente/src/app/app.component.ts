import { Component, OnInit } from '@angular/core';
import { ThemeService } from './services/theme/theme.service';
import { addIcons } from 'ionicons';
import * as allIcons from 'ionicons/icons';

@Component({
  selector: 'app-root',
  templateUrl: 'app.component.html',
  styleUrls: ['app.component.scss'],
  standalone: false
})
export class AppComponent implements OnInit {
  constructor(private themeService: ThemeService) {
    // Registra globalmente todos os ícones da biblioteca Ionicons v8
    addIcons(allIcons);
  }

  ngOnInit(): void {
    this.themeService.initTheme();
  }
}
