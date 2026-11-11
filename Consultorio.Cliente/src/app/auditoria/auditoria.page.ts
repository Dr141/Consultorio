import { ChangeDetectorRef, Component } from '@angular/core';
import { finalize } from 'rxjs';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { RegistroAuditoria } from '../types/types';
import { hojeInputData, paraInputData } from '../shared/utils/formatadores';

/**
 * Trilha de auditoria (LGPD): quem acessou ou alterou dados de pacientes e prontuários.
 * Somente administradores (a API recusa os demais perfis).
 */
@Component({
  selector: 'app-auditoria',
  templateUrl: './auditoria.page.html',
  styleUrls: ['./auditoria.page.scss'],
  standalone: false
})
export class AuditoriaPage {
  public registros: RegistroAuditoria[] = [];
  public carregando = false;
  public readonly recursos = ['Consulta', 'Exame', 'Paciente', 'Prontuario'];

  public dataInicio = paraInputData(new Date(Date.now() - 7 * 24 * 60 * 60 * 1000));
  public dataFim = hojeInputData();
  public usuario = '';
  public recurso: string | null = null;
  public idRecurso = '';

  constructor(
    private api: ConsultorioApiService,
    private feedback: FeedbackService,
    private cdr: ChangeDetectorRef
  ) {}

  ionViewWillEnter() {
    this.buscar();
  }

  buscar() {
    this.carregando = true;
    this.api.obterAuditoria({
      // Período em horário local convertido para UTC (início do primeiro dia até o fim do último).
      inicio: this.dataInicio ? new Date(`${this.dataInicio}T00:00:00`).toISOString() : null,
      fim: this.dataFim ? new Date(`${this.dataFim}T23:59:59`).toISOString() : null,
      usuario: this.usuario,
      recurso: this.recurso,
      idRecurso: this.idRecurso
    })
      .pipe(finalize(() => {
        this.carregando = false;
        this.cdr.detectChanges();
      }))
      .subscribe({
        next: (lista) => this.registros = lista ?? [],
        error: (err) => this.feedback.erro(err, 'Erro ao consultar a auditoria.')
      });
  }

  limpar() {
    this.usuario = '';
    this.recurso = null;
    this.idRecurso = '';
    this.buscar();
  }

  corAcao(r: RegistroAuditoria): string {
    if (r.statusCode >= 400) return 'danger';
    switch (r.acao) {
      case 'Leitura': return 'medium';
      case 'Criacao': return 'success';
      case 'Exclusao': return 'danger';
      default: return 'warning';
    }
  }

  rotuloAcao(acao: string): string {
    const mapa: Record<string, string> = {
      Leitura: 'Leitura',
      Criacao: 'Criação',
      Alteracao: 'Alteração',
      Exclusao: 'Exclusão',
      Adendo: 'Adendo',
      Finalizar: 'Finalização',
      Anular: 'Anulação',
      Cancelar: 'Cancelamento',
      Reativar: 'Reativação',
      RegistrarResultado: 'Laudo'
    };
    return mapa[acao] ?? acao;
  }
}
