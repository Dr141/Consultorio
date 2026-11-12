import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { PortalService } from '../services/portal/portal.service';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { Recibo } from '../types/types';

/** Recibo de pagamento para impressão (ou "Salvar como PDF"), útil ao paciente para reembolso e IR. */
@Component({
  selector: 'app-recibo',
  template: `
    <ion-header>
      <ion-toolbar color="primary">
        <ion-buttons slot="start">
          <ion-back-button [defaultHref]="portal ? '/portal/inicio' : '/financeiro'" text="Voltar"></ion-back-button>
        </ion-buttons>
        <ion-title>Recibo</ion-title>
        <ion-buttons slot="end">
          @if (recibo) {
            <ion-button (click)="imprimir()">
              <ion-icon slot="start" name="print-outline"></ion-icon>
              Imprimir / Salvar PDF
            </ion-button>
          }
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content>
      <div class="page-container">
        @if (carregando) {
          <div class="loading-state"><ion-spinner name="crescent"></ion-spinner><p>Carregando recibo...</p></div>
        } @else if (!recibo) {
          <div class="ion-text-center ion-padding empty-state">
            <ion-icon name="alert-circle-outline"></ion-icon>
            <h3>Recibo indisponível</h3>
            <p>{{ erro }}</p>
          </div>
        } @else {
          <div class="recibo-folha">
            <header>
              <div>
                <h1>{{ recibo.nomeClinica }}</h1>
                @if (recibo.enderecoClinica || recibo.telefoneClinica) {
                  <p>{{ recibo.enderecoClinica }}{{ recibo.enderecoClinica && recibo.telefoneClinica ? ' · ' : '' }}{{ recibo.telefoneClinica }}</p>
                }
              </div>
              <div class="numero">
                <span>RECIBO Nº</span>
                <strong>{{ recibo.cobranca.id }}</strong>
                <span class="valor">{{ recibo.cobranca.valorFinal | moeda }}</span>
              </div>
            </header>

            <p class="texto">
              Recebi(emos) de <strong>{{ recibo.cobranca.nomePaciente }}</strong>@if (recibo.cobranca.cpfPaciente) {<span>, CPF <strong>{{ recibo.cobranca.cpfPaciente }}</strong></span>},
              a importância de <strong>{{ recibo.cobranca.valorFinal | moeda }}</strong>
              ({{ recibo.valorPorExtenso }}), referente a <strong>{{ recibo.cobranca.descricao }}</strong>.
            </p>

            <dl>
              <dt>Forma de pagamento</dt><dd>{{ recibo.cobranca.formaPagamentoNome }}</dd>
              <dt>Data do pagamento</dt><dd>{{ recibo.cobranca.dtPagamento | dataBr:'dataHora' }}</dd>
              @if (recibo.cobranca.desconto > 0) {
                <dt>Valor original</dt><dd>{{ recibo.cobranca.valor | moeda }} (desconto de {{ recibo.cobranca.desconto | moeda }})</dd>
              }
              @if (recibo.cobranca.nomeConvenio) {
                <dt>Convênio</dt><dd>{{ recibo.cobranca.nomeConvenio }}</dd>
              }
            </dl>

            <p class="local-data">{{ recibo.dtEmissao | dataBr:'extenso' }}</p>
            <div class="assinatura">
              <div class="linha"></div>
              <span>{{ recibo.nomeClinica }}</span>
            </div>
          </div>
        }
      </div>
    </ion-content>
  `,
  styles: [`
    .recibo-folha { max-width: 760px; margin: 0 auto 24px; padding: 36px 44px; background: #fff; color: #1a1a1a;
      border: 1px solid #e2e8f0; border-radius: 6px; box-shadow: 0 4px 20px rgba(0,0,0,.08); font-family: 'Segoe UI', Roboto, Arial, sans-serif; line-height: 1.6; }
    header { display: flex; justify-content: space-between; gap: 16px; padding-bottom: 14px; border-bottom: 2px solid #0f172a; margin-bottom: 24px; }
    h1 { margin: 0; font-size: 22px; }
    header p { margin: 2px 0 0; font-size: 13px; color: #475569; }
    .numero { display: flex; flex-direction: column; align-items: flex-end; text-align: right; }
    .numero span { font-size: 11px; color: #64748b; letter-spacing: 1px; }
    .numero strong { font-size: 20px; }
    .numero .valor { font-size: 18px; font-weight: 700; color: #0f172a; letter-spacing: 0; border: 1px solid #0f172a; padding: 2px 10px; border-radius: 6px; margin-top: 4px; }
    .texto { font-size: 16px; text-align: justify; margin: 0 0 20px; }
    dl { display: grid; grid-template-columns: max-content 1fr; gap: 6px 16px; margin: 0 0 32px; font-size: 14px; }
    dt { color: #64748b; }
    dd { margin: 0; font-weight: 600; }
    .local-data { text-align: right; margin: 0 0 56px; }
    .assinatura { display: flex; flex-direction: column; align-items: center; gap: 4px; }
    .assinatura .linha { width: 300px; max-width: 80%; border-top: 1px solid #0f172a; }
    @media (max-width: 600px) { .recibo-folha { padding: 20px 16px; } header { flex-direction: column; } .numero { align-items: flex-start; text-align: left; } }
    @media print { .recibo-folha { border: none; box-shadow: none; margin: 0; padding: 0; max-width: 100%; } }
  `],
  standalone: false
})
export class ReciboPage implements OnInit {
  recibo: Recibo | null = null;
  carregando = true;
  erro = '';
  /** Aberto pelo portal do paciente (usa a sessão do portal em vez do login da equipe). */
  portal = false;

  constructor(private rota: ActivatedRoute, private api: ConsultorioApiService, private router: Router, private portalService: PortalService) {}

  ngOnInit() {
    const id = Number(this.rota.snapshot.paramMap.get('id'));
    if (!id) {
      this.carregando = false;
      this.erro = 'Recibo inválido.';
      return;
    }
    this.portal = this.router.url.startsWith('/portal');
    const origem: Observable<Recibo> = this.portal ? this.portalService.recibo(id) : this.api.obterRecibo(id);
    origem.subscribe({
      next: (r) => {
        this.recibo = r;
        this.carregando = false;
      },
      error: (err) => {
        this.carregando = false;
        this.erro = err?.message || 'Não foi possível carregar o recibo.';
      }
    });
  }

  imprimir() {
    window.print();
  }
}
