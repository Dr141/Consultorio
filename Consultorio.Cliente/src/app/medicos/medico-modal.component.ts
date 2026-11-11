import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Medico } from '../types/types';
import { formatarCpf, formatarTelefone, hojeInputData, paraInputData, somenteDigitos } from '../shared/utils/formatadores';
import { cpfValidator, dataNaoFuturaValidator, mensagemErro, possuiObrigatoriosPendentes, telefoneValidator } from '../shared/utils/validadores';
import { NOMES_ESPECIALIDADES } from '../shared/utils/especialidades';

@Component({
  selector: 'app-medico-modal',
  templateUrl: './medico-modal.component.html',
  standalone: false
})
export class MedicoModalComponent implements OnInit {
  @Input() medico?: Medico;

  public form!: FormGroup;
  public salvando = false;
  public readonly hoje = hojeInputData();
  public listaEspecialidades: { id: number; nome: string }[] = Object.entries(NOMES_ESPECIALIDADES)
    .map(([id, nome]) => ({ id: Number(id), nome }));

  constructor(
    private modalCtrl: ModalController,
    private api: ConsultorioApiService,
    private feedback: FeedbackService
  ) {}

  ngOnInit() {
    this.carregarEspecialidades();

    this.form = new FormGroup({
      nome: new FormControl(this.medico?.nome || '', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]),
      cpf: new FormControl(formatarCpf(this.medico?.cpf), [Validators.required, cpfValidator()]),
      crm: new FormControl(this.medico?.crm || '', [Validators.required, Validators.minLength(2), Validators.maxLength(20)]),
      dtNasc: new FormControl(paraInputData(this.medico?.dtNasc), [Validators.required, dataNaoFuturaValidator()]),
      telefone: new FormControl(formatarTelefone(this.medico?.telefone), [Validators.required, telefoneValidator()]),
      especialidades: new FormControl(this.medico?.especialidades || [], [Validators.required]),
      observacao: new FormControl(this.medico?.observacao || '', [Validators.maxLength(500)]),
      emailUsuario: new FormControl(this.medico?.emailUsuario || '', [Validators.email, Validators.maxLength(256)])
    });
  }

  /** A lista local já está pronta; a da API (nomes oficiais) substitui se responder. */
  carregarEspecialidades() {
    this.api.obterEspecialidades().subscribe({
      next: (dados) => {
        if (dados?.length) this.listaEspecialidades = dados;
      },
      error: () => undefined
    });
  }

  erro(campo: string): string {
    return mensagemErro(this.form.get(campo));
  }

  bloquearEnvio(): boolean {
    const especialidades = this.form.get('especialidades')?.value;
    return this.salvando || possuiObrigatoriosPendentes(this.form) || !especialidades?.length;
  }

  async fechar() {
    if (this.form.dirty && !this.salvando) {
      const descartar = await this.feedback.confirmar({
        titulo: 'Descartar alterações?',
        mensagem: 'Os dados preenchidos ainda não foram salvos.',
        textoConfirmar: 'Descartar',
        textoCancelar: 'Continuar editando',
        destrutivo: true
      });
      if (!descartar) return;
    }
    this.modalCtrl.dismiss(false);
  }

  salvar() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.feedback.aviso('Revise os campos destacados antes de salvar.');
      return;
    }

    this.salvando = true;
    const valores = this.form.value;
    const dados = {
      ...valores,
      nome: valores.nome?.trim(),
      crm: valores.crm?.trim().toUpperCase(),
      emailUsuario: valores.emailUsuario?.trim() || null,
      cpf: somenteDigitos(valores.cpf),
      telefone: somenteDigitos(valores.telefone),
      idPessoa: this.medico?.idPessoa
    };

    const operacao = this.medico
      ? this.api.atualizarMedico(this.medico.id, dados)
      : this.api.criarMedico(dados);

    operacao.subscribe({
      next: () => {
        this.salvando = false;
        this.modalCtrl.dismiss(true);
      },
      error: (err) => {
        this.salvando = false;
        this.feedback.erro(err, 'Erro ao salvar médico.');
      }
    });
  }
}
