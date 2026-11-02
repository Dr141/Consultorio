import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideIonicAngular } from '@ionic/angular';
import { AgendaPage } from './agenda.page';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { of } from 'rxjs';

describe('AgendaPage', () => {
  let component: AgendaPage;
  let fixture: ComponentFixture<AgendaPage>;
  let apiSpy: jasmine.SpyObj<ConsultorioApiService>;

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ConsultorioApiService', ['obterAgendas', 'removerAgenda', 'obterMedicos', 'obterPacientes']);
    apiSpy.obterAgendas.and.returnValue(of([]));
    apiSpy.obterMedicos.and.returnValue(of([]));
    apiSpy.obterPacientes.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      declarations: [AgendaPage],
      providers: [provideIonicAngular(), { provide: ConsultorioApiService, useValue: apiSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(AgendaPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
