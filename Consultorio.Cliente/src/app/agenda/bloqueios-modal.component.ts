import { Component, OnInit } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Agenda, BloqueioAgenda, Medico } from '../types/types';
import { hojeInputData } from '../shared/utils/formatadores';

/**
 * Bloqueios de agenda (férias, feriados, congressos). Ao bloquear, os horários livres do período
 * são removidos e os que já têm paciente são listados para a recepção remarcar.
 */
@Component({
  selector: 'app-bloqueios-modal',
  templateUrl: './bloqueios-modal.component.html',
  standalone: false
})
export class BloqueiosModalComponent implements OnInit {
  public bloqueios: BloqueioAgenda[] = [];
  public medicos: Medico[] = [];
  public carregando = false;
  public salvando = false;

  public idMedico: number | null = null;
  public dataInicio = hojeInputData();
  public dataFim = hojeInputData();
  public motivo = '';

  /** Pacientes que estavam agendados no período bloqueado e precisam ser remarcados. */
  public conflitos: Agenda[] = [];

  constructor(
    private modalCtrl: ModalController,
    private api: ConsultorioApiService,
    private feedback: FeedbackService
  ) {}

  ngOnInit() {
    this.carregar();
    this.api.obterMedicos().subscribe({
      next: (m) => this.medicos = [...(m ?? [])].sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR')),
      error: () => undefined
    });
  }

  carregar() {
    this.carregando = true;
    this.api.obterBloqueiosAgenda().subscribe({
      next: (lista) => { this.bloqueios = lista ?? []; this.carregando = false; },
      error: (err) => { this.carregando = false; this.feedback.erro(err, 'Erro ao carregar bloqueios.'); }
    });
  }

  get podeSalvar(): boolean {
    return !!this.dataInicio && !!this.dataFim && this.dataFim >= this.dataInicio && this.motivo.trim().length >= 3 && !this.salvando;
  }

  async salvar() {
    if (!this.podeSalvar) return;
    const quem = this.idMedico ? this.medicos.find(m => m.id === this.idMedico)?.nome ?? 'o médico' : 'toda a clínica';
    const ok = await this.feedback.confirmar({
      titulo: 'Bloquear agenda?',
      mensagem: `A agenda de ${quem} ficará bloqueada no período. Horários livres serão removidos; pacientes já agendados serão listados para remarcação.`,
      textoConfirmar: 'Bloquear',
      destrutivo: true
    });
    if (!ok) return;

    this.salvando = true;
    this.api.criarBloqueioAgenda({
      idMedico: this.idMedico,
      dataInicio: this.dataInicio,
      dataFim: this.dataFim,
      motivo: this.motivo.trim()
    }).subscribe({
      next: (r) => {
        this.salvando = false;
        this.conflitos = r.conflitos ?? [];
        this.motivo = '';
        const msg = `Agenda bloqueada. ${r.horariosLivresRemovidos} horário(s) livre(s) removido(s).`;
        if (this.conflitos.length) {
          this.feedback.aviso(`${msg} ${this.conflitos.length} paciente(s) precisam ser remarcados.`);
        } else {
          this.feedback.sucesso(msg);
        }
        this.carregar();
      },
      error: (err) => {
        this.salvando = false;
        this.feedback.erro(err, 'Não foi possível bloquear a agenda.');
      }
    });
  }

  async remover(b: BloqueioAgenda) {
    const ok = await this.feedback.confirmar({
      titulo: 'Remover bloqueio?',
      mensagem: `"${b.motivo}" deixará de bloquear a agenda. Os horários removidos não voltam automaticamente; gere-os novamente se precisar.`,
      textoConfirmar: 'Remover',
      destrutivo: true
    });
    if (!ok) return;

    this.api.removerBloqueioAgenda(b.id).subscribe({
      next: () => { this.feedback.sucesso('Bloqueio removido.'); this.carregar(); },
      error: (err) => this.feedback.erro(err, 'Não foi possível remover o bloqueio.')
    });
  }

  fechar() {
    this.modalCtrl.dismiss();
  }
}
