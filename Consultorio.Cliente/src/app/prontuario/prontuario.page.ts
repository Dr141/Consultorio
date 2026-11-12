import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ModalController, ToastController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { Medico, Paciente, ProntuarioRelatorio, StatusExame } from '../types/types';
import { AlertasModalComponent } from '../shared/alertas-clinicos/alertas-modal.component';
import { DocumentoModalComponent } from '../documentos/documento-modal.component';

@Component({
  selector: 'app-prontuario',
  templateUrl: './prontuario.page.html',
  styleUrls: ['./prontuario.page.scss'],
  standalone: false
})
export class ProntuarioPage implements OnInit {
  public pacientes: Paciente[] = [];
  public medicos: Medico[] = [];

  public idPacienteSelecionado: number | null = null;
  public idMedicoSelecionado: number | null = null;
  public dataInicio: string = '';
  public dataFim: string = '';

  public prontuario: ProntuarioRelatorio | null = null;
  public carregando = false;

  constructor(
    private api: ConsultorioApiService,
    private route: ActivatedRoute,
    private router: Router,
    private toastCtrl: ToastController,
    private cdr: ChangeDetectorRef,
    private modalCtrl: ModalController
  ) {}

  ngOnInit() {
    this.carregarListasAuxiliares();
  }

  ionViewWillEnter() {
    this.verificarParametroRota();
  }

  carregarListasAuxiliares() {
    this.api.obterPacientes().subscribe({
      next: (lista) => {
        this.pacientes = lista || [];
        this.verificarParametroRota();
        this.cdr.detectChanges();
      },
      error: () => {}
    });

    this.api.obterMedicos().subscribe({
      next: (lista) => {
        this.medicos = lista || [];
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  verificarParametroRota() {
    this.route.queryParams.subscribe((params) => {
      const idParam = params['idPaciente'];
      if (idParam) {
        const idNum = Number(idParam);
        if (!isNaN(idNum) && idNum > 0 && idNum !== this.idPacienteSelecionado) {
          this.idPacienteSelecionado = idNum;
          this.gerarProntuario();
        }
      }
    });
  }

  aoMudarPaciente() {
    this.prontuario = null;
    if (this.idPacienteSelecionado) {
      this.gerarProntuario();
    }
  }

  definirPeriodo(dias: number) {
    const hoje = new Date();
    const dataInicial = new Date();
    dataInicial.setDate(hoje.getDate() - dias);

    this.dataInicio = dataInicial.toISOString().substring(0, 10);
    this.dataFim = hoje.toISOString().substring(0, 10);
  }

  definirPeriodoEsteAno() {
    const hoje = new Date();
    const inicioAno = new Date(hoje.getFullYear(), 0, 1);

    this.dataInicio = inicioAno.toISOString().substring(0, 10);
    this.dataFim = hoje.toISOString().substring(0, 10);
  }

  limparDatas() {
    this.dataInicio = '';
    this.dataFim = '';
  }

  limparFiltros() {
    this.idPacienteSelecionado = null;
    this.idMedicoSelecionado = null;
    this.limparDatas();
    this.prontuario = null;
  }

  gerarProntuario() {
    if (!this.idPacienteSelecionado) {
      this.exibirMensagem('Selecione um paciente para gerar o prontuário.', 'warning');
      return;
    }

    if (this.dataInicio && this.dataFim && this.dataInicio > this.dataFim) {
      this.exibirMensagem('A data inicial não pode ser superior à data final.', 'danger');
      return;
    }

    this.carregando = true;
    this.cdr.markForCheck();

    this.api.obterProntuario(this.idPacienteSelecionado, {
      idMedico: this.idMedicoSelecionado,
      dataInicio: this.dataInicio || null,
      dataFim: this.dataFim || null
    }).subscribe({
      next: (relatorio) => {
        this.prontuario = relatorio;
        this.carregando = false;
        this.cdr.detectChanges();
        // Em telas menores o ofício fica abaixo dos filtros; leva a visão até ele.
        setTimeout(() => document.getElementById('area-oficio')?.scrollIntoView({ block: 'start' }));
      },
      error: (err) => {
        this.carregando = false;
        this.cdr.detectChanges();
        if (err?.status === 401 || err?.message?.includes('401')) {
          this.router.navigate(['/login']);
          return;
        }
        this.exibirMensagem(err?.message || 'Erro ao gerar prontuário.', 'danger');
      }
    });
  }

  rotuloStatusExame(status: StatusExame): string {
    switch (status) {
      case 'Solicitado': return 'SOLICITADO';
      case 'Realizado': return 'REALIZADO';
      case 'Laudado': return 'LAUDADO';
      case 'Entregue': return 'ENTREGUE';
    }
  }

  async editarAlertas() {
    if (!this.prontuario) return;
    const modal = await this.modalCtrl.create({
      component: AlertasModalComponent,
      componentProps: { paciente: this.prontuario.paciente }
    });
    await modal.present();
    const { data } = await modal.onDidDismiss();
    if (data && this.prontuario) {
      this.prontuario = { ...this.prontuario, paciente: data as Paciente };
      this.cdr.detectChanges();
    }
  }

  async abrirDocumentos() {
    if (!this.prontuario) return;
    const modal = await this.modalCtrl.create({
      component: DocumentoModalComponent,
      componentProps: { idPaciente: this.prontuario.paciente.id, nomePaciente: this.prontuario.paciente.nome }
    });
    await modal.present();
  }

  abrirDocumento(id: number) {
    this.router.navigate(['/documento', id]);
  }

  imprimir() {
    if (!this.prontuario) return;
    window.print();
  }

  formatarData(dataStr?: string | null): string {
    if (!dataStr) return '';
    const d = new Date(dataStr);
    return isNaN(d.getTime()) ? '' : d.toLocaleDateString('pt-BR');
  }

  formatarDataHora(dataStr?: string | null): string {
    if (!dataStr) return '';
    const d = new Date(dataStr);
    return isNaN(d.getTime()) ? '' : d.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
  }

  calcularIdade(dataNascStr?: string): number {
    if (!dataNascStr) return 0;
    const nasc = new Date(dataNascStr);
    if (isNaN(nasc.getTime())) return 0;
    const hoje = new Date();
    let idade = hoje.getFullYear() - nasc.getFullYear();
    const m = hoje.getMonth() - nasc.getMonth();
    if (m < 0 || (m === 0 && hoje.getDate() < nasc.getDate())) {
      idade--;
    }
    return Math.max(0, idade);
  }

  get dataHojeExtenso(): string {
    return new Date().toLocaleDateString('pt-BR', {
      day: 'numeric',
      month: 'long',
      year: 'numeric'
    });
  }

  private async exibirMensagem(texto: string, cor: 'warning' | 'danger' | 'success') {
    const toast = await this.toastCtrl.create({
      message: texto,
      duration: 3500,
      color: cor
    });
    await toast.present();
  }
}
