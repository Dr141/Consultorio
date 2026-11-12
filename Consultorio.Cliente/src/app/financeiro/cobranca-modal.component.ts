import { Component, Input, OnInit } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Convenio, Paciente } from '../types/types';
import { SelecionarPacienteComponent } from '../shared/selecionar-paciente/selecionar-paciente.component';
import { hojeInputData } from '../shared/utils/formatadores';

/** Lançamento de cobrança avulsa (procedimento, taxa, pacote...). Retorna a cobrança criada. */
@Component({
  selector: 'app-cobranca-modal',
  template: `
    <ion-header>
      <ion-toolbar color="primary">
        <ion-title>Nova cobrança</ion-title>
        <ion-buttons slot="end">
          <ion-button (click)="fechar()">
            <ion-icon slot="start" name="close-outline"></ion-icon>
            Fechar
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content class="ion-padding">
      <p class="secao-formulario">Paciente</p>
      <ion-item class="form-item" button detail="true" (click)="escolherPaciente()">
        <ion-icon slot="start" name="person-outline" color="primary"></ion-icon>
        <ion-label>
          @if (paciente) {
            <h3>{{ paciente.nome }}</h3>
            <p>{{ paciente.cpf | cpf }}{{ paciente.nomeConvenio ? ' · ' + paciente.nomeConvenio : ' · Particular' }}</p>
          } @else {
            <h3>Selecionar paciente *</h3>
          }
        </ion-label>
      </ion-item>

      <p class="secao-formulario">Cobrança</p>
      <ion-item class="form-item">
        <ion-input label="Descrição *" labelPlacement="floating" [maxlength]="200" [counter]="true"
                   placeholder="Ex.: Infiltração, taxa de material, pacote de sessões"
                   [value]="descricao" (ionInput)="descricao = $any($event).detail.value ?? ''"></ion-input>
      </ion-item>
      <ion-grid class="ion-no-padding">
        <ion-row>
          <ion-col size="12" size-sm="6">
            <ion-item class="form-item">
              <ion-input type="number" inputmode="decimal" label="Valor (R$) *" labelPlacement="floating" min="0.01"
                         [value]="valor" (ionInput)="valor = numero($any($event).detail.value)"></ion-input>
            </ion-item>
          </ion-col>
          <ion-col size="12" size-sm="6">
            <ion-item class="form-item">
              <ion-input type="date" label="Vencimento" labelPlacement="floating"
                         [value]="vencimento" (ionInput)="vencimento = $any($event).detail.value"></ion-input>
            </ion-item>
          </ion-col>
        </ion-row>
      </ion-grid>
      <ion-item class="form-item">
        <ion-select label="Quem paga" labelPlacement="floating" [value]="idConvenio" (ionChange)="idConvenio = $any($event).detail.value"
                    cancelText="Cancelar" okText="Confirmar">
          <ion-select-option [value]="null">Particular (paciente)</ion-select-option>
          @for (c of convenios; track c.id) {
            <ion-select-option [value]="c.id">{{ c.nome }}</ion-select-option>
          }
        </ion-select>
      </ion-item>

      <div class="acoes-formulario">
        <ion-button class="btn-cancelar" fill="outline" color="medium" (click)="fechar()">Cancelar</ion-button>
        <ion-button color="primary" [disabled]="salvando || !valido" (click)="salvar()">
          @if (salvando) {
            <ion-spinner slot="start" name="crescent"></ion-spinner>
          } @else {
            <ion-icon slot="start" name="save-outline"></ion-icon>
          }
          {{ salvando ? 'Salvando...' : 'Lançar cobrança' }}
        </ion-button>
      </div>
    </ion-content>
  `,
  standalone: false
})
export class CobrancaModalComponent implements OnInit {
  @Input() idPacienteInicial?: number;

  pacientes: Paciente[] = [];
  convenios: Convenio[] = [];
  paciente: Paciente | null = null;
  descricao = '';
  valor: number | null = null;
  vencimento = hojeInputData();
  idConvenio: number | null = null;
  salvando = false;

  constructor(
    private modalCtrl: ModalController,
    private api: ConsultorioApiService,
    private feedback: FeedbackService
  ) {}

  ngOnInit() {
    this.api.obterPacientes().subscribe({
      next: (lista) => {
        this.pacientes = [...lista].sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR'));
        if (this.idPacienteInicial) this.definirPaciente(this.pacientes.find(p => p.id === this.idPacienteInicial) ?? null);
      },
      error: (err) => this.feedback.erro(err, 'Não foi possível carregar os pacientes.')
    });
    this.api.obterConvenios().subscribe({ next: (c) => this.convenios = c, error: () => undefined });
  }

  get valido(): boolean {
    return !!this.paciente && this.descricao.trim().length >= 2 && (this.valor ?? 0) > 0;
  }

  numero(valor: string | number | null | undefined): number | null {
    const n = Number(`${valor ?? ''}`.replace(',', '.'));
    return `${valor ?? ''}`.trim() === '' || isNaN(n) ? null : n;
  }

  private definirPaciente(p: Paciente | null) {
    this.paciente = p;
    this.idConvenio = p?.idConvenio ?? null;
  }

  async escolherPaciente() {
    const modal = await this.modalCtrl.create({
      component: SelecionarPacienteComponent,
      componentProps: { pacientes: this.pacientes, titulo: 'Paciente da cobrança' }
    });
    await modal.present();
    const { data: id, role } = await modal.onDidDismiss<number>();
    if (role === 'selecionado' && id) this.definirPaciente(this.pacientes.find(p => p.id === id) ?? null);
  }

  fechar() {
    this.modalCtrl.dismiss(null);
  }

  salvar() {
    if (!this.valido || !this.paciente) return;
    this.salvando = true;
    this.api.criarCobranca({
      idPaciente: this.paciente.id,
      descricao: this.descricao.trim(),
      valor: this.valor!,
      dtVencimento: this.vencimento || null,
      idConvenio: this.idConvenio
    }).subscribe({
      next: (c) => {
        this.salvando = false;
        this.feedback.sucesso('Cobrança lançada.');
        this.modalCtrl.dismiss(c);
      },
      error: (err) => {
        this.salvando = false;
        this.feedback.erro(err, 'Não foi possível lançar a cobrança.');
      }
    });
  }
}
