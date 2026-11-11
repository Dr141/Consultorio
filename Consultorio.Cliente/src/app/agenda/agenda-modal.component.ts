import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Agenda, Medico, Paciente } from '../types/types';
import { hojeInputData, paraInputData } from '../shared/utils/formatadores';
import { mensagemErro, possuiObrigatoriosPendentes } from '../shared/utils/validadores';
import { SelecionarPacienteComponent } from '../shared/selecionar-paciente/selecionar-paciente.component';

@Component({
  selector: 'app-agenda-modal',
  templateUrl: './agenda-modal.component.html',
  styles: [`
    .paciente-escolhido { --padding-top: 6px; --padding-bottom: 6px; }
    .paciente-escolhido h3 { font-weight: 600; }
  `],
  standalone: false
})
export class AgendaModalComponent implements OnInit {
  @Input() agenda?: Agenda;

  public form!: FormGroup;
  public salvando = false;
  public carregandoListas = true;
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
      // Datas tratadas no fuso local: antes, após as 21h o "hoje" padrão virava o dia seguinte (UTC).
      data: new FormControl(this.agenda?.data ? paraInputData(this.agenda.data) : hojeInputData(), [Validators.required]),
      hora: new FormControl(this.agenda?.hora ? this.agenda.hora.substring(0, 5) : '08:00', [Validators.required]),
      idMedico: new FormControl(this.agenda?.idMedico || null, [Validators.required]),
      idPaciente: new FormControl(this.agenda?.idPaciente || null)
    });
  }

  get pacienteSelecionado(): Paciente | undefined {
    const id = this.form?.get('idPaciente')?.value;
    if (!id) return undefined;
    const encontrado = this.pacientes.find(p => p.id === id);
    if (encontrado) return encontrado;
    // Enquanto a lista carrega, mostra o paciente que já estava no horário.
    const agenda = this.agenda;
    if (agenda && agenda.idPaciente === id) {
      return { id, idPessoa: 0, nome: agenda.nomePaciente ?? 'Paciente', cpf: '', dtNasc: '', telefone: '' };
    }
    return undefined;
  }

  carregarDados() {
    let pendentes = 2;
    const concluir = () => { if (--pendentes === 0) this.carregandoListas = false; };

    this.api.obterMedicos().subscribe({
      next: (m) => this.medicos = [...(m || [])].sort((a, b) => (a.nome ?? '').localeCompare(b.nome ?? '', 'pt-BR')),
      error: (err) => { this.feedback.erro(err, 'Não foi possível carregar os médicos.'); concluir(); },
      complete: concluir
    });

    this.api.obterPacientes().subscribe({
      next: (p) => this.pacientes = p || [],
      error: () => concluir(),
      complete: concluir
    });
  }

  async escolherPaciente() {
    const modal = await this.modalCtrl.create({
      component: SelecionarPacienteComponent,
      componentProps: { pacientes: this.pacientes, titulo: 'Escolher paciente' }
    });
    await modal.present();
    const { data, role } = await modal.onDidDismiss<number>();
    if (role === 'selecionado' && data) {
      this.form.get('idPaciente')?.setValue(data);
      this.form.markAsDirty();
    }
  }

  removerPaciente() {
    this.form.get('idPaciente')?.setValue(null);
    this.form.markAsDirty();
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
        mensagem: 'O horário ainda não foi salvo.',
        textoConfirmar: 'Descartar',
        textoCancelar: 'Continuar editando',
        destrutivo: true
      });
      if (!descartar) return;
    }
    this.modalCtrl.dismiss(false, undefined, 'agenda-modal');
  }

  salvar() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.feedback.aviso('Revise os campos destacados antes de salvar.');
      return;
    }

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
        this.modalCtrl.dismiss(true, undefined, 'agenda-modal');
      },
      error: (err) => {
        this.salvando = false;
        this.feedback.erro(err, 'Erro ao salvar horário na agenda.');
      }
    });
  }
}
