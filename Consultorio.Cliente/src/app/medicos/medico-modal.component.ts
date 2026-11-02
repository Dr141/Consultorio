import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ModalController, ToastController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { Medico } from '../types/types';

@Component({
  selector: 'app-medico-modal',
  templateUrl: './medico-modal.component.html',
  standalone: false
})
export class MedicoModalComponent implements OnInit {
  @Input() medico?: Medico;

  public form!: FormGroup;
  public salvando = false;
  public listaEspecialidades: { id: number; nome: string }[] = [];

  constructor(
    private modalCtrl: ModalController,
    private api: ConsultorioApiService,
    private toastCtrl: ToastController
  ) {}

  ngOnInit() {
    this.carregarEspecialidades();

    const dataFormatada = this.medico?.dtNasc
      ? new Date(this.medico.dtNasc).toISOString().split('T')[0]
      : '';

    this.form = new FormGroup({
      nome: new FormControl(this.medico?.nome || '', [Validators.required, Validators.minLength(2)]),
      cpf: new FormControl(this.medico?.cpf || '', [Validators.required]),
      crm: new FormControl(this.medico?.crm || '', [Validators.required]),
      dtNasc: new FormControl(dataFormatada, [Validators.required]),
      telefone: new FormControl(this.medico?.telefone || '', [Validators.required]),
      especialidades: new FormControl(this.medico?.especialidades || [], [Validators.required]),
      observacao: new FormControl(this.medico?.observacao || '')
    });
  }

  carregarEspecialidades() {
    this.api.obterEspecialidades().subscribe({
      next: (dados) => {
        this.listaEspecialidades = dados;
      },
      error: () => {
        // Fallback para especialidades padrão
        this.listaEspecialidades = [
          { id: 1, nome: 'ClinicoGeral' },
          { id: 2, nome: 'Cardiologista' },
          { id: 3, nome: 'Urologista' },
          { id: 4, nome: 'Ginecologista' },
          { id: 5, nome: 'Ortopedista' },
          { id: 6, nome: 'Pediatra' },
          { id: 7, nome: 'Dermatologista' }
        ];
      }
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
      error: async (err) => {
        this.salvando = false;
        const toast = await this.toastCtrl.create({
          message: err.message || 'Erro ao salvar médico.',
          duration: 3000,
          color: 'danger'
        });
        await toast.present();
      }
    });
  }
}
