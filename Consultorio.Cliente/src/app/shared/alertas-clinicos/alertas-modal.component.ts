import { Component, Input, OnInit } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../../services/api/consultorio-api.service';
import { FeedbackService } from '../../services/feedback/feedback.service';
import { AlertasClinicos, Paciente } from '../../types/types';

type CampoAlerta = { chave: keyof AlertasClinicos; rotulo: string; dica: string; icone: string };

/** Edição dos alertas clínicos do paciente (um item por linha). Retorna o paciente atualizado. */
@Component({
  selector: 'app-alertas-modal',
  template: `
    <ion-header>
      <ion-toolbar color="primary">
        <ion-title>Alertas clínicos</ion-title>
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
        <h3><strong>Paciente:</strong> {{ paciente.nome }}</h3>
        <p>Esses alertas aparecem no registro de consultas, na emissão de documentos e no prontuário.</p>
      </div>

      @for (campo of campos; track campo.chave) {
        <p class="secao-formulario">
          <ion-icon [name]="campo.icone" aria-hidden="true"></ion-icon>
          {{ campo.rotulo }}
        </p>
        <ion-item class="form-item">
          <ion-textarea [value]="valores[campo.chave]" (ionInput)="alterar(campo.chave, $any($event).detail.value)"
                        [autoGrow]="true" rows="3" [maxlength]="2000" [counter]="true"
                        [placeholder]="campo.dica" [attr.aria-label]="campo.rotulo"></ion-textarea>
        </ion-item>
      }
      <p class="dica-alertas">Escreva um item por linha. Deixe em branco o que não se aplica.</p>

      <div class="acoes-formulario">
        <ion-button class="btn-cancelar" fill="outline" color="medium" type="button" (click)="fechar()">Cancelar</ion-button>
        <ion-button color="primary" [disabled]="salvando" (click)="salvar()">
          @if (salvando) {
            <ion-spinner slot="start" name="crescent"></ion-spinner>
          } @else {
            <ion-icon slot="start" name="save-outline"></ion-icon>
          }
          {{ salvando ? 'Salvando...' : 'Salvar alertas' }}
        </ion-button>
      </div>
    </ion-content>
  `,
  styles: [`
    .secao-formulario { display: flex; align-items: center; gap: 6px; }
    .dica-alertas { margin: 0 4px 12px; font-size: 12px; color: var(--app-text-muted); }
  `],
  standalone: false
})
export class AlertasModalComponent implements OnInit {
  @Input({ required: true }) paciente!: Paciente;

  readonly campos: CampoAlerta[] = [
    { chave: 'alergias', rotulo: 'Alergias', dica: 'Ex.: Dipirona\nPenicilina\nLátex', icone: 'warning-outline' },
    { chave: 'condicoesCronicas', rotulo: 'Condições crônicas', dica: 'Ex.: Hipertensão arterial\nDiabetes tipo 2', icone: 'pulse-outline' },
    { chave: 'medicamentosEmUso', rotulo: 'Medicamentos em uso contínuo', dica: 'Ex.: Losartana 50 mg 1x ao dia', icone: 'medkit-outline' }
  ];

  valores: AlertasClinicos = { alergias: '', medicamentosEmUso: '', condicoesCronicas: '' };
  salvando = false;
  private alterado = false;

  constructor(
    private modalCtrl: ModalController,
    private api: ConsultorioApiService,
    private feedback: FeedbackService
  ) {}

  ngOnInit() {
    this.valores = {
      alergias: this.paciente.alergias ?? '',
      medicamentosEmUso: this.paciente.medicamentosEmUso ?? '',
      condicoesCronicas: this.paciente.condicoesCronicas ?? ''
    };
  }

  alterar(chave: keyof AlertasClinicos, valor: string | null | undefined) {
    this.valores = { ...this.valores, [chave]: valor ?? '' };
    this.alterado = true;
  }

  async fechar() {
    if (this.alterado && !this.salvando) {
      const descartar = await this.feedback.confirmar({
        titulo: 'Descartar alterações?',
        mensagem: 'Os alertas ainda não foram salvos.',
        textoConfirmar: 'Descartar',
        textoCancelar: 'Continuar editando',
        destrutivo: true
      });
      if (!descartar) return;
    }
    this.modalCtrl.dismiss(null);
  }

  salvar() {
    const limpar = (v: string | null) => (v ?? '').trim() || null;
    this.salvando = true;
    this.api.atualizarAlertasPaciente(this.paciente.id, {
      alergias: limpar(this.valores.alergias),
      medicamentosEmUso: limpar(this.valores.medicamentosEmUso),
      condicoesCronicas: limpar(this.valores.condicoesCronicas)
    }).subscribe({
      next: (atualizado) => {
        this.salvando = false;
        this.feedback.sucesso('Alertas clínicos atualizados.');
        this.modalCtrl.dismiss(atualizado);
      },
      error: (err) => {
        this.salvando = false;
        this.feedback.erro(err, 'Não foi possível salvar os alertas.');
      }
    });
  }
}
