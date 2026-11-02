import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly STORAGE_KEY = 'consultorio_theme';
  private _isDark = false;

  constructor() {}

  public initTheme(): void {
    const saved = localStorage.getItem(this.STORAGE_KEY);
    if (saved === 'dark') {
      this.setTheme(true);
    } else if (saved === 'light') {
      this.setTheme(false);
    } else {
      // Preferência do sistema operacional
      const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
      this.setTheme(prefersDark);
    }
  }

  public isDark(): boolean {
    return this._isDark;
  }

  public toggleTheme(): boolean {
    this.setTheme(!this._isDark);
    return this._isDark;
  }

  public setTheme(dark: boolean): void {
    this._isDark = dark;
    localStorage.setItem(this.STORAGE_KEY, dark ? 'dark' : 'light');

    if (dark) {
      document.documentElement.classList.add('ion-palette-dark');
      document.body.classList.add('ion-palette-dark');
    } else {
      document.documentElement.classList.remove('ion-palette-dark');
      document.body.classList.remove('ion-palette-dark');
    }
  }
}
