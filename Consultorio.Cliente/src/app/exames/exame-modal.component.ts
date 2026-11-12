import { ChangeDetectorRef, Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Exame, Medico, Paciente } from '../types/types';
import { mensagemErro, possuiObrigatoriosPendentes } from '../shared/utils/validadores';

@Component({
  selector: 'app-exame-modal',
  templateUrl: './exame-modal.component.html',
  styles: [`
    .sugestoes-exame { display: flex; flex-wrap: wrap; gap: 4px; margin: -4px 0 12px; }
    .sugestoes-exame ion-chip { margin: 0; font-size: 13px; }
  `],
  standalone: false
})
export class ExameModalComponent implements OnInit {
  @Input() exame?: Exame;

  public form!: FormGroup;
  public salvando = false;
  public pacientes: Paciente[] = [];
  public medicos: Medico[] = [];
  /** Atalhos para os pedidos mais frequentes, poupando digitação na recepção. */
  public readonly sugestoes = ['Hemograma completo', 'Glicemia em jejum', 'Eletrocardiograma', 'Raio-X de tórax', 'Urina tipo I'];

  constructor(
    private modalCtrl: ModalController,
    private api: ConsultorioApiService,
    private feedback: FeedbackService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.carregarListas();

    this.form = new FormGroup({
      idPaciente: new FormControl(this.exame?.idPaciente || null, this.exame ? [] : [Validators.required]),
      descricao: new FormControl(this.exame?.descricao || '', this.exame ? [] : [Validators.required, Validators.minLength(2), Validators.maxLength(200)]),
      idMedico: new FormControl(this.exame?.idMedico || null),
      resultado: new FormControl(this.exame?.resultado || '')
    });
  }

  carregarListas() {
    const porNome = <T extends { nome: string }>(lista: T[]) =>
      [...(lista || [])].sort((a, b) => (a.nome ?? '').localeCompare(b.nome ?? '', 'pt-BR'));

    if (!this.exame) {
      this.api.obterPacientes().subscribe({
        next: (p) => { this.pacientes = porNome(p); this.cdr.detectChanges(); },
        error: (err) => this.feedback.erro(err, 'Não foi possível carregar os pacientes.')
      });
    }

    this.api.obterMedicos().subscribe({
      next: (m) => { this.medicos = porNome(m); this.cdr.detectChanges(); },
      error: () => undefined
    });
  }

  usarSugestao(sugestao: string) {
    const campo = this.form.get('descricao');
    campo?.setValue(sugestao);
    campo?.markAsDirty();
    campo?.markAsTouched();
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
        mensagem: 'As informações do exame ainda não foram salvas.',
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
    const resultado = (this.form.value.resultado ?? '').trim();

    const operacao = this.exame
      ? this.api.registrarResultadoExame(this.exame.id, resultado, this.form.value.idMedico)
      : this.api.criarExame({
          idPaciente: this.form.value.idPaciente,
          descricao: (this.form.value.descricao ?? '').trim(),
          idMedico: this.form.value.idMedico || null,
          resultado: resultado || null
        });

    operacao.subscribe({
      next: () => {
        this.salvando = false;
        this.modalCtrl.dismiss(true);
      },
      error: (err) => {
        this.salvando = false;
        this.feedback.erro(err, 'Erro ao salvar exame.');
      }
    });
  }
}
