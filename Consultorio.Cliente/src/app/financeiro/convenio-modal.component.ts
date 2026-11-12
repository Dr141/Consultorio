import { Component, Input, OnInit } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Convenio } from '../types/types';

@Component({
  selector: 'app-convenio-modal',
  template: `
    <ion-header>
      <ion-toolbar color="primary">
        <ion-title>{{ convenio ? 'Editar convênio' : 'Novo convênio' }}</ion-title>
        <ion-buttons slot="end">
          <ion-button (click)="fechar()">
            <ion-icon slot="start" name="close-outline"></ion-icon>
            Fechar
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <ion-item class="form-item">
        <ion-input label="Nome *" labelPlacement="floating" [maxlength]="120" placeholder="Ex.: Unimed, Bradesco Saúde"
                   [value]="nome" (ionInput)="nome = $any($event).detail.value ?? ''"
                   [class.ion-invalid]="tocado && nome.trim().length < 2" [class.ion-touched]="tocado"
                   errorText="Informe o nome (mínimo de 2 caracteres)."></ion-input>
      </ion-item>
      <ion-grid class="ion-no-padding">
        <ion-row>
          <ion-col size="12" size-sm="6">
            <ion-item class="form-item">
              <ion-input label="Registro ANS" labelPlacement="floating" inputmode="numeric" [maxlength]="6" helperText="6 dígitos (opcional)"
                         [value]="registroAns" (ionInput)="registroAns = $any($event).detail.value ?? ''"></ion-input>
            </ion-item>
          </ion-col>
          <ion-col size="12" size-sm="6">
            <ion-item class="form-item">
              <ion-input label="Telefone / central" labelPlacement="floating" [maxlength]="30"
                         [value]="telefone" (ionInput)="telefone = $any($event).detail.value ?? ''"></ion-input>
            </ion-item>
          </ion-col>
        </ion-row>
      </ion-grid>
      <ion-item class="form-item">
        <ion-textarea label="Observações" labelPlacement="floating" [autoGrow]="true" rows="3" [maxlength]="500" [counter]="true"
                      placeholder="Ex.: exige guia autorizada, prazo de repasse"
                      [value]="observacao" (ionInput)="observacao = $any($event).detail.value ?? ''"></ion-textarea>
      </ion-item>

      <div class="acoes-formulario">
        <ion-button class="btn-cancelar" fill="outline" color="medium" (click)="fechar()">Cancelar</ion-button>
        <ion-button color="primary" [disabled]="salvando" (click)="salvar()">
          @if (salvando) {
            <ion-spinner slot="start" name="crescent"></ion-spinner>
          } @else {
            <ion-icon slot="start" name="save-outline"></ion-icon>
          }
          {{ salvando ? 'Salvando...' : 'Salvar convênio' }}
        </ion-button>
      </div>
    </ion-content>
  `,
  standalone: false
})
export class ConvenioModalComponent implements OnInit {
  @Input() convenio?: Convenio;

  nome = '';
  registroAns = '';
  telefone = '';
  observacao = '';
  tocado = false;
  salvando = false;

  constructor(private modalCtrl: ModalController, private api: ConsultorioApiService, private feedback: FeedbackService) {}

  ngOnInit() {
    this.nome = this.convenio?.nome ?? '';
    this.registroAns = this.convenio?.registroAns ?? '';
    this.telefone = this.convenio?.telefone ?? '';
    this.observacao = this.convenio?.observacao ?? '';
  }

  fechar() {
    this.modalCtrl.dismiss(null);
  }

  salvar() {
    this.tocado = true;
    if (this.nome.trim().length < 2) {
      this.feedback.aviso('Informe o nome do convênio.');
      return;
    }
    const ans = this.registroAns.replace(/\D/g, '');
    if (ans && ans.length !== 6) {
      this.feedback.aviso('O registro ANS deve ter 6 dígitos.');
      return;
    }
    this.salvando = true;
    this.api.salvarConvenio({
      nome: this.nome.trim(),
      registroAns: ans || null,
      telefone: this.telefone.trim() || null,
      observacao: this.observacao.trim() || null
    }, this.convenio?.id).subscribe({
      next: (c) => {
        this.salvando = false;
        this.feedback.sucesso('Convênio salvo.');
        this.modalCtrl.dismiss(c);
      },
      error: (err) => {
        this.salvando = false;
        this.feedback.erro(err, 'Não foi possível salvar o convênio.');
      }
    });
  }
}
