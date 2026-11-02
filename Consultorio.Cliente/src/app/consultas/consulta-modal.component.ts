import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { Consulta, Medico, Paciente } from '../types/types';

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
    private toastCtrl: ToastController
  ) {}

  ngOnInit() {
    this.carregarDados();

    this.form = new FormGroup({
      idPaciente: new FormControl(this.consulta?.idPaciente || null, this.consulta ? [] : [Validators.required]),
      idMedico: new FormControl(this.consulta?.idMedico || null, this.consulta ? [] : [Validators.required]),
      observacao: new FormControl(this.consulta?.observacao || '', [Validators.required, Validators.minLength(2)])
    });
  }

  carregarDados() {
    if (!this.consulta) {
      this.api.obterMedicos().subscribe({
        next: (m) => this.medicos = m,
        error: () => {}
      });

      this.api.obterPacientes().subscribe({
        next: (p) => this.pacientes = p,
        error: () => {}
      });
    }
  }

  fechar() {
    this.modalCtrl.dismiss(false);
  }

  salvar() {
    if (this.form.invalid) return;

    this.salvando = true;

    const operacao = this.consulta
      ? this.api.atualizarObservacaoConsulta(this.consulta.id, this.form.value.observacao)
      : this.api.criarConsulta({
          idPaciente: this.form.value.idPaciente,
          idMedico: this.form.value.idMedico,
          observacao: this.form.value.observacao
        });

    operacao.subscribe({
      next: () => {
        this.salvando = false;
        this.modalCtrl.dismiss(true);
      },
      error: async (err) => {
        this.salvando = false;
        const toast = await this.toastCtrl.create({
          message: err.message || 'Erro ao salvar consulta.',
          duration: 3000,
          color: 'danger'
        });
        await toast.present();
      }
    });
  }
}
