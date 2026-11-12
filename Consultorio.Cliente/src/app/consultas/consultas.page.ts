import { ChangeDetectorRef, Component } from '@angular/core';
import { Router } from '@angular/router';
import { ModalController } from '@ionic/angular';
import { finalize } from 'rxjs';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Consulta, StatusConsulta } from '../types/types';
import { correspondeBusca } from '../shared/utils/formatadores';
import { ConsultaModalComponent } from './consulta-modal.component';
import { DocumentoModalComponent } from '../documentos/documento-modal.component';

const LIMITE_TEXTO_RECOLHIDO = 280;

type FiltroConsulta = 'todas' | StatusConsulta;

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
  public consultasVisiveis: Consulta[] = [];
  public filtroStatus: FiltroConsulta = 'todas';
  public totais = { todas: 0, abertas: 0, finalizadas: 0, anuladas: 0 };

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
      correspondeBusca(this.termoBusca, c.nomePaciente, c.nomeMedico, c.observacao, c.subjetivo, c.objetivo, c.avaliacao, c.plano,
        c.cidCodigo, c.cidDescricao, ...(c.adendos ?? []).map(a => a.texto))
    );
    const status = (c: Consulta): StatusConsulta => c.status ?? 'Aberta';
    this.totais = {
      todas: this.consultasFiltradas.length,
      abertas: this.consultasFiltradas.filter(c => status(c) === 'Aberta').length,
      finalizadas: this.consultasFiltradas.filter(c => status(c) === 'Finalizada').length,
      anuladas: this.consultasFiltradas.filter(c => status(c) === 'Anulada').length
    };
    this.consultasVisiveis = this.filtroStatus === 'todas'
      ? this.consultasFiltradas
      : this.consultasFiltradas.filter(c => status(c) === this.filtroStatus);
  }

  mudarFiltroStatus(valor: FiltroConsulta | string | undefined) {
    this.filtroStatus = (valor as FiltroConsulta) || 'todas';
    this.filtrar();
  }

  rotuloStatus(status?: StatusConsulta): string {
    switch (status) {
      case 'Finalizada': return 'Finalizada';
      case 'Anulada': return 'Anulada';
      default: return 'Em aberto';
    }
  }

  corStatus(status?: StatusConsulta): string {
    switch (status) {
      case 'Finalizada': return 'success';
      case 'Anulada': return 'medium';
      default: return 'warning';
    }
  }

  limparBusca() {
    this.termoBusca = '';
    this.filtrar();
  }

  temSoap(c: Consulta): boolean {
    return !!(c.subjetivo || c.objetivo || c.avaliacao || c.plano);
  }

  async abrirDocumentos(consulta: Consulta) {
    const modal = await this.modalCtrl.create({
      component: DocumentoModalComponent,
      componentProps: {
        idPaciente: consulta.idPaciente,
        nomePaciente: consulta.nomePaciente,
        idConsulta: consulta.id,
        idMedico: consulta.idMedico,
        cidSugerido: consulta.cidCodigo ? { codigo: consulta.cidCodigo, descricao: consulta.cidDescricao ?? '' } : null
      }
    });
    await modal.present();
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

  async finalizar(consulta: Consulta) {
    const confirmado = await this.feedback.confirmar({
      titulo: 'Finalizar consulta?',
      mensagem: 'Depois de finalizada, a evolução não pode mais ser editada. Correções e complementos serão feitos por adendo, com data e autor.',
      textoConfirmar: 'Finalizar'
    });
    if (!confirmado) return;

    this.api.finalizarConsulta(consulta.id).subscribe({
      next: () => {
        this.feedback.sucesso('Consulta finalizada.');
        this.carregarConsultas();
      },
      error: (err) => this.feedback.erro(err, 'Erro ao finalizar consulta.')
    });
  }

  async adicionarAdendo(consulta: Consulta) {
    const texto = await this.feedback.solicitarTexto({
      titulo: 'Adendo à consulta',
      mensagem: `Complemento ao atendimento de ${consulta.nomePaciente ?? 'paciente'}. Ficará registrado com seu usuário, data e hora.`,
      placeholder: 'Ex.: resultado de exame recebido, orientação complementar...',
      textoConfirmar: 'Registrar adendo',
      minimo: 2,
      maximo: 2000
    });
    if (!texto) return;

    this.api.adicionarAdendoConsulta(consulta.id, texto).subscribe({
      next: () => {
        this.feedback.sucesso('Adendo registrado.');
        this.carregarConsultas();
      },
      error: (err) => this.feedback.erro(err, 'Erro ao registrar adendo.')
    });
  }

  async anular(consulta: Consulta) {
    const motivo = await this.feedback.solicitarTexto({
      titulo: 'Anular consulta?',
      mensagem: 'Use para registros feitos por engano. A consulta continua no histórico, marcada como anulada, com o motivo informado.',
      placeholder: 'Motivo da anulação (ex.: registrada no paciente errado)',
      textoConfirmar: 'Anular',
      destrutivo: true
    });
    if (!motivo) return;

    this.api.anularConsulta(consulta.id, motivo).subscribe({
      next: () => {
        this.feedback.sucesso('Consulta anulada.');
        this.carregarConsultas();
      },
      error: (err) => this.feedback.erro(err, 'Erro ao anular consulta.')
    });
  }
}
