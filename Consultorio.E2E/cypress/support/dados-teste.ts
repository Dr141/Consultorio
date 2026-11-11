/**
 * Geradores de dados únicos por execução.
 * Como cadastros e registros de prontuário não são mais apagados (apenas inativados,
 * anulados ou cancelados), cada execução usa dados próprios para não colidir com as anteriores.
 */

/** Sufixo curto e único por execução (ex.: "482913"). */
export const sufixoExecucao = (): string => Date.now().toString().slice(-6);

/** Gera um CPF com dígitos verificadores válidos (somente números). */
export function gerarCpfValido(): string {
  const base = Array.from({ length: 9 }, () => Math.floor(Math.random() * 10));
  if (base.every(d => d === base[0])) base[8] = (base[8] + 1) % 10;

  const digito = (numeros: number[], pesoInicial: number) => {
    const soma = numeros.reduce((acc, n, i) => acc + n * (pesoInicial - i), 0);
    const resto = soma % 11;
    return resto < 2 ? 0 : 11 - resto;
  };

  const d1 = digito(base, 10);
  const d2 = digito([...base, d1], 11);
  return [...base, d1, d2].join('');
}

export const formatarCpf = (cpf: string): string =>
  cpf.replace(/^(\d{3})(\d{3})(\d{3})(\d{2})$/, '$1.$2.$3-$4');
