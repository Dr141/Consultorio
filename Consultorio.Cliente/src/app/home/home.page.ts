import { Component, OnInit } from '@angular/core';
import { ModalController, NavController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { AtualizarSenhaComponent } from '../components/atualizar-senha/atualizar-senha.component';
import { RoleService } from '../services/role/role.service';
import { CacheService } from '../services/cache/cache.service';
import { ThemeService } from '../services/theme/theme.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { catchError, forkJoin, of } from 'rxjs';
import { mesmoDia, paraDataLocal } from '../shared/utils/formatadores';

export interface ResumoDia {
  pacientes: number;
  ocupadosHoje: number;
  livresHoje: number;
  examesPendentes: number;
}

@Component({
  selector: 'app-home',
  templateUrl: 'home.page.html',
  styleUrls: ['home.page.scss'],
  standalone: false
})
export class HomePage implements OnInit {
  public resumo?: ResumoDia;
  public saudacao = '';
  public hojeExtenso = '';

  constructor(
    private modalController: ModalController,
    private api: ConsultorioApiService,
    private role: RoleService,
    private navCtrl: NavController,
    private cache: CacheService,
    public themeService: ThemeService,
    private feedback: FeedbackService
  ) { }

  ngOnInit(): void {
    this.ehAdmin();
  }

  ionViewWillEnter(): void {
    const agora = new Date();
    const hora = agora.getHours();
    this.saudacao = hora < 12 ? 'Bom dia!' : hora < 18 ? 'Boa tarde!' : 'Boa noite!';
    const extenso = agora.toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
    this.hojeExtenso = extenso.charAt(0).toUpperCase() + extenso.slice(1);
    this.carregarResumo();
  }

  /** Números do dia exibidos no topo do painel. Falhas isoladas não impedem o restante. */
  carregarResumo(): void {
    forkJoin({
      pacientes: this.api.obterPacientes().pipe(catchError(() => of(null))),
      agendas: this.api.obterAgendas().pipe(catchError(() => of(null))),
      exames: this.api.obterExames().pipe(catchError(() => of(null)))
    }).subscribe(({ pacientes, agendas, exames }) => {
      if (pacientes === null && agendas === null && exames === null) return;
      const hoje = new Date();
      const deHoje = (agendas ?? []).filter(a => {
        const dia = paraDataLocal(a.data);
        return !!dia && mesmoDia(dia, hoje);
      });
      this.resumo = {
        pacientes: pacientes?.length ?? 0,
        ocupadosHoje: deHoje.filter(a => !a.disponivel).length,
        livresHoje: deHoje.filter(a => a.disponivel).length,
        examesPendentes: (exames ?? []).filter(e => !e.resultado && !e.cancelado).length
      };
    });
  }

  toggleTheme() {
    this.themeService.toggleTheme();
  }

  async logOut() {
    const confirmado = await this.feedback.confirmar({
      titulo: 'Sair do sistema?',
      mensagem: 'Sua sessão será encerrada neste dispositivo.',
      textoConfirmar: 'Sair'
    });
    if (!confirmado) return;

    this.cache.RemoveTodosCookies();
    this.api.logout().subscribe({
      next: () => {
        this.navCtrl.navigateRoot('/login');
      },
      error: () => {
        this.navCtrl.navigateRoot('/login');
      }
    });
  }

  users() {
    (document.activeElement as HTMLElement)?.blur();
    this.navCtrl.navigateForward('/users');
  }

  async atualizarSenha() {
    (document.activeElement as HTMLElement)?.blur();
    const modal = await this.modalController.create({
      component: AtualizarSenhaComponent,
      componentProps: {
        api: this.api
      }
    });

    await modal.present();
  }

  public ehAdmin(): boolean {
    return this.role.ehAdmin();
  }

  public ehClinico(): boolean {
    return this.role.ehClinico();
  }

  public recepcaoNav() {
    (document.activeElement as HTMLElement)?.blur();
    this.navCtrl.navigateForward('/recepcao');
  }

  public auditoriaNav() {
    (document.activeElement as HTMLElement)?.blur();
    this.navCtrl.navigateForward('/auditoria');
  }

  public paciente() {
    (document.activeElement as HTMLElement)?.blur();
    this.navCtrl.navigateForward('/pacientes');
  }

  public agenda() {
    (document.activeElement as HTMLElement)?.blur();
    this.navCtrl.navigateForward('/agenda');
  }

  public consulta() {
    (document.activeElement as HTMLElement)?.blur();
    this.navCtrl.navigateForward('/consultas');
  }

  public exame() {
    (document.activeElement as HTMLElement)?.blur();
    this.navCtrl.navigateForward('/exames');
  }

  public medico() {
    (document.activeElement as HTMLElement)?.blur();
    this.navCtrl.navigateForward('/medicos');
  }

  public prontuarioNav() {
    (document.activeElement as HTMLElement)?.blur();
    this.navCtrl.navigateForward('/prontuario');
  }
}
