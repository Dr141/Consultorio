import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  let service: ThemeService;

  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove('ion-palette-dark');
    document.body.classList.remove('ion-palette-dark');
    TestBed.configureTestingModule({});
    service = TestBed.inject(ThemeService);
  });

  afterEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove('ion-palette-dark');
    document.body.classList.remove('ion-palette-dark');
  });

  it('deve ser criado com sucesso', () => {
    expect(service).toBeTruthy();
  });

  it('deve ativar o tema escuro e aplicar classe ion-palette-dark', () => {
    service.setTheme(true);
    expect(service.isDark()).toBeTrue();
    expect(localStorage.getItem('consultorio_theme')).toBe('dark');
    expect(document.documentElement.classList.contains('ion-palette-dark')).toBeTrue();
  });

  it('deve alternar entre claro e escuro via toggleTheme', () => {
    service.setTheme(false);
    expect(service.isDark()).toBeFalse();

    const novoEstado = service.toggleTheme();
    expect(novoEstado).toBeTrue();
    expect(service.isDark()).toBeTrue();
    expect(document.documentElement.classList.contains('ion-palette-dark')).toBeTrue();

    const segundoEstado = service.toggleTheme();
    expect(segundoEstado).toBeFalse();
    expect(service.isDark()).toBeFalse();
    expect(document.documentElement.classList.contains('ion-palette-dark')).toBeFalse();
  });

  it('deve respeitar a preferência salva no localStorage no initTheme', () => {
    localStorage.setItem('consultorio_theme', 'dark');
    service.initTheme();
    expect(service.isDark()).toBeTrue();
    expect(document.documentElement.classList.contains('ion-palette-dark')).toBeTrue();

    localStorage.setItem('consultorio_theme', 'light');
    service.initTheme();
    expect(service.isDark()).toBeFalse();
    expect(document.documentElement.classList.contains('ion-palette-dark')).toBeFalse();
  });
});
