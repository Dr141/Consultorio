import { Directive, HostListener, Input, Optional, Self } from '@angular/core';
import { NgControl } from '@angular/forms';
import { formatarCpf, formatarTelefone } from '../utils/formatadores';

/**
 * Aplica máscara enquanto o usuário digita em um <ion-input>.
 * Uso: <ion-input appMascara="cpf" formControlName="cpf"></ion-input>
 */
@Directive({
  selector: '[appMascara]',
  standalone: false
})
export class MascaraDirective {
  @Input('appMascara') tipo: 'cpf' | 'telefone' = 'cpf';

  constructor(@Optional() @Self() private ngControl: NgControl | null) {}

  @HostListener('ionInput', ['$event'])
  aoDigitar(evento: Event) {
    const alvo = evento.target as (HTMLElement & { value?: string | null }) | null;
    const detalhe = (evento as CustomEvent<{ value?: string | null }>).detail;
    const bruto = (detalhe?.value ?? alvo?.value ?? '').toString();
    const formatado = this.tipo === 'telefone' ? formatarTelefone(bruto) : formatarCpf(bruto);

    // Executa após o value accessor do Ionic para que o valor final do controle seja o formatado.
    queueMicrotask(() => {
      if (alvo && alvo.value !== formatado) alvo.value = formatado;
      const controle = this.ngControl?.control;
      if (controle && controle.value !== formatado) {
        controle.setValue(formatado);
        controle.markAsDirty();
      }
    });
  }
}
