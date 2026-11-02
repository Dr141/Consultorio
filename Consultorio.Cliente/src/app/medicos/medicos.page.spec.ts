import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideIonicAngular } from '@ionic/angular';
import { MedicosPage } from './medicos.page';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { of } from 'rxjs';

describe('MedicosPage', () => {
  let component: MedicosPage;
  let fixture: ComponentFixture<MedicosPage>;
  let apiSpy: jasmine.SpyObj<ConsultorioApiService>;

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ConsultorioApiService', ['obterMedicos', 'removerMedico']);
    apiSpy.obterMedicos.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      declarations: [MedicosPage],
      providers: [provideIonicAngular(), { provide: ConsultorioApiService, useValue: apiSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(MedicosPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
