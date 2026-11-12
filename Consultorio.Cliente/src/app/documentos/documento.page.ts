import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { PortalService } from '../services/portal/portal.service';
import { NavController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { DocumentoClinico } from '../types/types';
import { linhas } from '../shared/utils/formatadores';

/** Visualização para impressão (ou "Salvar como PDF") de um documento clínico emitido. */
@Component({
  selector: 'app-documento',
  templateUrl: './documento.page.html',
  styleUrls: ['./documento.page.scss'],
  standalone: false
})
export class DocumentoPage implements OnInit {
  documento: DocumentoClinico | null = null;
  carregando = true;
  erroCarregamento = '';
  /** Aberto pelo portal do paciente: só leitura e impressão. */
  portal = false;
  baixando = false;
  assinando = false;

  constructor(
    private rota: ActivatedRoute,
    private api: ConsultorioApiService,
    private feedback: FeedbackService,
    private nav: NavController,
    private router: Router,
    private portalService: PortalService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.portal = this.router.url.startsWith('/portal');
    const id = Number(this.rota.snapshot.paramMap.get('id'));
    if (!id) {
      this.carregando = false;
      this.erroCarregamento = 'Documento inválido.';
      return;
    }
    this.carregar(id);
  }

  carregar(id: number) {
    this.carregando = true;
    const origem: Observable<DocumentoClinico> = this.portal ? this.portalService.documento(id) : this.api.obterDocumento(id);
    origem.subscribe({
      next: (d) => {
        this.documento = d;
        this.carregando = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.carregando = false;
        this.erroCarregamento = err?.message || 'Não foi possível carregar o documento.';
        this.cdr.detectChanges();
      }
    });
  }

  /** Receita de controle especial sai em duas vias (farmácia e paciente). */
  get vias(): string[] {
    return this.documento?.tipo === 'ReceitaControleEspecial' ? ['1ª via — Farmácia', '2ª via — Paciente'] : [''];
  }

  get itensPedido(): string[] {
    return linhas(this.documento?.conteudo);
  }

  get ehReceita(): boolean {
    return this.documento?.tipo === 'Receita' || this.documento?.tipo === 'ReceitaControleEspecial';
  }

  imprimir() {
    window.print();
  }

  /** Baixa o PDF: a via assinada digitalmente, se o documento já foi assinado. */
  baixarPdf() {
    const doc = this.documento;
    if (!doc || this.baixando) return;
    this.baixando = true;
    const origem = this.portal ? this.portalService.pdfDocumento(doc.id) : this.api.baixarPdfDocumento(doc.id);
    origem.subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `${doc.tipo.toLowerCase()}-${doc.codigoVerificacao}${doc.assinado && !doc.cancelado ? '-assinado' : ''}.pdf`;
        link.click();
        setTimeout(() => URL.revokeObjectURL(url), 60_000);
        this.baixando = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.baixando = false;
        this.cdr.detectChanges();
        this.feedback.erro(err, 'Não foi possível gerar o PDF.');
      }
    });
  }

  /** Assina com o certificado A1 (ICP-Brasil) do médico; depois disso o documento não pode ser alterado. */
  async assinar() {
    const doc = this.documento;
    if (!doc || this.assinando) return;
    const confirmado = await this.feedback.confirmar({
      titulo: 'Assinar digitalmente',
      mensagem: `O documento será assinado com o certificado digital de ${doc.nomeMedico}. ` +
        'O PDF assinado tem validade jurídica (MP 2.200-2/2001) e pode ser enviado ao paciente ou à farmácia.',
      textoConfirmar: 'Assinar'
    });
    if (!confirmado) return;

    this.assinando = true;
    this.cdr.detectChanges();
    this.api.assinarDocumento(doc.id).subscribe({
      next: (atualizado) => {
        this.documento = atualizado;
        this.assinando = false;
        this.cdr.detectChanges();
        this.feedback.sucesso('Documento assinado digitalmente. Use "Baixar PDF" para obter a via assinada.');
      },
      error: (err) => {
        this.assinando = false;
        this.cdr.detectChanges();
        this.feedback.erro(err, 'Não foi possível assinar o documento.');
      }
    });
  }

  voltar() {
    this.nav.back();
  }

  async cancelar() {
    const doc = this.documento;
    if (!doc) return;
    const motivo = await this.feedback.solicitarTexto({
      titulo: 'Cancelar documento',
      mensagem: 'O documento continua no histórico e a verificação pública passa a indicar que foi cancelado.',
      placeholder: 'Motivo do cancelamento',
      textoConfirmar: 'Cancelar documento',
      destrutivo: true
    });
    if (!motivo) return;
    this.api.cancelarDocumento(doc.id, motivo).subscribe({
      next: (atualizado) => {
        this.documento = atualizado;
        this.feedback.sucesso('Documento cancelado.');
      },
      error: (err) => this.feedback.erro(err, 'Não foi possível cancelar o documento.')
    });
  }
}
