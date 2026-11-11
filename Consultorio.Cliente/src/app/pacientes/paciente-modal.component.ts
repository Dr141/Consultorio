import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Paciente } from '../types/types';
import { formatarCpf, formatarTelefone, hojeInputData, paraInputData, somenteDigitos } from '../shared/utils/formatadores';
import { cpfValidator, dataNaoFuturaValidator, mensagemErro, possuiObrigatoriosPendentes, telefoneValidator } from '../shared/utils/validadores';

@Component({
  selector: 'app-paciente-modal',
  templateUrl: './paciente-modal.component.html',
  standalone: false
})
export class PacienteModalComponent implements OnInit {
  @Input() paciente?: Paciente;

  public form!: FormGroup;
  public salvando = false;
  public readonly hoje = hojeInputData();

  constructor(
    private modalCtrl: ModalController,
    private api: ConsultorioApiService,
    private feedback: FeedbackService
  ) {}

  ngOnInit() {
    this.form = new FormGroup({
      nome: new FormControl(this.paciente?.nome || '', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]),
      cpf: new FormControl(formatarCpf(this.paciente?.cpf), [Validators.required, cpfValidator()]),
      dtNasc: new FormControl(paraInputData(this.paciente?.dtNasc), [Validators.required, dataNaoFuturaValidator()]),
      telefone: new FormControl(formatarTelefone(this.paciente?.telefone), [Validators.required, telefoneValidator()]),
      observacao: new FormControl(this.paciente?.observacao || '', [Validators.maxLength(500)]),
      email: new FormControl(this.paciente?.email || '', [Validators.email, Validators.maxLength(256)])
    });
  }

  erro(campo: string): string {
    return mensagemErro(this.form.get(campo));
  }

  bloquearEnvio(): boolean {
    return this.salvando || possuiObrigatoriosPendentes(this.form);
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
      cpf: somenteDigitos(valores.cpf),
      telefone: somenteDigitos(valores.telefone),
      email: valores.email?.trim() || null,
      idPessoa: this.paciente?.idPessoa
    };

    const operacao = this.paciente
      ? this.api.atualizarPaciente(this.paciente.id, dados)
      : this.api.criarPaciente(dados);

    operacao.subscribe({
      next: () => {
        this.salvando = false;
        this.modalCtrl.dismiss(true);
      },
      error: (err) => {
        this.salvando = false;
        this.feedback.erro(err, 'Erro ao salvar paciente.');
      }
    });
  }
}
