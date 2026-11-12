import { Component, Input, OnInit } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Cobranca, FORMAS_PAGAMENTO } from '../types/types';
import { hojeInputData } from '../shared/utils/formatadores';

/** Recebimento de uma cobrança: forma de pagamento, desconto opcional e data. Retorna a cobrança paga. */
@Component({
  selector: 'app-pagamento-modal',
  template: `
    <ion-header>
      <ion-toolbar color="primary">
        <ion-title>Receber pagamento</ion-title>
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
        <h3><strong>{{ cobranca.nomePaciente }}</strong></h3>
        <p>{{ cobranca.descricao }}</p>
        @if (cobranca.nomeConvenio) {
          <p>Convênio: {{ cobranca.nomeConvenio }}</p>
        }
      </div>

      <p class="secao-formulario">Forma de pagamento</p>
      <div class="formas-grid" role="radiogroup" aria-label="Forma de pagamento">
        @for (f of formas; track f.valor) {
          <button type="button" class="forma-opcao" role="radio" [attr.aria-checked]="forma === f.valor"
                  [class.ativo]="forma === f.valor" (click)="forma = f.valor">
            <ion-icon [name]="f.icone" aria-hidden="true"></ion-icon>
            <span>{{ f.nome }}</span>
          </button>
        }
      </div>

      <ion-grid class="ion-no-padding">
        <ion-row>
          <ion-col size="12" size-sm="6">
            <ion-item class="form-item">
              <ion-input type="number" inputmode="decimal" label="Desconto (R$)" labelPlacement="floating" min="0"
                         [value]="desconto" (ionInput)="desconto = numero($any($event).detail.value)"></ion-input>
            </ion-item>
          </ion-col>
          <ion-col size="12" size-sm="6">
            <ion-item class="form-item">
              <ion-input type="date" label="Data do pagamento" labelPlacement="floating" [max]="hoje"
                         [value]="data" (ionInput)="data = $any($event).detail.value"></ion-input>
            </ion-item>
          </ion-col>
        </ion-row>
      </ion-grid>

      <div class="total-pagamento" [class.erro]="!!erroDesconto">
        @if (erroDesconto) {
          <span>{{ erroDesconto }}</span>
        } @else {
          <span>Total a receber</span>
          <strong>{{ total | moeda }}</strong>
          @if (desconto) {
            <small>{{ cobranca.valor | moeda }} − {{ desconto | moeda }} de desconto</small>
          }
        }
      </div>

      <div class="acoes-formulario">
        <ion-button class="btn-cancelar" fill="outline" color="medium" (click)="fechar()">Cancelar</ion-button>
        <ion-button color="success" [disabled]="salvando || !forma || !!erroDesconto" (click)="confirmar()">
          @if (salvando) {
            <ion-spinner slot="start" name="crescent"></ion-spinner>
          } @else {
            <ion-icon slot="start" name="checkmark-circle-outline"></ion-icon>
          }
          {{ salvando ? 'Registrando...' : 'Confirmar recebimento' }}
        </ion-button>
      </div>
    </ion-content>
  `,
  styles: [`
    .formas-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(140px, 1fr)); gap: 8px; margin-bottom: 16px; }
    .forma-opcao { display: flex; align-items: center; gap: 8px; padding: 10px 12px; border-radius: 12px; cursor: pointer;
      border: 1px solid var(--app-border); background: var(--app-surface); color: var(--app-text); font: inherit; text-align: left; }
    .forma-opcao ion-icon { font-size: 20px; color: var(--ion-color-primary); flex-shrink: 0; }
    .forma-opcao.ativo { border-color: var(--ion-color-success); background: rgba(var(--ion-color-success-rgb), 0.12); font-weight: 600; }
    .total-pagamento { display: flex; flex-direction: column; align-items: center; gap: 2px; margin: 16px 0; padding: 16px;
      border-radius: 14px; background: rgba(var(--ion-color-success-rgb), 0.08); }
    .total-pagamento strong { font-size: 28px; color: var(--ion-color-success-shade); }
    .total-pagamento small { color: var(--app-text-muted); }
    .total-pagamento.erro { background: rgba(var(--ion-color-danger-rgb), 0.08); color: var(--ion-color-danger); font-weight: 600; }
  `],
  standalone: false
})
export class PagamentoModalComponent implements OnInit {
  @Input({ required: true }) cobranca!: Cobranca;

  readonly hoje = hojeInputData();
  formas = FORMAS_PAGAMENTO;
  forma: number | null = null;
  desconto: number | null = null;
  data = hojeInputData();
  salvando = false;

  constructor(
    private modalCtrl: ModalController,
    private api: ConsultorioApiService,
    private feedback: FeedbackService
  ) {}

  ngOnInit() {
    // "Convênio (repasse)" só faz sentido para cobranças de convênio.
    this.formas = FORMAS_PAGAMENTO.filter(f => f.chave !== 'Convenio' || !!this.cobranca.idConvenio);
    this.forma = this.cobranca.idConvenio ? 7 : null;
  }

  numero(valor: string | number | null | undefined): number | null {
    const n = Number(`${valor ?? ''}`.replace(',', '.'));
    return `${valor ?? ''}`.trim() === '' || isNaN(n) ? null : n;
  }

  get total(): number {
    return Math.max(0, this.cobranca.valor - (this.desconto ?? 0));
  }

  get erroDesconto(): string {
    if (this.desconto === null) return '';
    if (this.desconto < 0) return 'O desconto não pode ser negativo.';
    if (this.desconto >= this.cobranca.valor) return 'O desconto deve ser menor que o valor da cobrança.';
    return '';
  }

  fechar() {
    this.modalCtrl.dismiss(null);
  }

  confirmar() {
    if (!this.forma) return;
    this.salvando = true;
    this.api.pagarCobranca(this.cobranca.id, {
      forma: this.forma,
      desconto: this.desconto || null,
      dataPagamento: this.data && this.data !== this.hoje ? this.data : null
    }).subscribe({
      next: (paga) => {
        this.salvando = false;
        this.feedback.sucesso('Pagamento registrado.');
        this.modalCtrl.dismiss(paga);
      },
      error: (err) => {
        this.salvando = false;
        this.feedback.erro(err, 'Não foi possível registrar o pagamento.');
      }
    });
  }
}
