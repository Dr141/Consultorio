import { ChangeDetectorRef, Component } from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { RoleService } from '../services/role/role.service';
import { PendenciaItem, Pendencias } from '../types/types';

type Grupo = {
  chave: keyof Omit<Pendencias, 'total'>;
  titulo: string;
  icone: string;
  cor: string;
  dica: string;
  acao: string;
};

/** Lista do que precisa de atenção hoje, agrupada por tipo, com atalho para resolver cada item. */
@Component({
  selector: 'app-pendencias',
  templateUrl: './pendencias.page.html',
  styleUrls: ['./indicadores.page.scss'],
  styles: [`
    .grupo { margin-bottom: 16px; border-radius: 14px; }
    .grupo ion-card-header { display: flex; flex-direction: row; align-items: center; gap: 10px; padding-bottom: 4px; }
    .grupo ion-card-header ion-icon { font-size: 24px; }
    .grupo ion-card-title { font-size: 17px; }
    .grupo .dica { margin: 0 16px 4px; font-size: 13px; color: var(--app-text-muted); }
    .tudo-certo ion-icon { color: var(--ion-color-success); }
  `],
  standalone: false
})
export class PendenciasPage {
  dados: Pendencias | null = null;
  carregando = false;
  ehClinico = false;

  readonly grupos: Grupo[] = [
    { chave: 'examesAtrasados', titulo: 'Exames atrasados', icone: 'hourglass-outline', cor: 'danger',
      dica: 'Solicitados ou realizados há mais tempo que o prazo, ainda sem laudo.', acao: 'exames' },
    { chave: 'laudosNaoEntregues', titulo: 'Laudos prontos não entregues', icone: 'document-attach-outline', cor: 'warning',
      dica: 'Avise o paciente ou registre a entrega na tela de exames.', acao: 'exames' },
    { chave: 'cobrancasVencidas', titulo: 'Cobranças vencidas', icone: 'wallet-outline', cor: 'danger',
      dica: 'Valores em aberto depois do vencimento.', acao: 'financeiro' },
    { chave: 'carteirinhas', titulo: 'Carteirinhas vencidas ou vencendo', icone: 'card-outline', cor: 'warning',
      dica: 'Peça a carteirinha atualizada no próximo contato com o paciente.', acao: 'pacientes' },
    { chave: 'retornosSemAgendamento', titulo: 'Retornos sem horário marcado', icone: 'repeat-outline', cor: 'primary',
      dica: 'O médico indicou retorno e o paciente ainda não agendou.', acao: 'agenda' },
    { chave: 'consultasAbertas', titulo: 'Consultas abertas há mais de 24 h', icone: 'lock-open-outline', cor: 'medium',
      dica: 'Finalize a consulta para encerrar o registro no prontuário.', acao: 'consultas' }
  ];

  constructor(
    private api: ConsultorioApiService,
    private feedback: FeedbackService,
    private role: RoleService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ionViewWillEnter() {
    this.ehClinico = this.role.ehClinico();
    this.carregar();
  }

  carregar(aoConcluir?: () => void) {
    this.carregando = true;
    this.api.obterPendencias()
      .pipe(finalize(() => {
        this.carregando = false;
        aoConcluir?.();
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (d) => this.dados = d,
        error: (err) => this.feedback.erro(err, 'Não foi possível carregar as pendências.')
      });
  }

  atualizar(evento: Event) {
    this.carregar(() => (evento.target as HTMLIonRefresherElement | null)?.complete());
  }

  itens(g: Grupo): PendenciaItem[] {
    return (this.dados?.[g.chave] as PendenciaItem[] | undefined) ?? [];
  }

  get gruposComItens(): Grupo[] {
    return this.grupos.filter(g => this.itens(g).length > 0);
  }

  abrir(g: Grupo, item: PendenciaItem) {
    switch (g.acao) {
      case 'financeiro':
        this.router.navigate(['/financeiro'], { queryParams: { status: 'Pendente' } });
        break;
      case 'consultas':
        this.router.navigate(['/consultas']);
        break;
      case 'agenda':
        this.router.navigate(['/agenda']);
        break;
      case 'pacientes':
        this.router.navigate(['/pacientes']);
        break;
      default:
        this.router.navigate(['/exames']);
    }
  }

  prontuario(item: PendenciaItem) {
    if (item.idPaciente) this.router.navigate(['/prontuario'], { queryParams: { idPaciente: item.idPaciente } });
  }
}
