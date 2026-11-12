import { Component, Input, OnInit } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Cid, Consulta, EvolucaoClinica, Medico, Paciente, SinaisVitais } from '../types/types';
import { mensagemErro, possuiObrigatoriosPendentes } from '../shared/utils/validadores';
import { calcularImc, classificarImc, resumirSinaisVitais } from '../shared/utils/formatadores';

const CAMPOS_EVOLUCAO = ['observacao', 'subjetivo', 'objetivo', 'avaliacao', 'plano'];

/** Exige ao menos um campo da evolução preenchido (texto livre ou qualquer parte do SOAP). */
const evolucaoObrigatoria: ValidatorFn = (grupo: AbstractControl): ValidationErrors | null => {
  const algum = CAMPOS_EVOLUCAO.some(c => `${grupo.get(c)?.value ?? ''}`.trim().length > 0);
  return algum ? null : { evolucaoVazia: true };
};

/** Pressão arterial só faz sentido com os dois valores, e a diastólica abaixo da sistólica. */
const pressaoCoerente: ValidatorFn = (grupo: AbstractControl): ValidationErrors | null => {
  const sis = grupo.get('pressaoSistolica')?.value;
  const dia = grupo.get('pressaoDiastolica')?.value;
  const temSis = sis !== null && sis !== undefined && `${sis}` !== '';
  const temDia = dia !== null && dia !== undefined && `${dia}` !== '';
  if (temSis !== temDia) return { pressaoIncompleta: true };
  if (temSis && temDia && Number(dia) >= Number(sis)) return { pressaoInvertida: true };
  return null;
};

type CampoSinal = { nome: keyof SinaisVitais; rotulo: string; unidade: string; min: number; max: number; passo?: string };

@Component({
  selector: 'app-consulta-modal',
  templateUrl: './consulta-modal.component.html',
  styles: [`
    .toggle-rotulo { display: block; font-weight: 600; }
    .toggle-dica { display: block; font-size: 12px; color: var(--app-text-muted); }
    .dica-evolucao { margin: 0 4px 12px; font-size: 12px; color: var(--app-text-muted); }
    .dica-evolucao.erro { color: var(--ion-color-danger); }
  `],
  standalone: false
})
export class ConsultaModalComponent implements OnInit {
  @Input() consulta?: Consulta;
  /** Quando aberto pela fila do dia: horário, paciente e médico do atendimento. */
  @Input() idAgenda?: number;
  @Input() idPacienteInicial?: number;
  @Input() idMedicoInicial?: number;
  @Input() descricaoAtendimento?: string;

  public form!: FormGroup;
  public sinais!: FormGroup;
  public salvando = false;
  public medicos: Medico[] = [];
  public pacientes: Paciente[] = [];
  public finalizarAoSalvar = false;
  public cid: Cid | null = null;
  public mostrarSoap = false;
  public mostrarSinais = false;
  private pacienteCarregado: Paciente | null = null;

  readonly camposSinais: CampoSinal[] = [
    { nome: 'pressaoSistolica', rotulo: 'PA sistólica', unidade: 'mmHg', min: 40, max: 300 },
    { nome: 'pressaoDiastolica', rotulo: 'PA diastólica', unidade: 'mmHg', min: 20, max: 200 },
    { nome: 'frequenciaCardiaca', rotulo: 'Freq. cardíaca', unidade: 'bpm', min: 20, max: 300 },
    { nome: 'frequenciaRespiratoria', rotulo: 'Freq. respiratória', unidade: 'irpm', min: 4, max: 80 },
    { nome: 'temperatura', rotulo: 'Temperatura', unidade: '°C', min: 30, max: 45, passo: '0.1' },
    { nome: 'saturacaoO2', rotulo: 'Saturação O₂', unidade: '%', min: 40, max: 100 },
    { nome: 'pesoKg', rotulo: 'Peso', unidade: 'kg', min: 0.3, max: 400, passo: '0.1' },
    { nome: 'alturaCm', rotulo: 'Altura', unidade: 'cm', min: 25, max: 250 },
    { nome: 'glicemia', rotulo: 'Glicemia', unidade: 'mg/dL', min: 10, max: 1500 }
  ];

  constructor(
    private modalCtrl: ModalController,
    private api: ConsultorioApiService,
    private feedback: FeedbackService
  ) {}

  ngOnInit() {
    const c = this.consulta;
    const sv = c?.sinaisVitais ?? null;

    const controlesSinais: Record<string, FormControl> = {};
    for (const campo of this.camposSinais) {
      controlesSinais[campo.nome] = new FormControl(sv?.[campo.nome] ?? null, [Validators.min(campo.min), Validators.max(campo.max)]);
    }
    this.sinais = new FormGroup(controlesSinais, { validators: pressaoCoerente });

    this.form = new FormGroup({
      idPaciente: new FormControl(c?.idPaciente || this.idPacienteInicial || null, c ? [] : [Validators.required]),
      idMedico: new FormControl(c?.idMedico || this.idMedicoInicial || null, c ? [] : [Validators.required]),
      observacao: new FormControl(c?.observacao || '', [Validators.maxLength(2000)]),
      subjetivo: new FormControl(c?.subjetivo || '', [Validators.maxLength(4000)]),
      objetivo: new FormControl(c?.objetivo || '', [Validators.maxLength(4000)]),
      avaliacao: new FormControl(c?.avaliacao || '', [Validators.maxLength(4000)]),
      plano: new FormControl(c?.plano || '', [Validators.maxLength(4000)]),
      sinais: this.sinais
    }, { validators: evolucaoObrigatoria });

    this.cid = c?.cidCodigo ? { codigo: c.cidCodigo, descricao: c.cidDescricao ?? '' } : null;
    this.mostrarSoap = !!(c?.subjetivo || c?.objetivo || c?.avaliacao || c?.plano);
    this.mostrarSinais = !!sv;

    this.carregarDados();
  }

  carregarDados() {
    if (this.consulta) {
      this.api.obterPacientePorId(this.consulta.idPaciente).subscribe({
        next: (p) => this.pacienteCarregado = p,
        error: () => undefined
      });
      return;
    }

    const porNome = <T extends { nome: string }>(lista: T[]) =>
      [...(lista || [])].sort((a, b) => (a.nome ?? '').localeCompare(b.nome ?? '', 'pt-BR'));

    this.api.obterMedicos().subscribe({
      next: (m) => this.medicos = porNome(m),
      error: (err) => this.feedback.erro(err, 'Não foi possível carregar os médicos.')
    });

    // Quando quem está logado é médico, ele já vem selecionado como responsável.
    this.api.obterMeuMedico().subscribe({
      next: (meu) => {
        const campo = this.form.get('idMedico');
        if (meu && meu.ativo !== false && campo && !campo.value) {
          campo.setValue(meu.id);
        }
      },
      error: () => undefined
    });

    this.api.obterPacientes().subscribe({
      next: (p) => this.pacientes = porNome(p),
      error: (err) => this.feedback.erro(err, 'Não foi possível carregar os pacientes.')
    });
  }

  /** Paciente do atendimento, para exibir alergias e condições antes de escrever a conduta. */
  get pacienteAtual(): Paciente | null {
    const id = this.form?.get('idPaciente')?.value;
    return this.pacientes.find(p => p.id === id) ?? this.pacienteCarregado;
  }

  get imc(): number | null {
    return calcularImc(this.sinais.get('pesoKg')?.value, this.sinais.get('alturaCm')?.value);
  }

  get classificacaoImc(): string {
    return classificarImc(this.imc);
  }

  get resumoSinais(): string {
    const itens = resumirSinaisVitais(this.valoresSinais() ?? undefined);
    return itens.length ? `${itens.length} registrado(s)` : 'opcional';
  }

  get resumoSoap(): string {
    const preenchidos = ['subjetivo', 'objetivo', 'avaliacao', 'plano'].filter(c => `${this.form.get(c)?.value ?? ''}`.trim()).length;
    return preenchidos ? `${preenchidos} de 4 preenchidos` : 'opcional';
  }

  get erroPressao(): string {
    if (this.sinais.hasError('pressaoIncompleta')) return 'Informe a pressão sistólica e a diastólica.';
    if (this.sinais.hasError('pressaoInvertida')) return 'A diastólica deve ser menor que a sistólica.';
    return '';
  }

  erro(campo: string): string {
    return mensagemErro(this.form.get(campo));
  }

  erroSinal(campo: CampoSinal): string {
    const controle = this.sinais.get(campo.nome);
    if (controle?.hasError('min') || controle?.hasError('max')) return `Entre ${campo.min} e ${campo.max} ${campo.unidade}.`;
    return mensagemErro(controle);
  }

  definirCid(cid: Cid | null) {
    this.cid = cid;
    this.form.markAsDirty();
  }

  bloquearEnvio(): boolean {
    return this.salvando || possuiObrigatoriosPendentes(this.form) || this.form.hasError('evolucaoVazia');
  }

  async fechar() {
    if (this.form.dirty && !this.salvando) {
      const descartar = await this.feedback.confirmar({
        titulo: 'Descartar alterações?',
        mensagem: 'O texto da consulta ainda não foi salvo.',
        textoConfirmar: 'Descartar',
        textoCancelar: 'Continuar editando',
        destrutivo: true
      });
      if (!descartar) return;
    }
    this.modalCtrl.dismiss(false);
  }

  private texto(campo: string): string | null {
    const valor = `${this.form.get(campo)?.value ?? ''}`.trim();
    return valor.length ? valor : null;
  }

  private valoresSinais(): SinaisVitais | null {
    const resultado: SinaisVitais = {};
    let algum = false;
    for (const campo of this.camposSinais) {
      const bruto = this.sinais.get(campo.nome)?.value;
      if (bruto === null || bruto === undefined || `${bruto}`.trim() === '') continue;
      const numero = Number(`${bruto}`.replace(',', '.'));
      if (isNaN(numero)) continue;
      resultado[campo.nome] = numero;
      algum = true;
    }
    return algum ? resultado : null;
  }

  private evolucao(): EvolucaoClinica {
    return {
      observacao: this.texto('observacao'),
      subjetivo: this.texto('subjetivo'),
      objetivo: this.texto('objetivo'),
      avaliacao: this.texto('avaliacao'),
      plano: this.texto('plano'),
      cidCodigo: this.cid?.codigo ?? null,
      cidDescricao: this.cid?.descricao ?? null,
      sinaisVitais: this.valoresSinais()
    };
  }

  salvar() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      if (this.sinais.invalid) this.mostrarSinais = true;
      if (['subjetivo', 'objetivo', 'avaliacao', 'plano'].some(c => this.form.get(c)?.invalid)) this.mostrarSoap = true;
      this.feedback.aviso(this.form.hasError('evolucaoVazia')
        ? 'Escreva a evolução (texto livre ou SOAP) antes de salvar.'
        : 'Revise os campos destacados antes de salvar.');
      return;
    }

    this.salvando = true;
    const evolucao = this.evolucao();

    const operacao = this.consulta
      ? this.api.atualizarEvolucaoConsulta(this.consulta.id, evolucao)
      : this.api.criarConsulta({
          ...evolucao,
          idPaciente: this.form.value.idPaciente,
          idMedico: this.form.value.idMedico,
          idAgenda: this.idAgenda ?? null
        });

    operacao.subscribe({
      next: (salva) => {
        if (!this.finalizarAoSalvar) {
          this.salvando = false;
          this.modalCtrl.dismiss(true);
          return;
        }
        this.api.finalizarConsulta(salva.id).subscribe({
          next: () => {
            this.salvando = false;
            this.modalCtrl.dismiss(true);
          },
          error: (err) => {
            // A consulta foi salva; só a finalização falhou. Fecha e avisa.
            this.salvando = false;
            this.feedback.erro(err, 'A consulta foi salva, mas não foi possível finalizá-la.');
            this.modalCtrl.dismiss(true);
          }
        });
      },
      error: (err) => {
        this.salvando = false;
        this.feedback.erro(err, 'Erro ao salvar consulta.');
      }
    });
  }
}
