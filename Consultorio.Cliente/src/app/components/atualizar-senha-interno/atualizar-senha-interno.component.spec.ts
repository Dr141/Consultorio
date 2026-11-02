import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { IonicModule } from '../../shared/ionic.module';
import { provideIonicAngular } from '@ionic/angular';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { AtualizarSenhaInternoComponent } from './atualizar-senha-interno.component';

describe('AtualizarSenhaInternoComponent', () => {
  let component: AtualizarSenhaInternoComponent;
  let fixture: ComponentFixture<AtualizarSenhaInternoComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ AtualizarSenhaInternoComponent ],
      imports: [IonicModule],
      providers: [provideIonicAngular(), provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    fixture = TestBed.createComponent(AtualizarSenhaInternoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }));

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
