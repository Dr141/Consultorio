import { ChangeDetectorRef, Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Cid, DocumentoClinico, Medico, Paciente, TIPOS_DOCUMENTO, TipoDocumento } from '../types/types';
import { mensagemErro, possuiObrigatoriosPendentes } from '../shared/utils/validadores';
import { dicaConteudo, modeloDocumento } from './modelos-documento';

/**
 * Emissão de receitas, atestados, declarações, pedidos de exames, encaminhamentos e relatórios,
 * com o histórico de documentos do paciente na segunda aba.
 */
@Component({
  selector: 'app-documento-modal',
  templateUrl: './documento-modal.component.html',
  styles: [`
    .tipos-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 8px; margin-bottom: 16px; }
    .tipo-opcao { display: flex; align-items: center; gap: 8px; padding: 10px 12px; border-radius: 12px; cursor: pointer;
      border: 1px solid var(--app-border); background: var(--app-surface); color: var(--app-text); font: inherit; text-align: left; }
    .tipo-opcao ion-icon { font-size: 20px; color: var(--ion-color-primary); flex-shrink: 0; }
    .tipo-opcao.ativo { border-color: var(--ion-color-primary); background: rgba(var(--ion-color-primary-rgb), 0.1); font-weight: 600; }
    .dica { margin: -4px 4px 12px; font-size: 12px; color: var(--app-text-muted); }
    .linha-modelo { display: flex; justify-content: flex-end; margin: -6px 0 6px; }
    .toggle-rotulo { display: block; font-weight: 600; }
    .toggle-dica { display: block; font-size: 12px; color: var(--app-text-muted); white-space: normal; }
    .doc-item.cancelado ion-label h3 { text-decoration: line-through; }
  `],
  standalone: false
})
export class DocumentoModalComponent implements OnInit {
  @Input({ required: true }) idPaciente!: number;
  @Input() nomePaciente?: string;
  @Input() idConsulta?: number | null;
  @Input() idMedico?: number | null;
  /** Diagnóstico da consulta, sugerido quando o médico decide incluir o CID. */
  @Input() cidSugerido?: Cid | null;
  @Input() tipoInicial?: TipoDocumento;
  /** Abre direto na aba de documentos já emitidos. */
  @Input() abrirHistorico = false;

  readonly tipos = TIPOS_DOCUMENTO;
  aba: 'emitir' | 'historico' = 'emitir';
  form!: FormGroup;
  medicos: Medico[] = [];
  medicoLogado: Medico | null = null;
  paciente: Paciente | null = null;
  cid: Cid | null = null;
  documentos: DocumentoClinico[] = [];
  carregandoHistorico = false;
  salvando = false;

  constructor(
    private modalCtrl: ModalController,
    private api: ConsultorioApiService,
    private feedback: FeedbackService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    const tipo = this.tipoInicial ?? 'Receita';
    this.form = new FormGroup({
      tipo: new FormControl<TipoDocumento>(tipo, { nonNullable: true }),
      idMedico: new FormControl<number | null>(this.idMedico ?? null, [Validators.required]),
      conteudo: new FormControl('', [Validators.required, Validators.minLength(3), Validators.maxLength(8000)]),
      diasAfastamento: new FormControl<number | null>(1),
      incluirCid: new FormControl(false, { nonNullable: true }),
      criarExames: new FormControl(true, { nonNullable: true })
    });
    this.cid = this.cidSugerido ?? null;
    this.aplicarRegrasDoTipo(tipo);
    this.aba = this.abrirHistorico ? 'historico' : 'emitir';

    this.api.obterMeuMedico().subscribe({
      next: (meu) => {
        this.medicoLogado = meu && meu.ativo !== false ? meu : null;
        if (this.medicoLogado) {
          // O médico logado só emite em nome próprio (regra também validada no servidor).
          this.form.get('idMedico')?.setValue(this.medicoLogado.id);
        }
        this.cdr.detectChanges();
      },
      error: () => undefined
    });
    this.api.obterMedicos().subscribe({
      next: (m) => { this.medicos = [...(m || [])].sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR')); this.cdr.detectChanges(); },
      error: () => undefined
    });
    this.api.obterPacientePorId(this.idPaciente).subscribe({
      next: (p) => {
        this.paciente = p;
        this.nomePaciente = this.nomePaciente || p.nome;
        this.cdr.detectChanges();
      },
      error: () => undefined
    });
    this.carregarHistorico();
  }

  get tipoAtual(): TipoDocumento {
    return this.form.get('tipo')?.value as TipoDocumento;
  }

  get dica(): string {
    return dicaConteudo(this.tipoAtual);
  }

  get rotuloConteudo(): string {
    switch (this.tipoAtual) {
      case 'Receita':
      case 'ReceitaControleEspecial': return 'Prescrição *';
      case 'PedidoExames': return 'Exames solicitados *';
      default: return 'Texto do documento *';
    }
  }

  get quantidadeExames(): number {
    return `${this.form.get('conteudo')?.value ?? ''}`.split('\n').map(l => l.trim()).filter(l => l).length;
  }

  selecionarTipo(tipo: TipoDocumento) {
    if (tipo === this.tipoAtual) return;
    this.form.get('tipo')?.setValue(tipo);
    this.aplicarRegrasDoTipo(tipo);
    // Troca o texto pelo modelo do novo tipo só se o médico ainda não escreveu nada.
    const atual = `${this.form.get('conteudo')?.value ?? ''}`.trim();
    if (!atual) this.usarModelo();
  }

  private aplicarRegrasDoTipo(tipo: TipoDocumento) {
    const dias = this.form.get('diasAfastamento');
    if (tipo === 'Atestado') {
      dias?.setValidators([Validators.required, Validators.min(1), Validators.max(365)]);
      if (!dias?.value) dias?.setValue(1);
    } else {
      dias?.clearValidators();
    }
    dias?.updateValueAndValidity();
  }

  usarModelo() {
    const texto = modeloDocumento(this.tipoAtual, {
      nomePaciente: this.nomePaciente ?? this.paciente?.nome,
      dias: Number(this.form.get('diasAfastamento')?.value) || null
    });
    this.form.get('conteudo')?.setValue(texto);
    this.form.get('conteudo')?.markAsDirty();
  }

  definirCid(cid: Cid | null) {
    this.cid = cid;
  }

  erro(campo: string): string {
    return mensagemErro(this.form.get(campo));
  }

  bloquearEnvio(): boolean {
    return this.salvando || possuiObrigatoriosPendentes(this.form) || (this.form.value.incluirCid && !this.cid);
  }

  carregarHistorico() {
    this.carregandoHistorico = true;
    this.api.obterDocumentosPorPaciente(this.idPaciente).subscribe({
      next: (lista) => {
        this.documentos = lista;
        this.carregandoHistorico = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.carregandoHistorico = false;
        this.cdr.detectChanges();
      }
    });
  }

  async fechar() {
    if (this.form.dirty && !this.salvando && this.aba === 'emitir') {
      const descartar = await this.feedback.confirmar({
        titulo: 'Descartar documento?',
        mensagem: 'O documento ainda não foi emitido.',
        textoConfirmar: 'Descartar',
        textoCancelar: 'Continuar editando',
        destrutivo: true
      });
      if (!descartar) return;
    }
    this.modalCtrl.dismiss(null);
  }

  emitir() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.feedback.aviso('Revise os campos destacados antes de emitir.');
      return;
    }
    const tipo = this.tipos.find(t => t.tipo === this.tipoAtual)!;
    const incluirCid = !!this.form.value.incluirCid && !!this.cid;

    this.salvando = true;
    this.api.emitirDocumento({
      tipo: tipo.valor,
      idPaciente: this.idPaciente,
      idMedico: this.form.value.idMedico,
      idConsulta: this.idConsulta ?? null,
      conteudo: `${this.form.value.conteudo ?? ''}`.trim(),
      diasAfastamento: tipo.tipo === 'Atestado' ? Number(this.form.value.diasAfastamento) : null,
      incluirCid,
      cidCodigo: incluirCid ? this.cid!.codigo : null,
      cidDescricao: incluirCid ? this.cid!.descricao : null,
      criarExames: tipo.tipo === 'PedidoExames' ? !!this.form.value.criarExames : false
    }).subscribe({
      next: async (doc) => {
        this.salvando = false;
        const extra = doc.examesCriados ? ` ${doc.examesCriados} exame(s) adicionado(s) à lista de exames.` : '';
        this.feedback.sucesso(`${doc.tipoNome} emitido.${extra}`);
        await this.modalCtrl.dismiss(doc);
        this.router.navigate(['/documento', doc.id]);
      },
      error: (err) => {
        this.salvando = false;
        this.feedback.erro(err, 'Não foi possível emitir o documento.');
      }
    });
  }

  async abrir(doc: DocumentoClinico) {
    await this.modalCtrl.dismiss(null);
    this.router.navigate(['/documento', doc.id]);
  }

  async cancelar(doc: DocumentoClinico) {
    const motivo = await this.feedback.solicitarTexto({
      titulo: 'Cancelar documento',
      mensagem: 'O documento continua no histórico e a verificação pública passa a indicar que foi cancelado.',
      placeholder: 'Motivo do cancelamento',
      textoConfirmar: 'Cancelar documento',
      destrutivo: true
    });
    if (!motivo) return;
    this.api.cancelarDocumento(doc.id, motivo).subscribe({
      next: () => {
        this.feedback.sucesso('Documento cancelado.');
        this.carregarHistorico();
      },
      error: (err) => this.feedback.erro(err, 'Não foi possível cancelar o documento.')
    });
  }
}
