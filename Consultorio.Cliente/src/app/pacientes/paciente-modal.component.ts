import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { Paciente } from '../types/types';

@Component({
  selector: 'app-paciente-modal',
  templateUrl: './paciente-modal.component.html',
  standalone: false
})
export class PacienteModalComponent implements OnInit {
  @Input() paciente?: Paciente;

  public form!: FormGroup;
  public salvando = false;

  constructor(
    private modalCtrl: ModalController,
    private api: ConsultorioApiService,
    private toastCtrl: ToastController
  ) {}

  ngOnInit() {
    const dataFormatada = this.paciente?.dtNasc
      ? new Date(this.paciente.dtNasc).toISOString().split('T')[0]
      : '';

    this.form = new FormGroup({
      nome: new FormControl(this.paciente?.nome || '', [Validators.required, Validators.minLength(2)]),
      cpf: new FormControl(this.paciente?.cpf || '', [Validators.required]),
      dtNasc: new FormControl(dataFormatada, [Validators.required]),
      telefone: new FormControl(this.paciente?.telefone || '', [Validators.required]),
      observacao: new FormControl(this.paciente?.observacao || '')
    });
  }

  fechar() {
    this.modalCtrl.dismiss(false);
  }

  salvar() {
    if (this.form.invalid) return;

    this.salvando = true;
    const dados = {
      ...this.form.value,
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
      error: async (err) => {
        this.salvando = false;
        const toast = await this.toastCtrl.create({
          message: err.message || 'Erro ao salvar paciente.',
          duration: 3000,
          color: 'danger'
        });
        await toast.present();
      }
    });
  }
}
