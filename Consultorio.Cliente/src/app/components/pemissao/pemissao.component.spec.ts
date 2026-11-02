import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { IonicModule } from '../../shared/ionic.module';
import { provideIonicAngular } from '@ionic/angular';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';

import { PemissaoComponent } from './pemissao.component';
import { ConsultorioApiService } from '../../services/api/consultorio-api.service';

describe('PemissaoComponent', () => {
  let component: PemissaoComponent;
  let fixture: ComponentFixture<PemissaoComponent>;
  let apiSpy: any;

  beforeEach(waitForAsync(() => {
    apiSpy = jasmine.createSpyObj('ConsultorioApiService', ['obterRoles', 'adicionarRole', 'removerRole']);
    apiSpy.obterRoles.and.returnValue(of(['Admin', 'User']));

    TestBed.configureTestingModule({
      declarations: [ PemissaoComponent ],
      imports: [IonicModule],
      providers: [provideIonicAngular(), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    fixture = TestBed.createComponent(PemissaoComponent);
    component = fixture.componentInstance;
    component.api = apiSpy;
    component.user = { email: 'test@example.com', emailConfirmado: true, claims: [], roles: [] };
    fixture.detectChanges();
  }));

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
