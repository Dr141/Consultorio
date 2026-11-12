/**
 * Funções utilitárias de formatação e datas usadas em toda a aplicação.
 */

export function somenteDigitos(valor: string | number | null | undefined): string {
  return (valor ?? '').toString().replace(/\D/g, '');
}

/** Formata progressivamente um CPF: 000.000.000-00 */
export function formatarCpf(valor: string | null | undefined): string {
  const d = somenteDigitos(valor).slice(0, 11);
  if (d.length <= 3) return d;
  if (d.length <= 6) return `${d.slice(0, 3)}.${d.slice(3)}`;
  if (d.length <= 9) return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6)}`;
  return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`;
}

/** Formata progressivamente um telefone: (00) 0000-0000 ou (00) 00000-0000 */
export function formatarTelefone(valor: string | null | undefined): string {
  const d = somenteDigitos(valor).slice(0, 11);
  if (!d) return '';
  if (d.length <= 2) return `(${d}`;
  if (d.length <= 6) return `(${d.slice(0, 2)}) ${d.slice(2)}`;
  if (d.length <= 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
}

/** Remove acentos e caixa para buscas tolerantes ("joao" encontra "João"). */
export function normalizarTexto(valor: string | null | undefined): string {
  return (valor ?? '')
    .toString()
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .trim();
}

/**
 * Verifica se o termo de busca casa com algum dos campos.
 * Compara texto normalizado e, quando o termo tem dígitos, também só os dígitos (CPF/telefone com ou sem pontuação).
 */
export function correspondeBusca(termo: string, ...campos: (string | null | undefined)[]): boolean {
  const termoNormalizado = normalizarTexto(termo);
  if (!termoNormalizado) return true;
  const termoDigitos = somenteDigitos(termo);
  const termoSoNumerico = termoDigitos.length > 0 && termoDigitos.length === termoNormalizado.replace(/[\s.\-()/]/g, '').length;

  return campos.some(campo => {
    if (!campo) return false;
    if (normalizarTexto(campo).includes(termoNormalizado)) return true;
    return termoSoNumerico && somenteDigitos(campo).includes(termoDigitos);
  });
}

function doisDigitos(n: number): string {
  return n.toString().padStart(2, '0');
}

/**
 * Converte um campo "somente data" vindo da API (ex.: '2026-11-10' ou '2026-11-10T00:00:00Z')
 * em uma data local, sem o deslocamento de fuso que fazia o dia aparecer um a menos no Brasil.
 */
export function paraDataLocal(valor: string | Date | null | undefined): Date | null {
  if (!valor) return null;
  if (valor instanceof Date) return isNaN(valor.getTime()) ? null : valor;
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(valor);
  if (m) return new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3]));
  const d = new Date(valor);
  return isNaN(d.getTime()) ? null : d;
}

/** Valor no formato aceito por <input type="date"> (YYYY-MM-DD), sem deslocamento de fuso. */
export function paraInputData(valor: string | Date | null | undefined): string {
  const d = paraDataLocal(valor);
  if (!d) return '';
  return `${d.getFullYear()}-${doisDigitos(d.getMonth() + 1)}-${doisDigitos(d.getDate())}`;
}

/** Data de hoje (fuso local) no formato YYYY-MM-DD. */
export function hojeInputData(): string {
  return paraInputData(new Date());
}

/** Data + hora de um horário da agenda (data somente-data + 'HH:mm:ss') como Date local. */
export function combinarDataHora(data: string | null | undefined, hora: string | null | undefined): Date | null {
  const d = paraDataLocal(data);
  if (!d) return null;
  const [h, min] = (hora ?? '00:00').split(':').map(Number);
  d.setHours(h || 0, min || 0, 0, 0);
  return d;
}

export function mesmoDia(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}

/** Rótulo amigável para um dia: "Hoje", "Amanhã", "Ontem" ou "seg., 10 de nov. de 2026". */
export function rotuloDia(data: Date): string {
  const hoje = new Date();
  const amanha = new Date(hoje.getFullYear(), hoje.getMonth(), hoje.getDate() + 1);
  const ontem = new Date(hoje.getFullYear(), hoje.getMonth(), hoje.getDate() - 1);
  if (mesmoDia(data, hoje)) return 'Hoje';
  if (mesmoDia(data, amanha)) return 'Amanhã';
  if (mesmoDia(data, ontem)) return 'Ontem';
  return data.toLocaleDateString('pt-BR', { weekday: 'short', day: '2-digit', month: 'short', year: 'numeric' });
}

/** Campos mínimos de sinais vitais usados nos resumos (aceita o tipo da API ou valores do formulário). */
export type SinaisVitaisLike = {
  pressaoSistolica?: number | null;
  pressaoDiastolica?: number | null;
  frequenciaCardiaca?: number | null;
  frequenciaRespiratoria?: number | null;
  temperatura?: number | null;
  saturacaoO2?: number | null;
  pesoKg?: number | null;
  alturaCm?: number | null;
  glicemia?: number | null;
  imc?: number | null;
};

/** IMC com uma casa decimal, ou null se faltar peso/altura. */
export function calcularImc(pesoKg: number | null | undefined, alturaCm: number | null | undefined): number | null {
  const peso = Number(pesoKg);
  const altura = Number(alturaCm);
  if (!peso || !altura || peso <= 0 || altura <= 0) return null;
  const metros = altura / 100;
  return Math.round((peso / (metros * metros)) * 10) / 10;
}

/** Classificação do IMC para adultos (OMS). */
export function classificarImc(imc: number | null | undefined): string {
  if (imc === null || imc === undefined || !isFinite(imc)) return '';
  if (imc < 18.5) return 'Abaixo do peso';
  if (imc < 25) return 'Peso adequado';
  if (imc < 30) return 'Sobrepeso';
  if (imc < 35) return 'Obesidade grau I';
  if (imc < 40) return 'Obesidade grau II';
  return 'Obesidade grau III';
}

function preenchido(v: number | null | undefined): v is number {
  return v !== null && v !== undefined && `${v}` !== '' && !isNaN(Number(v));
}

/** Lista legível dos sinais vitais preenchidos ("PA 120/80 mmHg", "FC 72 bpm"...). */
export function resumirSinaisVitais(sv: SinaisVitaisLike | null | undefined): string[] {
  if (!sv) return [];
  const itens: string[] = [];
  const decimal = (v: number, casas = 1) => Number(v).toLocaleString('pt-BR', { maximumFractionDigits: casas });
  if (preenchido(sv.pressaoSistolica) && preenchido(sv.pressaoDiastolica)) itens.push(`PA ${sv.pressaoSistolica}/${sv.pressaoDiastolica} mmHg`);
  if (preenchido(sv.frequenciaCardiaca)) itens.push(`FC ${sv.frequenciaCardiaca} bpm`);
  if (preenchido(sv.frequenciaRespiratoria)) itens.push(`FR ${sv.frequenciaRespiratoria} irpm`);
  if (preenchido(sv.temperatura)) itens.push(`T ${decimal(sv.temperatura)} °C`);
  if (preenchido(sv.saturacaoO2)) itens.push(`SpO₂ ${sv.saturacaoO2}%`);
  if (preenchido(sv.pesoKg)) itens.push(`Peso ${decimal(sv.pesoKg, 2)} kg`);
  if (preenchido(sv.alturaCm)) itens.push(`Altura ${sv.alturaCm} cm`);
  const imc = preenchido(sv.imc) ? sv.imc : calcularImc(sv.pesoKg, sv.alturaCm);
  if (preenchido(imc)) itens.push(`IMC ${decimal(imc)}`);
  if (preenchido(sv.glicemia)) itens.push(`Glicemia ${sv.glicemia} mg/dL`);
  return itens;
}

/** Quebra um texto de "um item por linha" em lista (alergias, medicamentos, condições). */
export function linhas(valor: string | null | undefined): string[] {
  return (valor ?? '').split('\n').map(l => l.trim()).filter(l => l.length > 0);
}

/** Tamanho de arquivo legível (KB/MB). */
export function formatarTamanhoArquivo(bytes: number | null | undefined): string {
  const b = Number(bytes) || 0;
  if (b < 1024) return `${b} B`;
  if (b < 1024 * 1024) return `${(b / 1024).toLocaleString('pt-BR', { maximumFractionDigits: 0 })} KB`;
  return `${(b / (1024 * 1024)).toLocaleString('pt-BR', { maximumFractionDigits: 1 })} MB`;
}
