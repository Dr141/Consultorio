import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideIonicAngular } from '@ionic/angular';
import { ConsultasPage } from './consultas.page';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { of } from 'rxjs';

describe('ConsultasPage', () => {
  let component: ConsultasPage;
  let fixture: ComponentFixture<ConsultasPage>;
  let apiSpy: jasmine.SpyObj<ConsultorioApiService>;

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ConsultorioApiService', ['obterConsultas', 'finalizarConsulta', 'anularConsulta', 'adicionarAdendoConsulta']);
    apiSpy.obterConsultas.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      declarations: [ConsultasPage],
      providers: [provideIonicAngular(), { provide: ConsultorioApiService, useValue: apiSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(ConsultasPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
