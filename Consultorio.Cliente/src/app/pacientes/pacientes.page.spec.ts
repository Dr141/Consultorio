import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideIonicAngular } from '@ionic/angular';
import { PacientesPage } from './pacientes.page';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { of } from 'rxjs';

describe('PacientesPage', () => {
  let component: PacientesPage;
  let fixture: ComponentFixture<PacientesPage>;
  let apiSpy: jasmine.SpyObj<ConsultorioApiService>;

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ConsultorioApiService', ['obterPacientes', 'removerPaciente', 'reativarPaciente']);
    apiSpy.obterPacientes.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      declarations: [PacientesPage],
      providers: [provideIonicAngular(), { provide: ConsultorioApiService, useValue: apiSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(PacientesPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
