import { Pipe, PipeTransform } from '@angular/core';
import { SinaisVitaisLike, formatarCpf, formatarTamanhoArquivo, formatarTelefone, linhas, paraDataLocal, resumirSinaisVitais } from '../utils/formatadores';

@Pipe({ name: 'cpf', standalone: false })
export class CpfPipe implements PipeTransform {
  transform(valor: string | null | undefined): string {
    return valor ? formatarCpf(valor) : '';
  }
}

@Pipe({ name: 'telefone', standalone: false })
export class TelefonePipe implements PipeTransform {
  transform(valor: string | null | undefined): string {
    return valor ? formatarTelefone(valor) : '';
  }
}

/**
 * Exibe datas em pt-BR.
 * - 'data' (padrão): campos somente-data (nascimento, dia da agenda) sem deslocamento de fuso.
 * - 'dataHora': instantes reais (ex.: data de criação), convertidos para o fuso local.
 * - 'extenso': ex.: "terça-feira, 10 de novembro de 2026".
 */
@Pipe({ name: 'dataBr', standalone: false })
export class DataBrPipe implements PipeTransform {
  transform(valor: string | Date | null | undefined, modo: 'data' | 'dataHora' | 'extenso' = 'data'): string {
    if (!valor) return '';
    if (modo === 'dataHora') {
      const d = valor instanceof Date ? valor : new Date(valor);
      if (isNaN(d.getTime())) return '';
      return d.toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
    }
    const d = paraDataLocal(valor);
    if (!d) return '';
    if (modo === 'extenso') {
      return d.toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
    }
    return d.toLocaleDateString('pt-BR');
  }
}

/** Idade em anos a partir de uma data de nascimento. */
@Pipe({ name: 'idade', standalone: false })
export class IdadePipe implements PipeTransform {
  transform(valor: string | null | undefined): string {
    const nasc = paraDataLocal(valor);
    if (!nasc) return '';
    const hoje = new Date();
    let anos = hoje.getFullYear() - nasc.getFullYear();
    const aindaNaoFezAniversario =
      hoje.getMonth() < nasc.getMonth() || (hoje.getMonth() === nasc.getMonth() && hoje.getDate() < nasc.getDate());
    if (aindaNaoFezAniversario) anos--;
    if (anos < 0) return '';
    return anos === 1 ? '1 ano' : `${anos} anos`;
  }
}

/** Sinais vitais preenchidos em forma de lista curta ("PA 120/80 mmHg", "FC 72 bpm"...). */
@Pipe({ name: 'sinaisVitais', standalone: false })
export class SinaisVitaisPipe implements PipeTransform {
  transform(valor: SinaisVitaisLike | null | undefined): string[] {
    return resumirSinaisVitais(valor);
  }
}

/** Texto "um item por linha" convertido em lista. */
@Pipe({ name: 'linhas', standalone: false })
export class LinhasPipe implements PipeTransform {
  transform(valor: string | null | undefined): string[] {
    return linhas(valor);
  }
}

@Pipe({ name: 'tamanhoArquivo', standalone: false })
export class TamanhoArquivoPipe implements PipeTransform {
  transform(valor: number | null | undefined): string {
    return formatarTamanhoArquivo(valor);
  }
}
