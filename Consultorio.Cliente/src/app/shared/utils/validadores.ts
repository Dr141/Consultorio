import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { somenteDigitos } from './formatadores';

/** Mesmo algoritmo de dígitos verificadores usado no backend (Cpf.ValidarCpf). */
export function cpfValido(valor: string | null | undefined): boolean {
  const cpf = somenteDigitos(valor);
  if (cpf.length !== 11 || /^(\d)\1{10}$/.test(cpf)) return false;

  const calcularDigito = (base: string, pesoInicial: number) => {
    let soma = 0;
    for (let i = 0; i < base.length; i++) soma += Number(base[i]) * (pesoInicial - i);
    const resto = soma % 11;
    return resto < 2 ? 0 : 11 - resto;
  };

  const d1 = calcularDigito(cpf.slice(0, 9), 10);
  const d2 = calcularDigito(cpf.slice(0, 9) + d1, 11);
  return cpf.endsWith(`${d1}${d2}`);
}

export function cpfValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) return null;
    return cpfValido(control.value) ? null : { cpf: true };
  };
}

export function telefoneValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) return null;
    const total = somenteDigitos(control.value).length;
    return total === 10 || total === 11 ? null : { telefone: true };
  };
}

/** Impede datas futuras (ex.: data de nascimento). Espera valor YYYY-MM-DD. */
export function dataNaoFuturaValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) return null;
    const hoje = new Date();
    const hojeStr = `${hoje.getFullYear()}-${String(hoje.getMonth() + 1).padStart(2, '0')}-${String(hoje.getDate()).padStart(2, '0')}`;
    return String(control.value) > hojeStr ? { dataFutura: true } : null;
  };
}

/** Mensagem amigável para o primeiro erro de um campo (usada no errorText dos inputs). */
export function mensagemErro(controle: AbstractControl | null | undefined): string {
  const erros = controle?.errors;
  if (!erros) return '';
  if (erros['required']) return 'Campo obrigatório.';
  if (erros['minlength']) return `Use pelo menos ${erros['minlength'].requiredLength} caracteres.`;
  if (erros['maxlength']) return `Use no máximo ${erros['maxlength'].requiredLength} caracteres.`;
  if (erros['email']) return 'Informe um e-mail válido.';
  if (erros['cpf']) return 'CPF inválido. Confira os 11 dígitos.';
  if (erros['telefone']) return 'Informe DDD + número (10 ou 11 dígitos).';
  if (erros['dataFutura']) return 'A data não pode estar no futuro.';
  return 'Valor inválido.';
}

/**
 * Verdadeiro quando ainda falta preencher algum campo obrigatório.
 * Usado para manter o botão salvar desabilitado só nesse caso; erros de formato
 * (CPF, telefone...) são explicados ao clicar em salvar, em vez de deixar o botão "travado" sem motivo aparente.
 */
export function possuiObrigatoriosPendentes(form: { controls: Record<string, AbstractControl> } | null | undefined): boolean {
  if (!form) return true;
  return Object.values(form.controls).some(c => c.enabled && c.hasError('required'));
}
