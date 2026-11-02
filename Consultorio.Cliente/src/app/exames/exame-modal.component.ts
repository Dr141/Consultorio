import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { Exame, Paciente } from '../types/types';

@Component({
  selector: 'app-exame-modal',
  templateUrl: './exame-modal.component.html',
  standalone: false
})
export class ExameModalComponent implements OnInit {
  @Input() exame?: Exame;

  public form!: FormGroup;
  public salvando = false;
  public pacientes: Paciente[] = [];

  constructor(
    private modalCtrl: ModalController,
    private api: ConsultorioApiService,
    private toastCtrl: ToastController
  ) {}

  ngOnInit() {
    this.carregarPacientes();

    this.form = new FormGroup({
      idPaciente: new FormControl(this.exame?.idPaciente || null, this.exame ? [] : [Validators.required]),
      descricao: new FormControl(this.exame?.descricao || '', this.exame ? [] : [Validators.required, Validators.minLength(2)]),
      resultado: new FormControl(this.exame?.resultado || '')
    });
  }

  carregarPacientes() {
    if (!this.exame) {
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

    const operacao = this.exame
      ? this.api.registrarResultadoExame(this.exame.id, this.form.value.resultado)
      : this.api.criarExame({
          idPaciente: this.form.value.idPaciente,
          descricao: this.form.value.descricao,
          resultado: this.form.value.resultado || null
        });

    operacao.subscribe({
      next: () => {
        this.salvando = false;
        this.modalCtrl.dismiss(true);
      },
      error: async (err) => {
        this.salvando = false;
        const toast = await this.toastCtrl.create({
          message: err.message || 'Erro ao salvar exame.',
          duration: 3000,
          color: 'danger'
        });
        await toast.present();
      }
    });
  }
}
