import { Component, OnInit } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Medico, TIPOS_ATENDIMENTO } from '../types/types';
import { hojeInputData, paraDataLocal, paraInputData } from '../shared/utils/formatadores';

/** Gera horários em lote a partir de uma grade semanal (ex.: seg a sex, 8h–12h, de 30 em 30 min). */
@Component({
  selector: 'app-grade-modal',
  templateUrl: './grade-modal.component.html',
  standalone: false
})
export class GradeModalComponent implements OnInit {
  public medicos: Medico[] = [];
  public readonly tipos = TIPOS_ATENDIMENTO;
  public readonly diasSemana = [
    { valor: 1, nome: 'Seg' }, { valor: 2, nome: 'Ter' }, { valor: 3, nome: 'Qua' },
    { valor: 4, nome: 'Qui' }, { valor: 5, nome: 'Sex' }, { valor: 6, nome: 'Sáb' }, { valor: 0, nome: 'Dom' }
  ];

  public idMedico: number | null = null;
  public dataInicio = hojeInputData();
  public dataFim = paraInputData(new Date(Date.now() + 27 * 24 * 60 * 60 * 1000));
  public dias = new Set<number>([1, 2, 3, 4, 5]);
  public horaInicio = '08:00';
  public horaFim = '12:00';
  public intervalo = 30;
  public usarPausa = false;
  public pausaInicio = '10:00';
  public pausaFim = '10:15';
  public tipo = 1;
  public gerando = false;

  constructor(
    private modalCtrl: ModalController,
    private api: ConsultorioApiService,
    private feedback: FeedbackService
  ) {}

  ngOnInit() {
    this.api.obterMedicos().subscribe({
      next: (m) => this.medicos = [...(m ?? [])].sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR')),
      error: (err) => this.feedback.erro(err, 'Não foi possível carregar os médicos.')
    });
  }

  alternarDia(valor: number) {
    if (this.dias.has(valor)) this.dias.delete(valor);
    else this.dias.add(valor);
  }

  private minutos(hhmm: string): number {
    const [h, m] = (hhmm || '0:0').split(':').map(Number);
    return (h || 0) * 60 + (m || 0);
  }

  /** Horários por dia, descontando a pausa (mesma regra do servidor). */
  get horariosPorDia(): number {
    const ini = this.minutos(this.horaInicio);
    const fim = this.minutos(this.horaFim);
    const passo = Number(this.intervalo);
    if (!passo || passo < 5 || fim <= ini) return 0;
    let total = 0;
    for (let h = ini; h + passo <= fim; h += passo) {
      const cruzaPausa = this.usarPausa && h < this.minutos(this.pausaFim) && h + passo > this.minutos(this.pausaInicio);
      if (!cruzaPausa) total++;
    }
    return total;
  }

  get diasNoPeriodo(): number {
    const ini = paraDataLocal(this.dataInicio);
    const fim = paraDataLocal(this.dataFim);
    if (!ini || !fim || fim < ini) return 0;
    let total = 0;
    for (const d = new Date(ini); d <= fim; d.setDate(d.getDate() + 1)) {
      if (this.dias.has(d.getDay())) total++;
    }
    return total;
  }

  get previa(): number {
    return this.horariosPorDia * this.diasNoPeriodo;
  }

  get podeGerar(): boolean {
    return !!this.idMedico && this.previa > 0 && !this.gerando;
  }

  fechar() {
    this.modalCtrl.dismiss(false);
  }

  gerar() {
    if (!this.podeGerar) return;
    this.gerando = true;
    const comSegundos = (h: string) => h.length === 5 ? `${h}:00` : h;

    this.api.gerarGradeAgenda({
      idMedico: this.idMedico!,
      dataInicio: this.dataInicio,
      dataFim: this.dataFim,
      diasSemana: Array.from(this.dias),
      horaInicio: comSegundos(this.horaInicio),
      horaFim: comSegundos(this.horaFim),
      intervaloMinutos: Number(this.intervalo),
      pausaInicio: this.usarPausa ? comSegundos(this.pausaInicio) : null,
      pausaFim: this.usarPausa ? comSegundos(this.pausaFim) : null,
      tipo: this.tipo
    }).subscribe({
      next: (r) => {
        this.gerando = false;
        const extras = [
          r.ignoradosExistentes ? `${r.ignoradosExistentes} já existiam` : '',
          r.ignoradosBloqueio ? `${r.ignoradosBloqueio} em dias bloqueados` : ''
        ].filter(Boolean).join(', ');
        this.feedback.sucesso(`${r.criados} horário(s) criado(s)${extras ? ' (' + extras + ')' : ''}.`);
        this.modalCtrl.dismiss(true);
      },
      error: (err) => {
        this.gerando = false;
        this.feedback.erro(err, 'Não foi possível gerar os horários.');
      }
    });
  }
}
