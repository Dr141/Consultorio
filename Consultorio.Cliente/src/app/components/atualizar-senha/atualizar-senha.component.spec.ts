import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { IonicModule } from '../../shared/ionic.module';
import { provideIonicAngular } from '@ionic/angular';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { AtualizarSenhaComponent } from './atualizar-senha.component';

describe('AtualizarSenhaComponent', () => {
  let component: AtualizarSenhaComponent;
  let fixture: ComponentFixture<AtualizarSenhaComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ AtualizarSenhaComponent ],
      imports: [IonicModule],
      providers: [provideIonicAngular(), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    fixture = TestBed.createComponent(AtualizarSenhaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }));

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
