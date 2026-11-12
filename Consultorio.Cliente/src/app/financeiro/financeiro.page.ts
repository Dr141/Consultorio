import { ChangeDetectorRef, Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ModalController } from '@ionic/angular';
import { finalize, forkJoin } from 'rxjs';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { RoleService } from '../services/role/role.service';
import { Cobranca, Convenio, StatusCobranca, TIPOS_ATENDIMENTO, TabelaPreco, TipoAtendimento } from '../types/types';
import { correspondeBusca, hojeInputData, paraInputData } from '../shared/utils/formatadores';
import { PagamentoModalComponent } from './pagamento-modal.component';
import { CobrancaModalComponent } from './cobranca-modal.component';
import { ConvenioModalComponent } from './convenio-modal.component';

type Aba = 'cobrancas' | 'precos' | 'convenios';
type Periodo = 'hoje' | '7' | '30' | 'mes';
type FiltroStatus = 'todas' | StatusCobranca;

@Component({
  selector: 'app-financeiro',
  templateUrl: './financeiro.page.html',
  styleUrls: ['./financeiro.page.scss'],
  standalone: false
})
export class FinanceiroPage {
  aba: Aba = 'cobrancas';
  podeGerir = false;
  carregando = false;

  // Cobranças
  periodo: Periodo = '30';
  status: FiltroStatus = 'todas';
  termoBusca = '';
  cobrancas: Cobranca[] = [];
  visiveis: Cobranca[] = [];
  totais = { recebido: 0, aberto: 0, vencido: 0 };

  // Preços e convênios
  readonly tipos = TIPOS_ATENDIMENTO;
  convenios: Convenio[] = [];
  precos: TabelaPreco[] = [];
  mostrarConveniosInativos = false;
  salvandoPreco = '';

  constructor(
    private api: ConsultorioApiService,
    private modalCtrl: ModalController,
    private feedback: FeedbackService,
    private role: RoleService,
    private cdr: ChangeDetectorRef,
    private router: Router,
    private rota: ActivatedRoute
  ) {}

  ionViewWillEnter() {
    this.podeGerir = this.role.ehGestao();
    const aba = this.rota.snapshot.queryParamMap.get('aba') as Aba | null;
    if (aba && ['cobrancas', 'precos', 'convenios'].includes(aba)) this.aba = aba;
    if (this.rota.snapshot.queryParamMap.get('status') === 'Pendente') this.status = 'Pendente';
    this.carregarTudo();
  }

  mudarAba(valor: string | undefined) {
    this.aba = (valor as Aba) || 'cobrancas';
  }

  carregarTudo(aoConcluir?: () => void) {
    this.carregando = true;
    forkJoin({
      convenios: this.api.obterConvenios(true),
      precos: this.api.obterPrecos()
    }).subscribe({
      next: ({ convenios, precos }) => {
        this.convenios = convenios;
        this.precos = precos;
      },
      error: (err) => this.feedback.erro(err, 'Não foi possível carregar convênios e preços.')
    });
    this.carregarCobrancas(aoConcluir);
  }

  atualizar(evento: Event) {
    this.carregarTudo(() => (evento.target as HTMLIonRefresherElement | null)?.complete());
  }

  // ---------- Cobranças ----------

  private intervalo(): { inicio: string; fim: string } {
    const hoje = new Date();
    const fim = hojeInputData();
    switch (this.periodo) {
      case 'hoje': return { inicio: fim, fim };
      case '7': return { inicio: paraInputData(new Date(hoje.getFullYear(), hoje.getMonth(), hoje.getDate() - 6)), fim };
      case 'mes': return { inicio: paraInputData(new Date(hoje.getFullYear(), hoje.getMonth(), 1)), fim };
      default: return { inicio: paraInputData(new Date(hoje.getFullYear(), hoje.getMonth(), hoje.getDate() - 29)), fim };
    }
  }

  mudarPeriodo(p: Periodo) {
    this.periodo = p;
    this.carregarCobrancas();
  }

  mudarStatus(valor: string | undefined) {
    this.status = (valor as FiltroStatus) || 'todas';
    this.carregarCobrancas();
  }

  carregarCobrancas(aoConcluir?: () => void) {
    this.carregando = true;
    // "Pendentes" mostra todas as cobranças em aberto, independentemente do período.
    const consulta = this.status === 'Pendente'
      ? this.api.obterCobrancasPendentes()
      : this.api.obterCobrancas({ ...this.intervalo(), status: this.status === 'todas' ? null : this.status });
    consulta
      .pipe(finalize(() => {
        this.carregando = false;
        aoConcluir?.();
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (lista) => {
          this.cobrancas = lista;
          this.filtrar();
        },
        error: (err) => this.feedback.erro(err, 'Não foi possível carregar as cobranças.')
      });
  }

  filtrar() {
    this.visiveis = this.cobrancas.filter(c => correspondeBusca(this.termoBusca, c.nomePaciente, c.descricao, c.nomeConvenio, c.cpfPaciente));
    const soma = (lista: Cobranca[]) => lista.reduce((t, c) => t + c.valorFinal, 0);
    this.totais = {
      recebido: soma(this.visiveis.filter(c => c.status === 'Paga')),
      aberto: soma(this.visiveis.filter(c => c.status === 'Pendente')),
      vencido: soma(this.visiveis.filter(c => c.status === 'Pendente' && c.vencida))
    };
  }

  rotuloStatus(c: Cobranca): string {
    if (c.status === 'Pendente') return c.vencida ? 'Vencida' : 'Pendente';
    return c.status;
  }

  corStatus(c: Cobranca): string {
    switch (c.status) {
      case 'Paga': return 'success';
      case 'Pendente': return c.vencida ? 'danger' : 'warning';
      case 'Estornada': return 'tertiary';
      default: return 'medium';
    }
  }

  async novaCobranca() {
    const modal = await this.modalCtrl.create({ component: CobrancaModalComponent });
    await modal.present();
    const { data } = await modal.onDidDismiss<Cobranca>();
    if (data) this.carregarCobrancas();
  }

  async receber(c: Cobranca) {
    const modal = await this.modalCtrl.create({ component: PagamentoModalComponent, componentProps: { cobranca: c } });
    await modal.present();
    const { data } = await modal.onDidDismiss<Cobranca>();
    if (!data) return;
    this.carregarCobrancas();
    const imprimir = await this.feedback.confirmar({
      titulo: 'Pagamento registrado',
      mensagem: `${data.nomePaciente} pagou ${data.valorFinal.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}. Deseja emitir o recibo?`,
      textoConfirmar: 'Emitir recibo',
      textoCancelar: 'Agora não'
    });
    if (imprimir) this.recibo(data);
  }

  recibo(c: Cobranca) {
    this.router.navigate(['/financeiro/recibo', c.id]);
  }

  async cancelar(c: Cobranca) {
    const motivo = await this.feedback.solicitarTexto({
      titulo: 'Cancelar cobrança',
      mensagem: `"${c.descricao}" de ${c.nomePaciente} continua no histórico com o motivo informado.`,
      placeholder: 'Motivo (ex.: lançada em duplicidade, cortesia)',
      textoConfirmar: 'Cancelar cobrança',
      destrutivo: true
    });
    if (!motivo) return;
    this.api.cancelarCobranca(c.id, motivo).subscribe({
      next: () => {
        this.feedback.sucesso('Cobrança cancelada.');
        this.carregarCobrancas();
      },
      error: (err) => this.feedback.erro(err, 'Não foi possível cancelar a cobrança.')
    });
  }

  async estornar(c: Cobranca) {
    const motivo = await this.feedback.solicitarTexto({
      titulo: 'Estornar pagamento',
      mensagem: `O valor de ${c.valorFinal.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })} recebido de ${c.nomePaciente} será registrado como devolvido.`,
      placeholder: 'Motivo do estorno',
      textoConfirmar: 'Estornar',
      destrutivo: true
    });
    if (!motivo) return;
    this.api.estornarCobranca(c.id, motivo).subscribe({
      next: () => {
        this.feedback.sucesso('Pagamento estornado.');
        this.carregarCobrancas();
      },
      error: (err) => this.feedback.erro(err, 'Não foi possível estornar o pagamento.')
    });
  }

  // ---------- Tabela de preços ----------

  get tabelas(): { idConvenio: number | null; nome: string; ativo: boolean }[] {
    return [
      { idConvenio: null, nome: 'Particular', ativo: true },
      ...this.convenios.filter(c => c.ativo || this.precos.some(p => p.idConvenio === c.id))
        .map(c => ({ idConvenio: c.id as number | null, nome: c.nome, ativo: c.ativo }))
    ];
  }

  preco(tipo: TipoAtendimento, idConvenio: number | null): TabelaPreco | undefined {
    return this.precos.find(p => p.tipo === tipo && (p.idConvenio ?? null) === idConvenio);
  }

  salvarPreco(tipo: { valor: number; nome: TipoAtendimento }, idConvenio: number | null, bruto: string | number | null | undefined) {
    const texto = `${bruto ?? ''}`.trim().replace(',', '.');
    const atual = this.preco(tipo.nome, idConvenio);
    if (texto === '') {
      if (!atual) return;
      this.api.removerPreco(atual.id).subscribe({
        next: () => {
          this.precos = this.precos.filter(p => p.id !== atual.id);
          this.feedback.sucesso(`Valor de ${tipo.nome} removido.`);
        },
        error: (err) => this.feedback.erro(err, 'Não foi possível remover o valor.')
      });
      return;
    }
    const valor = Number(texto);
    if (isNaN(valor) || valor < 0) {
      this.feedback.aviso('Informe um valor válido.');
      return;
    }
    if (atual && atual.valor === valor) return;

    this.salvandoPreco = `${tipo.valor}-${idConvenio ?? 0}`;
    this.api.definirPreco({ tipo: tipo.valor, idConvenio, valor }).subscribe({
      next: (salvo) => {
        this.salvandoPreco = '';
        this.precos = [...this.precos.filter(p => p.id !== salvo.id), salvo];
        this.feedback.sucesso(`Valor de ${tipo.nome} salvo.`);
      },
      error: (err) => {
        this.salvandoPreco = '';
        this.feedback.erro(err, 'Não foi possível salvar o valor.');
      }
    });
  }

  // ---------- Convênios ----------

  get conveniosVisiveis(): Convenio[] {
    return this.convenios.filter(c => this.mostrarConveniosInativos || c.ativo);
  }

  async editarConvenio(convenio?: Convenio) {
    const modal = await this.modalCtrl.create({ component: ConvenioModalComponent, componentProps: { convenio } });
    await modal.present();
    const { data } = await modal.onDidDismiss<Convenio>();
    if (data) this.carregarTudo();
  }

  async alternarConvenio(c: Convenio) {
    if (c.ativo) {
      const ok = await this.feedback.confirmar({
        titulo: 'Inativar convênio?',
        mensagem: `${c.nome} deixa de aparecer para novos vínculos. Pacientes e cobranças já registrados são mantidos.`,
        textoConfirmar: 'Inativar',
        destrutivo: true
      });
      if (!ok) return;
    }
    this.api.alterarSituacaoConvenio(c.id, c.ativo ? 'inativar' : 'reativar').subscribe({
      next: () => {
        this.feedback.sucesso(c.ativo ? 'Convênio inativado.' : 'Convênio reativado.');
        this.carregarTudo();
      },
      error: (err) => this.feedback.erro(err, 'Não foi possível alterar o convênio.')
    });
  }
}
