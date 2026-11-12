import { ChangeDetectorRef, Component } from '@angular/core';
import { NavController } from '@ionic/angular';
import { finalize } from 'rxjs';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { Indicadores, ItemIndicador, Medico } from '../types/types';
import { hojeInputData, paraInputData } from '../shared/utils/formatadores';

type Atalho = 'mes' | 'mesAnterior' | '30' | 'ano';

/** Painel de gestão: agenda, exames, financeiro e diagnósticos do período. */
@Component({
  selector: 'app-indicadores',
  templateUrl: './indicadores.page.html',
  styleUrls: ['./indicadores.page.scss'],
  standalone: false
})
export class IndicadoresPage {
  atalho: Atalho | null = 'mes';
  inicio = '';
  fim = '';
  idMedico: number | null = null;
  medicos: Medico[] = [];
  dados: Indicadores | null = null;
  carregando = false;

  readonly atalhos: { valor: Atalho; rotulo: string }[] = [
    { valor: 'mes', rotulo: 'Este mês' },
    { valor: 'mesAnterior', rotulo: 'Mês passado' },
    { valor: '30', rotulo: 'Últimos 30 dias' },
    { valor: 'ano', rotulo: 'Este ano' }
  ];

  constructor(
    private api: ConsultorioApiService,
    private feedback: FeedbackService,
    private nav: NavController,
    private cdr: ChangeDetectorRef
  ) {}

  ionViewWillEnter() {
    if (!this.inicio) this.aplicarAtalho('mes', false);
    this.api.obterMedicos().subscribe({
      next: (m) => this.medicos = [...m].sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR')),
      error: () => undefined
    });
    this.carregar();
  }

  aplicarAtalho(atalho: Atalho, recarregar = true) {
    const hoje = new Date();
    const a = hoje.getFullYear();
    const m = hoje.getMonth();
    this.atalho = atalho;
    switch (atalho) {
      case 'mes':
        this.inicio = paraInputData(new Date(a, m, 1));
        this.fim = hojeInputData();
        break;
      case 'mesAnterior':
        this.inicio = paraInputData(new Date(a, m - 1, 1));
        this.fim = paraInputData(new Date(a, m, 0));
        break;
      case '30':
        this.inicio = paraInputData(new Date(a, m, hoje.getDate() - 29));
        this.fim = hojeInputData();
        break;
      case 'ano':
        this.inicio = paraInputData(new Date(a, 0, 1));
        this.fim = hojeInputData();
        break;
    }
    if (recarregar) this.carregar();
  }

  mudarData(campo: 'inicio' | 'fim', valor: string | null | undefined) {
    if (!valor) return;
    this[campo] = valor;
    this.atalho = null;
    this.carregar();
  }

  mudarMedico(valor: number | null) {
    this.idMedico = valor;
    this.carregar();
  }

  carregar() {
    if (this.inicio > this.fim) {
      this.feedback.aviso('A data inicial não pode ser posterior à final.');
      return;
    }
    this.carregando = true;
    this.api.obterIndicadores(this.inicio, this.fim, this.idMedico)
      .pipe(finalize(() => {
        this.carregando = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (d) => this.dados = d,
        error: (err) => this.feedback.erro(err, 'Não foi possível carregar os indicadores.')
      });
  }

  /** Largura proporcional da barra (0–100%). */
  largura(valor: number, lista: { valor?: number; quantidade?: number }[] | ItemIndicador[], campo: 'valor' | 'quantidade'): number {
    const maximo = Math.max(...(lista as any[]).map(i => Number(i[campo]) || 0), 0);
    return maximo === 0 ? 0 : Math.max(2, Math.round((valor / maximo) * 100));
  }

  decimal(valor: number | null | undefined): string {
    return (Number(valor) || 0).toLocaleString('pt-BR', { maximumFractionDigits: 1 });
  }

  get maxMedico(): number {
    return Math.max(...(this.dados?.porMedico ?? []).map(m => m.agendados), 0);
  }

  pendencias() {
    this.nav.navigateForward('/indicadores/pendencias');
  }

  imprimir() {
    window.print();
  }
}
