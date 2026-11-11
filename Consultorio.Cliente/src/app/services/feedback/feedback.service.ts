import { Injectable } from '@angular/core';
import { AlertController, ToastController } from '@ionic/angular';

export interface OpcoesConfirmacao {
  titulo: string;
  mensagem: string;
  textoConfirmar?: string;
  textoCancelar?: string;
  /** Ações irreversíveis (exclusões) ganham destaque em vermelho. */
  destrutivo?: boolean;
}

/**
 * Centraliza mensagens ao usuário (toasts de sucesso/erro e confirmações),
 * garantindo o mesmo visual, duração e comportamento em todas as telas.
 */
@Injectable({
  providedIn: 'root'
})
export class FeedbackService {
  private toastAtual?: HTMLIonToastElement;

  constructor(private toastCtrl: ToastController, private alertCtrl: AlertController) {}

  sucesso(mensagem: string): Promise<void> {
    return this.exibir(mensagem, 'success', 'checkmark-circle-outline', 2500);
  }

  aviso(mensagem: string): Promise<void> {
    return this.exibir(mensagem, 'warning', 'warning-outline', 3500);
  }

  /**
   * Exibe um erro. Aceita string ou o erro retornado pela API.
   * Erros 401 são ignorados porque o interceptor já redireciona para o login.
   */
  erro(erro: unknown, mensagemPadrao = 'Não foi possível concluir a operação. Tente novamente.'): Promise<void> {
    const status = (erro as { status?: number } | null)?.status;
    if (status === 401) return Promise.resolve();
    const mensagem = typeof erro === 'string'
      ? erro
      : (erro as { message?: string } | null)?.message || mensagemPadrao;
    return this.exibir(mensagem, 'danger', 'alert-circle-outline', 5000);
  }

  async confirmar(opcoes: OpcoesConfirmacao): Promise<boolean> {
    const alerta = await this.alertCtrl.create({
      header: opcoes.titulo,
      message: opcoes.mensagem,
      cssClass: opcoes.destrutivo ? 'alerta-destrutivo' : undefined,
      buttons: [
        { text: opcoes.textoCancelar ?? 'Cancelar', role: 'cancel' },
        {
          text: opcoes.textoConfirmar ?? 'Confirmar',
          role: opcoes.destrutivo ? 'destructive' : 'confirm'
        }
      ]
    });

    await alerta.present();
    const { role } = await alerta.onDidDismiss();
    return role === 'destructive' || role === 'confirm';
  }

  /**
   * Pede um texto curto ao usuário (ex.: motivo de anulação, adendo).
   * Retorna o texto informado ou null se o usuário cancelar. Valida o tamanho mínimo antes de fechar.
   */
  async solicitarTexto(opcoes: {
    titulo: string;
    mensagem?: string;
    placeholder?: string;
    textoConfirmar?: string;
    minimo?: number;
    maximo?: number;
    destrutivo?: boolean;
  }): Promise<string | null> {
    const minimo = opcoes.minimo ?? 5;
    const maximo = opcoes.maximo ?? 500;
    let erroAtual = '';

    const alerta = await this.alertCtrl.create({
      header: opcoes.titulo,
      message: opcoes.mensagem,
      cssClass: opcoes.destrutivo ? 'alerta-destrutivo alerta-texto' : 'alerta-texto',
      inputs: [{
        name: 'texto',
        type: 'textarea',
        placeholder: opcoes.placeholder ?? '',
        attributes: { maxlength: maximo, rows: 4 }
      }],
      buttons: [
        { text: 'Cancelar', role: 'cancel' },
        {
          text: opcoes.textoConfirmar ?? 'Confirmar',
          role: opcoes.destrutivo ? 'destructive' : 'confirm',
          handler: (valores: { texto?: string }) => {
            const texto = (valores?.texto ?? '').trim();
            if (texto.length < minimo) {
              erroAtual = `Escreva pelo menos ${minimo} caracteres.`;
              alerta.message = `${opcoes.mensagem ? opcoes.mensagem + ' ' : ''}⚠ ${erroAtual}`;
              return false; // mantém o diálogo aberto
            }
            return { values: { texto } };
          }
        }
      ]
    });

    await alerta.present();
    const { role, data } = await alerta.onDidDismiss<{ values?: { texto?: string } }>();
    if (role !== 'confirm' && role !== 'destructive') return null;
    return (data?.values?.texto ?? '').trim() || null;
  }

  private async exibir(message: string, color: string, icon: string, duration: number): Promise<void> {
    // Evita empilhar vários toasts: o mais recente substitui o anterior.
    await this.toastAtual?.dismiss().catch(() => undefined);

    const toast = await this.toastCtrl.create({
      message,
      color,
      icon,
      duration,
      position: 'bottom',
      swipeGesture: 'vertical',
      cssClass: 'app-toast',
      buttons: [{ icon: 'close-outline', role: 'cancel', side: 'end' }]
    });

    this.toastAtual = toast;
    await toast.present();
  }
}
