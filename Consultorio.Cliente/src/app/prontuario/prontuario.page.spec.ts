import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideIonicAngular } from '@ionic/angular';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { ProntuarioPage } from './prontuario.page';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';

describe('ProntuarioPage', () => {
  let component: ProntuarioPage;
  let fixture: ComponentFixture<ProntuarioPage>;
  let apiSpy: jasmine.SpyObj<ConsultorioApiService>;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ConsultorioApiService', ['obterPacientes', 'obterMedicos', 'obterProntuario']);
    apiSpy.obterPacientes.and.returnValue(of([]));
    apiSpy.obterMedicos.and.returnValue(of([]));

    routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      declarations: [ProntuarioPage],
      providers: [
        provideIonicAngular(),
        { provide: ConsultorioApiService, useValue: apiSpy },
        { provide: Router, useValue: routerSpy },
        {
          provide: ActivatedRoute,
          useValue: {
            queryParams: of({})
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProntuarioPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve criar a página de prontuário', () => {
    expect(component).toBeTruthy();
  });

  it('deve definir período rápido de 30 dias corretamente', () => {
    component.definirPeriodo(30);
    expect(component.dataInicio).toBeDefined();
    expect(component.dataFim).toBeDefined();
    expect(component.dataInicio.length).toBe(10);
    expect(component.dataFim.length).toBe(10);
  });

  it('deve limpar filtros', () => {
    component.idPacienteSelecionado = 1;
    component.idMedicoSelecionado = 2;
    component.dataInicio = '2026-01-01';
    component.dataFim = '2026-06-01';

    component.limparFiltros();

    expect(component.idPacienteSelecionado).toBeNull();
    expect(component.idMedicoSelecionado).toBeNull();
    expect(component.dataInicio).toBe('');
    expect(component.dataFim).toBe('');
    expect(component.prontuario).toBeNull();
  });
});
