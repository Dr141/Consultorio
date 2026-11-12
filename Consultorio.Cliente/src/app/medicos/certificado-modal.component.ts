import { ChangeDetectorRef, Component, Input, OnInit } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { ConsultorioApiService } from '../services/api/consultorio-api.service';
import { FeedbackService } from '../services/feedback/feedback.service';
import { CertificadoDigital, Medico } from '../types/types';

/** Cadastro do certificado digital A1 (.pfx/.p12) usado para assinar receitas e atestados em PDF. */
@Component({
  selector: 'app-certificado-modal',
  templateUrl: './certificado-modal.component.html',
  styleUrls: ['./certificado-modal.component.scss'],
  standalone: false
})
export class CertificadoModalComponent implements OnInit {
  @Input() medico!: Medico;

  certificado: CertificadoDigital | null = null;
  carregando = true;
  enviando = false;
  arquivo: File | null = null;
  senha = '';

  constructor(
    private modalCtrl: ModalController,
    private api: ConsultorioApiService,
    private feedback: FeedbackService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.api.obterCertificado(this.medico.id).subscribe({
      next: (certificado) => {
        this.certificado = certificado ?? null;
        this.carregando = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.carregando = false;
        this.cdr.detectChanges();
        this.feedback.erro(err, 'Não foi possível consultar o certificado.');
      }
    });
  }

  selecionar(evento: Event) {
    const entrada = evento.target as HTMLInputElement;
    const arquivo = entrada.files?.[0] ?? null;
    if (arquivo && !/\.(pfx|p12)$/i.test(arquivo.name)) {
      this.feedback.aviso('Selecione o arquivo do certificado A1 (.pfx ou .p12).');
      entrada.value = '';
      return;
    }
    this.arquivo = arquivo;
    this.cdr.detectChanges();
  }

  enviar() {
    if (!this.arquivo || !this.senha || this.enviando) return;
    this.enviando = true;
    this.api.enviarCertificado(this.medico.id, this.arquivo, this.senha).subscribe({
      next: (certificado) => {
        this.certificado = certificado;
        this.arquivo = null;
        this.senha = '';
        this.enviando = false;
        this.cdr.detectChanges();
        this.feedback.sucesso(certificado.icpBrasil
          ? 'Certificado cadastrado. Os documentos deste médico já podem ser assinados digitalmente.'
          : 'Certificado cadastrado, mas ele não é da ICP-Brasil: as assinaturas não terão validade jurídica.');
      },
      error: (err) => {
        this.enviando = false;
        this.cdr.detectChanges();
        this.feedback.erro(err, 'Não foi possível cadastrar o certificado.');
      }
    });
  }

  async remover() {
    const confirmado = await this.feedback.confirmar({
      titulo: 'Remover certificado?',
      mensagem: 'O arquivo e a senha serão apagados do servidor. Documentos já assinados continuam válidos.',
      textoConfirmar: 'Remover',
      destrutivo: true
    });
    if (!confirmado) return;

    this.api.removerCertificado(this.medico.id).subscribe({
      next: () => {
        this.certificado = null;
        this.cdr.detectChanges();
        this.feedback.sucesso('Certificado removido.');
      },
      error: (err) => this.feedback.erro(err, 'Não foi possível remover o certificado.')
    });
  }

  fechar() {
    this.modalCtrl.dismiss();
  }
}
