import { Component, OnInit } from '@angular/core';
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

  constructor(
    private rota: ActivatedRoute,
    private api: ConsultorioApiService,
    private feedback: FeedbackService,
    private nav: NavController,
    private router: Router,
    private portalService: PortalService
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
      },
      error: (err) => {
        this.carregando = false;
        this.erroCarregamento = err?.message || 'Não foi possível carregar o documento.';
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
