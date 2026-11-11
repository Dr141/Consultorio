import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Consulta, Medico, Paciente } from '../types/types';
import { mensagemErro, possuiObrigatoriosPendentes } from '../shared/utils/validadores';

@Component({
  selector: 'app-consulta-modal',
  templateUrl: './consulta-modal.component.html',
  standalone: false
})
export class ConsultaModalComponent implements OnInit {
  @Input() consulta?: Consulta;

  public form!: FormGroup;
  public salvando = false;
  public medicos: Medico[] = [];
  public pacientes: Paciente[] = [];

  constructor(
    private modalCtrl: ModalController,
    private api: ConsultorioApiService,
    private feedback: FeedbackService
  ) {}

  ngOnInit() {
    this.carregarDados();

    this.form = new FormGroup({
      idPaciente: new FormControl(this.consulta?.idPaciente || null, this.consulta ? [] : [Validators.required]),
      idMedico: new FormControl(this.consulta?.idMedico || null, this.consulta ? [] : [Validators.required]),
      observacao: new FormControl(this.consulta?.observacao || '', [Validators.required, Validators.minLength(2), Validators.maxLength(2000)])
    });
  }

  carregarDados() {
    if (this.consulta) return;
    const porNome = <T extends { nome: string }>(lista: T[]) =>
      [...(lista || [])].sort((a, b) => (a.nome ?? '').localeCompare(b.nome ?? '', 'pt-BR'));

    this.api.obterMedicos().subscribe({
      next: (m) => this.medicos = porNome(m),
      error: (err) => this.feedback.erro(err, 'Não foi possível carregar os médicos.')
    });

    this.api.obterPacientes().subscribe({
      next: (p) => this.pacientes = porNome(p),
      error: (err) => this.feedback.erro(err, 'Não foi possível carregar os pacientes.')
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
        mensagem: 'O texto da consulta ainda não foi salvo.',
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
    const observacao = (this.form.value.observacao ?? '').trim();

    const operacao = this.consulta
      ? this.api.atualizarObservacaoConsulta(this.consulta.id, observacao)
      : this.api.criarConsulta({
          idPaciente: this.form.value.idPaciente,
          idMedico: this.form.value.idMedico,
          observacao
        });

    operacao.subscribe({
      next: () => {
        this.salvando = false;
        this.modalCtrl.dismiss(true);
      },
      error: (err) => {
        this.salvando = false;
        this.feedback.erro(err, 'Erro ao salvar consulta.');
      }
    });
  }
}
