import { ChangeDetectorRef, Component } from '@angular/core';
import { Router } from '@angular/router';
import { ModalController } from '@ionic/angular';
import { finalize } from 'rxjs';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Consulta } from '../types/types';
import { correspondeBusca } from '../shared/utils/formatadores';
import { ConsultaModalComponent } from './consulta-modal.component';

const LIMITE_TEXTO_RECOLHIDO = 280;

@Component({
  selector: 'app-consultas',
  templateUrl: './consultas.page.html',
  styleUrls: ['./consultas.page.scss'],
  standalone: false
})
export class ConsultasPage {
  public consultas: Consulta[] = [];
  public consultasFiltradas: Consulta[] = [];
  public termoBusca = '';
  public carregando = false;
  public expandidas = new Set<number>();

  constructor(
    private api: ConsultorioApiService,
    private modalCtrl: ModalController,
    private feedback: FeedbackService,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  ionViewWillEnter() {
    this.carregarConsultas();
  }

  atualizar(evento: Event) {
    this.carregarConsultas(() => (evento.target as HTMLIonRefresherElement | null)?.complete());
  }

  carregarConsultas(aoConcluir?: () => void) {
    this.carregando = true;
    this.cdr.markForCheck();
    this.api.obterConsultas()
      .pipe(finalize(() => {
        this.carregando = false;
        aoConcluir?.();
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (lista) => {
          const instante = (c: Consulta) => new Date(c.dataHora || c.dtCriacao || 0).getTime() || 0;
          this.consultas = [...(lista || [])].sort((a, b) => instante(b) - instante(a));
          this.filtrar();
        },
        error: (err) => this.feedback.erro(err, 'Erro ao carregar consultas.')
      });
  }

  ehLongo(texto: string | undefined): boolean {
    return (texto?.length ?? 0) > LIMITE_TEXTO_RECOLHIDO;
  }

  alternarExpandida(id: number) {
    if (this.expandidas.has(id)) this.expandidas.delete(id);
    else this.expandidas.add(id);
  }

  filtrar() {
    this.consultasFiltradas = this.consultas.filter(c =>
      correspondeBusca(this.termoBusca, c.nomePaciente, c.nomeMedico, c.observacao)
    );
  }

  limparBusca() {
    this.termoBusca = '';
    this.filtrar();
  }

  abrirProntuario(consulta: Consulta) {
    this.router.navigate(['/prontuario'], { queryParams: { idPaciente: consulta.idPaciente } });
  }

  async adicionar() {
    await this.abrirModal();
  }

  async editar(consulta: Consulta) {
    await this.abrirModal(consulta);
  }

  private async abrirModal(consulta?: Consulta) {
    const modal = await this.modalCtrl.create({
      component: ConsultaModalComponent,
      componentProps: { consulta }
    });

    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data) {
      this.feedback.sucesso(consulta ? 'Evolução da consulta atualizada.' : 'Consulta registrada com sucesso.');
      this.carregarConsultas();
    }
  }

  async remover(consulta: Consulta) {
    const confirmado = await this.feedback.confirmar({
      titulo: 'Excluir consulta?',
      mensagem: `O registro da consulta de ${consulta.nomePaciente ?? 'paciente'} será removido do histórico. Esta ação não pode ser desfeita.`,
      textoConfirmar: 'Excluir',
      destrutivo: true
    });
    if (!confirmado) return;

    this.api.removerConsulta(consulta.id).subscribe({
      next: () => {
        this.feedback.sucesso('Consulta excluída.');
        this.carregarConsultas();
      },
      error: (err) => this.feedback.erro(err, 'Erro ao excluir consulta.')
    });
  }
}
