import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideIonicAngular } from '@ionic/angular';
import { ExamesPage } from './exames.page';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { of } from 'rxjs';

describe('ExamesPage', () => {
  let component: ExamesPage;
  let fixture: ComponentFixture<ExamesPage>;
  let apiSpy: jasmine.SpyObj<ConsultorioApiService>;

  beforeEach(async () => {
    apiSpy = jasmine.createSpyObj('ConsultorioApiService', ['obterExames', 'removerExame']);
    apiSpy.obterExames.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      declarations: [ExamesPage],
      providers: [provideIonicAngular(), { provide: ConsultorioApiService, useValue: apiSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(ExamesPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
