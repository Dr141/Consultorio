import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';
import { IonicModule } from '@ionic/angular';

import { AtualizarSenhaInternoComponent } from './atualizar-senha-interno.component';

describe('AtualizarSenhaInternoComponent', () => {
  let component: AtualizarSenhaInternoComponent;
  let fixture: ComponentFixture<AtualizarSenhaInternoComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ AtualizarSenhaInternoComponent ],
      imports: [IonicModule.forRoot()]
    }).compileComponents();

    fixture = TestBed.createComponent(AtualizarSenhaInternoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }));

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
