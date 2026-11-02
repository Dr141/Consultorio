import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { Agenda, Medico, Paciente } from '../types/types';

@Component({
  selector: 'app-agenda-modal',
  templateUrl: './agenda-modal.component.html',
  standalone: false
})
export class AgendaModalComponent implements OnInit {
  @Input() agenda?: Agenda;

  public form!: FormGroup;
  public salvando = false;
  public medicos: Medico[] = [];
  public pacientes: Paciente[] = [];

  constructor(
    private modalCtrl: ModalController,
    private api: ConsultorioApiService,
    private toastCtrl: ToastController
  ) {}

  ngOnInit() {
    this.carregarDados();

    const dataFormatada = this.agenda?.data
      ? new Date(this.agenda.data).toISOString().split('T')[0]
      : new Date().toISOString().split('T')[0];

    const horaFormatada = this.agenda?.hora
      ? this.agenda.hora.substring(0, 5)
      : '08:00';

    this.form = new FormGroup({
      data: new FormControl(dataFormatada, [Validators.required]),
      hora: new FormControl(horaFormatada, [Validators.required]),
      idMedico: new FormControl(this.agenda?.idMedico || null, [Validators.required]),
      idPaciente: new FormControl(this.agenda?.idPaciente || null)
    });
  }

  carregarDados() {
    this.api.obterMedicos().subscribe({
      next: (m) => this.medicos = m,
      error: () => {}
    });

    this.api.obterPacientes().subscribe({
      next: (p) => this.pacientes = p,
      error: () => {}
    });
  }

  fechar() {
    this.modalCtrl.dismiss(false);
  }

  salvar() {
    if (this.form.invalid) return;

    this.salvando = true;
    const horaVal = this.form.value.hora.length === 5
      ? `${this.form.value.hora}:00`
      : this.form.value.hora;

    const dados = {
      data: this.form.value.data,
      hora: horaVal,
      idMedico: this.form.value.idMedico,
      idPaciente: this.form.value.idPaciente || null
    };

    const operacao = this.agenda
      ? this.api.atualizarAgenda(this.agenda.id, dados)
      : this.api.criarAgenda(dados);

    operacao.subscribe({
      next: () => {
        this.salvando = false;
        this.modalCtrl.dismiss(true);
      },
      error: async (err) => {
        this.salvando = false;
        const toast = await this.toastCtrl.create({
          message: err.message || 'Erro ao salvar horário na agenda.',
          duration: 3000,
          color: 'danger'
        });
        await toast.present();
      }
    });
  }
}
