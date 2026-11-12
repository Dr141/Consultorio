import { Component, EventEmitter, Input, Output } from '@angular/core';
import { linhas } from '../utils/formatadores';

type AlertasPaciente = {
  alergias?: string | null;
  medicamentosEmUso?: string | null;
  condicoesCronicas?: string | null;
};

/**
 * Faixa de alertas clínicos do paciente (alergias em destaque, medicamentos e condições crônicas).
 * Aparece onde o médico decide algo: registro de consulta, emissão de documentos e prontuário.
 */
@Component({
  selector: 'app-alertas-clinicos',
  template: `
    @if (temAlgum) {
      <section class="alertas-clinicos" [class.compacto]="compacto" aria-label="Alertas clínicos do paciente">
        @if (alergias.length) {
          <div class="alerta-linha alerta-alergia" role="alert">
            <ion-icon name="warning-outline" aria-hidden="true"></ion-icon>
            <strong>Alergias:</strong>
            <span>{{ alergias.join(' · ') }}</span>
          </div>
        }
        @if (condicoes.length) {
          <div class="alerta-linha alerta-condicao">
            <ion-icon name="pulse-outline" aria-hidden="true"></ion-icon>
            <strong>Condições crônicas:</strong>
            <span>{{ condicoes.join(' · ') }}</span>
          </div>
        }
        @if (medicamentos.length) {
          <div class="alerta-linha alerta-medicamento">
            <ion-icon name="medkit-outline" aria-hidden="true"></ion-icon>
            <strong>Em uso:</strong>
            <span>{{ medicamentos.join(' · ') }}</span>
          </div>
        }
        @if (editavel) {
          <ion-button size="small" fill="clear" (click)="editar.emit()">
            <ion-icon slot="start" name="create-outline"></ion-icon>
            Editar alertas
          </ion-button>
        }
      </section>
    } @else if (mostrarVazio) {
      <section class="alertas-clinicos vazio" [class.compacto]="compacto">
        <div class="alerta-linha">
          <ion-icon name="shield-checkmark-outline" aria-hidden="true"></ion-icon>
          <span>Nenhuma alergia, medicamento ou condição crônica registrada.</span>
        </div>
        @if (editavel) {
          <ion-button size="small" fill="clear" (click)="editar.emit()">
            <ion-icon slot="start" name="add-circle-outline"></ion-icon>
            Registrar alertas
          </ion-button>
        }
      </section>
    }
  `,
  standalone: false
})
export class AlertasClinicosComponent {
  @Input() set paciente(valor: AlertasPaciente | null | undefined) {
    this.alergias = linhas(valor?.alergias);
    this.medicamentos = linhas(valor?.medicamentosEmUso);
    this.condicoes = linhas(valor?.condicoesCronicas);
  }
  /** Mostra uma linha neutra quando não há alertas (útil no cadastro/prontuário). */
  @Input() mostrarVazio = false;
  @Input() editavel = false;
  @Input() compacto = false;
  @Output() editar = new EventEmitter<void>();

  alergias: string[] = [];
  medicamentos: string[] = [];
  condicoes: string[] = [];

  get temAlgum(): boolean {
    return this.alergias.length + this.medicamentos.length + this.condicoes.length > 0;
  }
}
