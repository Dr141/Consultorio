import { TipoDocumento } from '../types/types';

/** Dados usados para preencher os modelos de texto. */
export type ContextoModelo = {
  nomePaciente?: string | null;
  dias?: number | null;
  data?: Date;
};

const dataBr = (d: Date) => d.toLocaleDateString('pt-BR');

/**
 * Modelos iniciais de texto por tipo de documento. São só ponto de partida:
 * o médico revisa e completa antes de emitir.
 */
export function modeloDocumento(tipo: TipoDocumento, ctx: ContextoModelo): string {
  const paciente = ctx.nomePaciente?.trim() || '____________________';
  const data = dataBr(ctx.data ?? new Date());
  const dias = ctx.dias && ctx.dias > 0 ? ctx.dias : null;

  switch (tipo) {
    case 'Receita':
      return 'Uso oral\n\n1. ____________________ ___ mg\n   Tomar 1 comprimido de ___ em ___ horas, por ___ dias.\n\n2. ____________________\n   ';
    case 'ReceitaControleEspecial':
      return 'Uso oral\n\n1. ____________________ ___ mg — quantidade: ___ (___________ por extenso)\n   Tomar ___ comprimido(s) ___________, por ___ dias.\n';
    case 'Atestado':
      return `Atesto, para os devidos fins, que o(a) paciente ${paciente} esteve sob meus cuidados profissionais em ${data}, `
        + `necessitando de afastamento de suas atividades por ${dias ?? '___'} dia(s) a partir desta data.`;
    case 'DeclaracaoComparecimento':
      return `Declaro, para os devidos fins, que ${paciente} compareceu a este consultório em ${data}, `
        + 'no período das ___:___ às ___:___, para atendimento médico.';
    case 'PedidoExames':
      return 'Hemograma completo\nGlicemia de jejum\n';
    case 'Encaminhamento':
      return `Encaminho o(a) paciente ${paciente} para avaliação e conduta com especialista em ____________________.\n\n`
        + 'Motivo do encaminhamento: \n\nHistórico relevante: \n\nGrato(a) pela atenção.';
    case 'Relatorio':
      return `Paciente ${paciente}, em acompanhamento neste consultório.\n\nHistórico clínico: \n\nExames relevantes: \n\nConduta atual: \n\nPrognóstico / recomendações: `;
  }
}

/** Dica exibida abaixo do campo de conteúdo. */
export function dicaConteudo(tipo: TipoDocumento): string {
  switch (tipo) {
    case 'PedidoExames': return 'Um exame por linha. Cada linha vira um item do pedido.';
    case 'ReceitaControleEspecial': return 'Impressa em duas vias (farmácia e paciente). Informe a quantidade também por extenso.';
    case 'Atestado': return 'O CID só é impresso com autorização do paciente.';
    default: return '';
  }
}
