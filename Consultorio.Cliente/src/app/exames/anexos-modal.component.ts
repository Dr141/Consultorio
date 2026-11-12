import { Component, ElementRef, Input, OnInit, ViewChild } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Exame, ExameAnexo } from '../types/types';

const TAMANHO_MAXIMO = 10 * 1024 * 1024;
const TIPOS_ACEITOS = ['application/pdf', 'image/jpeg', 'image/png'];

/** Arquivos do resultado do exame (PDF do laboratório, imagens). */
@Component({
  selector: 'app-anexos-modal',
  template: `
    <ion-header>
      <ion-toolbar color="primary">
        <ion-title>Arquivos do exame</ion-title>
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
        <h3><strong>{{ exame.descricao }}</strong></h3>
        <p>{{ exame.nomePaciente }}</p>
      </div>

      <input #seletor type="file" accept="application/pdf,image/jpeg,image/png" hidden (change)="arquivoEscolhido($event)" />

      @if (!exame.cancelado) {
        <button type="button" class="area-envio" [class.arrastando]="arrastando" [disabled]="enviando"
                (click)="seletor.click()"
                (dragover)="$event.preventDefault(); arrastando = true"
                (dragleave)="arrastando = false"
                (drop)="soltar($event)">
          @if (enviando) {
            <ion-spinner name="crescent"></ion-spinner>
            <span>Enviando {{ nomeEnviando }}...</span>
          } @else {
            <ion-icon name="cloud-upload-outline" aria-hidden="true"></ion-icon>
            <strong>Enviar arquivo</strong>
            <span>Clique ou arraste aqui · PDF, JPG ou PNG até 10 MB</span>
          }
        </button>
      }

      @if (carregando) {
        <ion-progress-bar type="indeterminate"></ion-progress-bar>
      } @else if (anexos.length === 0) {
        <p class="sem-anexos">Nenhum arquivo anexado ainda.</p>
      } @else {
        <ion-list lines="full">
          @for (a of anexos; track a.id) {
            <ion-item button detail="false" (click)="abrir(a)" [title]="'Abrir ' + a.nomeArquivo">
              <ion-icon slot="start" [name]="a.contentType === 'application/pdf' ? 'document-outline' : 'image-outline'" color="primary"></ion-icon>
              <ion-label>
                <h3>{{ a.nomeArquivo }}</h3>
                <p>{{ a.tamanhoBytes | tamanhoArquivo }} · {{ a.dtCriacao | dataBr:'dataHora' }} · {{ a.enviadoPor }}</p>
              </ion-label>
              <ion-icon slot="end" name="open-outline" color="medium"></ion-icon>
            </ion-item>
          }
        </ion-list>
      }
    </ion-content>
  `,
  styles: [`
    .area-envio { display: flex; flex-direction: column; align-items: center; gap: 4px; width: 100%; margin: 0 0 16px;
      padding: 22px 16px; border-radius: 14px; border: 2px dashed var(--app-border); background: transparent;
      color: var(--app-text); font: inherit; cursor: pointer; }
    .area-envio ion-icon { font-size: 34px; color: var(--ion-color-primary); }
    .area-envio span { font-size: 13px; color: var(--app-text-muted); }
    .area-envio.arrastando, .area-envio:hover { border-color: var(--ion-color-primary); background: rgba(var(--ion-color-primary-rgb), 0.06); }
    .sem-anexos { color: var(--app-text-muted); text-align: center; }
  `],
  standalone: false
})
export class AnexosModalComponent implements OnInit {
  @Input({ required: true }) exame!: Exame;
  @ViewChild('seletor') seletor?: ElementRef<HTMLInputElement>;

  anexos: ExameAnexo[] = [];
  carregando = false;
  enviando = false;
  arrastando = false;
  nomeEnviando = '';
  /** Indica à tela de exames que a lista deve ser recarregada (status e contagem mudam). */
  private alterou = false;

  constructor(
    private modalCtrl: ModalController,
    private api: ConsultorioApiService,
    private feedback: FeedbackService
  ) {}

  ngOnInit() {
    this.carregar();
  }

  carregar() {
    this.carregando = true;
    this.api.obterAnexosExame(this.exame.id).subscribe({
      next: (lista) => {
        this.anexos = lista;
        this.carregando = false;
      },
      error: (err) => {
        this.carregando = false;
        this.feedback.erro(err, 'Não foi possível carregar os arquivos.');
      }
    });
  }

  arquivoEscolhido(evento: Event) {
    const input = evento.target as HTMLInputElement;
    const arquivo = input.files?.[0];
    input.value = '';
    if (arquivo) this.enviar(arquivo);
  }

  soltar(evento: DragEvent) {
    evento.preventDefault();
    this.arrastando = false;
    const arquivo = evento.dataTransfer?.files?.[0];
    if (arquivo) this.enviar(arquivo);
  }

  private enviar(arquivo: File) {
    if (arquivo.size > TAMANHO_MAXIMO) {
      this.feedback.aviso('O arquivo ultrapassa o limite de 10 MB.');
      return;
    }
    if (arquivo.type && !TIPOS_ACEITOS.includes(arquivo.type)) {
      this.feedback.aviso('Formato não suportado. Envie PDF, JPG ou PNG.');
      return;
    }
    this.enviando = true;
    this.nomeEnviando = arquivo.name;
    this.api.enviarAnexoExame(this.exame.id, arquivo).subscribe({
      next: () => {
        this.enviando = false;
        this.alterou = true;
        this.feedback.sucesso('Arquivo anexado.');
        this.carregar();
      },
      error: (err) => {
        this.enviando = false;
        this.feedback.erro(err, 'Não foi possível enviar o arquivo.');
      }
    });
  }

  abrir(anexo: ExameAnexo) {
    // A janela é aberta já no clique para não ser barrada pelo bloqueador de pop-ups.
    const janela = window.open('', '_blank');
    this.api.baixarAnexoExame(this.exame.id, anexo.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        if (janela) {
          janela.location.href = url;
        } else {
          const link = document.createElement('a');
          link.href = url;
          link.download = anexo.nomeArquivo;
          link.click();
        }
        setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: (err) => {
        janela?.close();
        this.feedback.erro(err, 'Não foi possível abrir o arquivo.');
      }
    });
  }

  fechar() {
    this.modalCtrl.dismiss(this.alterou);
  }
}
