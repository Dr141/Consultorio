import { Component, Input, OnInit } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { Paciente } from '../../types/types';
import { correspondeBusca } from '../utils/formatadores';

/**
 * Modal de seleção de paciente com busca por nome ou CPF.
 * Substitui listas de rádio extensas, que ficam inutilizáveis com muitos pacientes.
 * Retorna o id do paciente escolhido no dismiss (role 'selecionado').
 */
@Component({
  selector: 'app-selecionar-paciente',
  template: `
    <ion-header>
      <ion-toolbar color="primary">
        <ion-title>{{ titulo }}</ion-title>
        <ion-buttons slot="end">
          <ion-button (click)="fechar()">
            <ion-icon slot="start" name="close-outline"></ion-icon>
            Fechar
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
      <ion-toolbar>
        <ion-searchbar
          class="seletor-busca"
          placeholder="Buscar por nome ou CPF"
          [debounce]="150"
          (ionInput)="filtrar($any($event).detail.value)">
        </ion-searchbar>
      </ion-toolbar>
    </ion-header>

    <ion-content>
      @if (subtitulo) {
        <div class="seletor-contexto">
          <ion-icon name="calendar-outline"></ion-icon>
          <span>{{ subtitulo }}</span>
        </div>
      }

      @if (filtrados.length === 0) {
        <div class="empty-state ion-text-center ion-padding">
          <ion-icon name="search-outline"></ion-icon>
          <h3>Nenhum paciente encontrado</h3>
          <p>Tente buscar por outra parte do nome ou pelo CPF.</p>
        </div>
      } @else {
        <ion-list lines="full" class="seletor-lista">
          @for (p of filtrados; track p.id) {
            <ion-item button detail="true" class="seletor-item" (click)="selecionar(p)">
              <div slot="start" class="avatar-iniciais">{{ iniciais(p.nome) }}</div>
              <ion-label>
                <h3>{{ p.nome }}</h3>
                <p>CPF: {{ p.cpf | cpf }}@if (p.telefone) { · {{ p.telefone | telefone }} }</p>
              </ion-label>
            </ion-item>
          }
        </ion-list>
      }
    </ion-content>
  `,
  styles: [`
    .seletor-contexto {
      display: flex;
      align-items: center;
      gap: 8px;
      margin: 12px 16px 4px;
      padding: 10px 12px;
      border-radius: 10px;
      background: rgba(var(--ion-color-primary-rgb), 0.1);
      color: var(--ion-color-primary);
      font-size: 14px;
      font-weight: 600;
    }
    .seletor-lista { padding: 8px 8px 24px; }
    .seletor-item { --border-radius: 10px; margin-bottom: 4px; }
    .seletor-item h3 { font-weight: 600; }
  `],
  standalone: false
})
export class SelecionarPacienteComponent implements OnInit {
  @Input() pacientes: Paciente[] = [];
  @Input() titulo = 'Selecionar paciente';
  @Input() subtitulo?: string;

  public filtrados: Paciente[] = [];

  constructor(private modalCtrl: ModalController) {}

  ngOnInit() {
    this.pacientes = [...(this.pacientes ?? [])].sort((a, b) => (a.nome ?? '').localeCompare(b.nome ?? '', 'pt-BR'));
    this.filtrados = this.pacientes;
  }

  filtrar(termo: string | null | undefined) {
    this.filtrados = this.pacientes.filter(p => correspondeBusca(termo ?? '', p.nome, p.cpf));
  }

  iniciais(nome: string | undefined): string {
    const partes = (nome ?? '').trim().split(/\s+/).filter(Boolean);
    if (partes.length === 0) return '?';
    const primeira = partes[0][0];
    const ultima = partes.length > 1 ? partes[partes.length - 1][0] : '';
    return (primeira + ultima).toUpperCase();
  }

  selecionar(paciente: Paciente) {
    this.modalCtrl.dismiss(paciente.id, 'selecionado');
  }

  fechar() {
    this.modalCtrl.dismiss(null, 'cancel');
  }
}
