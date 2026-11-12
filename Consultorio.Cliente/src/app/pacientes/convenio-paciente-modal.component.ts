import { Component, Input, OnInit } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Convenio, Paciente } from '../types/types';
import { hojeInputData, paraInputData } from '../shared/utils/formatadores';

/** Convênio e carteirinha do paciente. Sem convênio = particular. Retorna o paciente atualizado. */
@Component({
  selector: 'app-convenio-paciente-modal',
  template: `
    <ion-header>
      <ion-toolbar color="primary">
        <ion-title>Convênio do paciente</ion-title>
        <ion-buttons slot="end">
          <ion-button (click)="fechar()">
            <ion-icon slot="start" name="close-outline"></ion-icon>
            Fechar
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <div class="exame-info-card">
        <h3><strong>{{ paciente.nome }}</strong></h3>
        <p>O valor do atendimento é cobrado do convênio enquanto a carteirinha estiver válida; depois, como particular.</p>
      </div>

      <ion-item class="form-item">
        <ion-select label="Convênio" labelPlacement="floating" [value]="idConvenio" (ionChange)="idConvenio = $any($event).detail.value"
                    cancelText="Cancelar" okText="Confirmar">
          <ion-select-option [value]="null">Particular (sem convênio)</ion-select-option>
          @for (c of convenios; track c.id) {
            <ion-select-option [value]="c.id">{{ c.nome }}{{ c.ativo ? '' : ' (inativo)' }}</ion-select-option>
          }
        </ion-select>
      </ion-item>
      @if (convenios.length === 0) {
        <p class="dica">Nenhum convênio cadastrado. A gestão cadastra em Financeiro › Convênios.</p>
      }

      @if (idConvenio) {
        <ion-grid class="ion-no-padding">
          <ion-row>
            <ion-col size="12" size-sm="7">
              <ion-item class="form-item">
                <ion-input label="Número da carteirinha *" labelPlacement="floating" [maxlength]="30"
                           [value]="numero" (ionInput)="numero = $any($event).detail.value ?? ''"></ion-input>
              </ion-item>
            </ion-col>
            <ion-col size="12" size-sm="5">
              <ion-item class="form-item">
                <ion-input type="date" label="Validade" labelPlacement="floating" [value]="validade"
                           (ionInput)="validade = $any($event).detail.value ?? ''"></ion-input>
              </ion-item>
            </ion-col>
          </ion-row>
        </ion-grid>
        @if (vencida) {
          <p class="dica erro">A validade informada já passou: os atendimentos serão cobrados como particular até a atualização.</p>
        }
      }

      <div class="acoes-formulario">
        <ion-button class="btn-cancelar" fill="outline" color="medium" (click)="fechar()">Cancelar</ion-button>
        <ion-button color="primary" [disabled]="salvando || (!!idConvenio && numero.trim().length < 3)" (click)="salvar()">
          @if (salvando) {
            <ion-spinner slot="start" name="crescent"></ion-spinner>
          } @else {
            <ion-icon slot="start" name="save-outline"></ion-icon>
          }
          {{ salvando ? 'Salvando...' : 'Salvar' }}
        </ion-button>
      </div>
    </ion-content>
  `,
  styles: [`
    .dica { margin: 0 4px 12px; font-size: 13px; color: var(--app-text-muted); }
    .dica.erro { color: var(--ion-color-danger); }
  `],
  standalone: false
})
export class ConvenioPacienteModalComponent implements OnInit {
  @Input({ required: true }) paciente!: Paciente;

  convenios: Convenio[] = [];
  idConvenio: number | null = null;
  numero = '';
  validade = '';
  salvando = false;

  constructor(private modalCtrl: ModalController, private api: ConsultorioApiService, private feedback: FeedbackService) {}

  ngOnInit() {
    this.idConvenio = this.paciente.idConvenio ?? null;
    this.numero = this.paciente.numeroCarteirinha ?? '';
    this.validade = paraInputData(this.paciente.validadeCarteirinha);
    // Inclui inativos apenas para exibir o convênio atual do paciente.
    this.api.obterConvenios(true).subscribe({
      next: (lista) => this.convenios = lista.filter(c => c.ativo || c.id === this.paciente.idConvenio),
      error: (err) => this.feedback.erro(err, 'Não foi possível carregar os convênios.')
    });
  }

  get vencida(): boolean {
    return !!this.validade && this.validade < hojeInputData();
  }

  fechar() {
    this.modalCtrl.dismiss(null);
  }

  salvar() {
    this.salvando = true;
    this.api.atualizarConvenioPaciente(this.paciente.id, {
      idConvenio: this.idConvenio,
      numeroCarteirinha: this.idConvenio ? this.numero.trim() : null,
      validadeCarteirinha: this.idConvenio && this.validade ? this.validade : null
    }).subscribe({
      next: (p) => {
        this.salvando = false;
        this.feedback.sucesso(this.idConvenio ? 'Convênio atualizado.' : 'Paciente definido como particular.');
        this.modalCtrl.dismiss(p);
      },
      error: (err) => {
        this.salvando = false;
        this.feedback.erro(err, 'Não foi possível salvar o convênio.');
      }
    });
  }
}
