export type usuario = {
  email: string;
  emailConfirmado: boolean;
  claims: any[];
  roles: any[];
};

export enum Especialidade {
  ClinicoGeral = 1,
  Cardiologista = 2,
  Urologista = 3,
  Ginecologista = 4,
  Ortopedista = 5,
  Pediatra = 6,
  Dermatologista = 7,
  Neurologista = 8,
  Oftalmologista = 9
}

export type Pessoa = {
  id: number;
  nome: string;
  cpf: string;
  dtNasc: string;
  telefone: string;
  observacao?: string;
  dtCriacao?: string;
};

export type Paciente = {
  id: number;
  idPessoa: number;
  nome: string;
  cpf: string;
  dtNasc: string;
  telefone: string;
  observacao?: string;
  dtCriacao?: string;
  ativo?: boolean;
  dtInativacao?: string | null;
  email?: string | null;
  alergias?: string | null;
  medicamentosEmUso?: string | null;
  condicoesCronicas?: string | null;
  idConvenio?: number | null;
  nomeConvenio?: string | null;
  numeroCarteirinha?: string | null;
  validadeCarteirinha?: string | null;
};

export type Medico = {
  id: number;
  idPessoa: number;
  nome: string;
  cpf: string;
  dtNasc: string;
  telefone: string;
  crm: string;
  especialidades: Especialidade[];
  observacao?: string;
  dtCriacao?: string;
  ativo?: boolean;
  emailUsuario?: string | null;
};

export type StatusAgendamento = 'Agendado' | 'Confirmado' | 'Aguardando' | 'EmAtendimento' | 'Atendido' | 'Faltou';
export type TipoAtendimento = 'Consulta' | 'Retorno' | 'Exame' | 'Procedimento';

export const TIPOS_ATENDIMENTO: { valor: number; nome: TipoAtendimento }[] = [
  { valor: 1, nome: 'Consulta' },
  { valor: 2, nome: 'Retorno' },
  { valor: 3, nome: 'Exame' },
  { valor: 4, nome: 'Procedimento' }
];

export type Agenda = {
  id: number;
  data: string;
  hora: string;
  idMedico?: number;
  nomeMedico?: string;
  idPaciente?: number;
  nomePaciente?: string;
  disponivel: boolean;
  idConsulta?: number;
  idExame?: number;
  dtCriacao?: string;
  status?: StatusAgendamento | null;
  tipo?: TipoAtendimento;
  duracaoMinutos?: number;
  dtConfirmacao?: string | null;
  dtChegada?: string | null;
  dtInicioAtendimento?: string | null;
  dtFimAtendimento?: string | null;
  lembreteEnviadoEm?: string | null;
  telefonePaciente?: string | null;
  emailPaciente?: string | null;
  temConvenio?: boolean;
  carteirinhaVencida?: boolean;
};

export type BloqueioAgenda = {
  id: number;
  idMedico?: number | null;
  nomeMedico?: string | null;
  dataInicio: string;
  dataFim: string;
  motivo: string;
  dtCriacao: string;
};

export type BloqueioCriado = {
  bloqueio: BloqueioAgenda;
  horariosLivresRemovidos: number;
  conflitos: Agenda[];
};

export type GradeRequest = {
  idMedico: number;
  dataInicio: string;
  dataFim: string;
  diasSemana: number[];
  horaInicio: string;
  horaFim: string;
  intervaloMinutos: number;
  pausaInicio?: string | null;
  pausaFim?: string | null;
  tipo?: number | null;
};

export type GradeResultado = {
  criados: number;
  ignoradosExistentes: number;
  ignoradosBloqueio: number;
};

export type StatusConsulta = 'Aberta' | 'Finalizada' | 'Anulada';

export type ConsultaAdendo = {
  id: number;
  texto: string;
  autor: string;
  dtCriacao: string;
};

export type Consulta = {
  id: number;
  idPaciente: number;
  nomePaciente?: string;
  idMedico: number;
  nomeMedico?: string;
  observacao: string;
  idAgenda?: number;
  dataHora?: string;
  dtCriacao?: string;
  status?: StatusConsulta;
  dtFinalizacao?: string | null;
  dtAnulacao?: string | null;
  motivoAnulacao?: string | null;
  adendos?: ConsultaAdendo[];
  subjetivo?: string | null;
  objetivo?: string | null;
  avaliacao?: string | null;
  plano?: string | null;
  cidCodigo?: string | null;
  cidDescricao?: string | null;
  sinaisVitais?: SinaisVitais | null;
  retornoEm?: string | null;
};

export type SinaisVitais = {
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

export type Cid = { codigo: string; descricao: string };

/** Evolução clínica enviada ao criar/atualizar uma consulta. */
export type EvolucaoClinica = {
  observacao?: string | null;
  subjetivo?: string | null;
  objetivo?: string | null;
  avaliacao?: string | null;
  plano?: string | null;
  cidCodigo?: string | null;
  cidDescricao?: string | null;
  sinaisVitais?: SinaisVitais | null;
  retornoEm?: string | null;
};

export type AlertasClinicos = {
  alergias: string | null;
  medicamentosEmUso: string | null;
  condicoesCronicas: string | null;
};

export type TipoDocumento =
  | 'Receita' | 'ReceitaControleEspecial' | 'Atestado' | 'DeclaracaoComparecimento'
  | 'PedidoExames' | 'Encaminhamento' | 'Relatorio';

export const TIPOS_DOCUMENTO: { valor: number; tipo: TipoDocumento; nome: string; icone: string }[] = [
  { valor: 1, tipo: 'Receita', nome: 'Receita simples', icone: 'medkit-outline' },
  { valor: 2, tipo: 'ReceitaControleEspecial', nome: 'Receita de controle especial', icone: 'shield-checkmark-outline' },
  { valor: 3, tipo: 'Atestado', nome: 'Atestado médico', icone: 'bed-outline' },
  { valor: 4, tipo: 'DeclaracaoComparecimento', nome: 'Declaração de comparecimento', icone: 'time-outline' },
  { valor: 5, tipo: 'PedidoExames', nome: 'Pedido de exames', icone: 'flask-outline' },
  { valor: 6, tipo: 'Encaminhamento', nome: 'Encaminhamento', icone: 'arrow-redo-outline' },
  { valor: 7, tipo: 'Relatorio', nome: 'Relatório médico', icone: 'document-text-outline' }
];

export type DocumentoRequest = {
  tipo: number;
  idPaciente: number;
  conteudo: string;
  idMedico?: number | null;
  idConsulta?: number | null;
  diasAfastamento?: number | null;
  incluirCid?: boolean;
  cidCodigo?: string | null;
  cidDescricao?: string | null;
  criarExames?: boolean;
};

export type DocumentoClinico = {
  id: number;
  tipo: TipoDocumento;
  tipoNome: string;
  idPaciente: number;
  nomePaciente: string;
  cpfPaciente?: string | null;
  dtNascPaciente?: string | null;
  idMedico: number;
  nomeMedico: string;
  crmMedico: string;
  especialidadesMedico: string[];
  idConsulta?: number | null;
  conteudo: string;
  diasAfastamento?: number | null;
  cidCodigo?: string | null;
  cidDescricao?: string | null;
  codigoVerificacao: string;
  urlVerificacao: string;
  dtEmissao: string;
  emitidoPor: string;
  cancelado: boolean;
  dtCancelamento?: string | null;
  motivoCancelamento?: string | null;
  nomeClinica: string;
  enderecoClinica?: string | null;
  telefoneClinica?: string | null;
  examesCriados?: number;
};

export type RegistroAuditoria = {
  id: number;
  dtOcorrencia: string;
  usuario: string;
  acao: string;
  recurso: string;
  idRecurso?: string | null;
  metodo: string;
  caminho: string;
  statusCode: number;
  ip?: string | null;
};

export type Exame = {
  id: number;
  idPaciente: number;
  nomePaciente?: string;
  descricao: string;
  resultado?: string;
  idAgenda?: number;
  dataHora?: string;
  idMedico?: number;
  nomeMedico?: string;
  crmMedico?: string;
  dtCriacao?: string;
  cancelado?: boolean;
  dtCancelamento?: string | null;
  motivoCancelamento?: string | null;
  status?: StatusExame;
  dtRealizacao?: string | null;
  dtLaudo?: string | null;
  dtEntrega?: string | null;
  quantidadeAnexos?: number;
};

export type StatusExame = 'Solicitado' | 'Realizado' | 'Laudado' | 'Entregue';

export type ExameAnexo = {
  id: number;
  idExame: number;
  nomeArquivo: string;
  contentType: string;
  tamanhoBytes: number;
  enviadoPor: string;
  dtCriacao: string;
};

export type MedicoResumoProntuario = {
  id: number;
  nome: string;
  crm: string;
  especialidades: string[];
};

export type ConsultaProntuarioItem = {
  id: number;
  idMedico: number;
  nomeMedico: string;
  crmMedico: string;
  especialidadesMedico: string[];
  observacao: string;
  dataAtendimento: string;
  dtCriacao: string;
  status?: StatusConsulta;
  motivoAnulacao?: string | null;
  adendos?: ConsultaAdendo[];
  subjetivo?: string | null;
  objetivo?: string | null;
  avaliacao?: string | null;
  plano?: string | null;
  cidCodigo?: string | null;
  cidDescricao?: string | null;
  sinaisVitais?: SinaisVitais | null;
};

export type ExameProntuarioItem = {
  id: number;
  descricao: string;
  resultado?: string;
  possuiLaudo: boolean;
  idMedico?: number;
  nomeMedico?: string;
  crmMedico?: string;
  dataExame: string;
  dtCriacao: string;
  cancelado?: boolean;
  motivoCancelamento?: string | null;
  status?: StatusExame;
};

export type ProntuarioResumo = {
  totalConsultas: number;
  totalExames: number;
  totalLaudosEmitidos: number;
  totalExamesPendentes: number;
};

export type ProntuarioRelatorio = {
  numeroOficio: string;
  dataEmissao: string;
  paciente: Paciente;
  medicoFiltro?: MedicoResumoProntuario;
  dataInicio?: string;
  dataFim?: string;
  resumo: ProntuarioResumo;
  consultas: ConsultaProntuarioItem[];
  exames: ExameProntuarioItem[];
  documentos?: DocumentoClinico[] | null;
};

// ---------- Fase 4: convênios, financeiro, indicadores e pendências ----------

export type Convenio = {
  id: number;
  nome: string;
  registroAns?: string | null;
  telefone?: string | null;
  observacao?: string | null;
  ativo: boolean;
  dtCriacao: string;
};

export type TabelaPreco = {
  id: number;
  tipo: TipoAtendimento;
  tipoNome: string;
  idConvenio?: number | null;
  nomeConvenio?: string | null;
  valor: number;
  dtAtualizacao?: string | null;
};

export type StatusCobranca = 'Pendente' | 'Paga' | 'Cancelada' | 'Estornada';

export const FORMAS_PAGAMENTO: { valor: number; chave: string; nome: string; icone: string }[] = [
  { valor: 2, chave: 'Pix', nome: 'Pix', icone: 'qr-code-outline' },
  { valor: 1, chave: 'Dinheiro', nome: 'Dinheiro', icone: 'cash-outline' },
  { valor: 3, chave: 'CartaoDebito', nome: 'Cartão de débito', icone: 'card-outline' },
  { valor: 4, chave: 'CartaoCredito', nome: 'Cartão de crédito', icone: 'card-outline' },
  { valor: 6, chave: 'Transferencia', nome: 'Transferência', icone: 'swap-horizontal-outline' },
  { valor: 5, chave: 'Boleto', nome: 'Boleto', icone: 'barcode-outline' },
  { valor: 7, chave: 'Convenio', nome: 'Convênio (repasse)', icone: 'business-outline' }
];

export type Cobranca = {
  id: number;
  idPaciente: number;
  nomePaciente: string;
  cpfPaciente?: string | null;
  idAgenda?: number | null;
  idConvenio?: number | null;
  nomeConvenio?: string | null;
  descricao: string;
  valor: number;
  desconto: number;
  valorFinal: number;
  status: StatusCobranca;
  dtVencimento: string;
  vencida: boolean;
  formaPagamento?: string | null;
  formaPagamentoNome?: string | null;
  dtPagamento?: string | null;
  recebidoPor?: string | null;
  dtCancelamento?: string | null;
  motivoCancelamento?: string | null;
  registradoPor: string;
  dtCriacao: string;
};

export type ItemIndicador = { rotulo: string; quantidade: number; valor: number };

export type ResumoFinanceiro = {
  recebido: number;
  quantidadeRecebida: number;
  ticketMedio: number;
  aReceber: number;
  vencido: number;
  estornado: number;
  porForma: ItemIndicador[];
  porConvenio: ItemIndicador[];
};

export type Recibo = {
  cobranca: Cobranca;
  valorPorExtenso: string;
  nomeClinica: string;
  enderecoClinica?: string | null;
  telefoneClinica?: string | null;
  dtEmissao: string;
};

export type IndicadorMedico = { idMedico: number; nome: string; agendados: number; atendidos: number; faltas: number };

export type Indicadores = {
  inicio: string;
  fim: string;
  agendamentos: number;
  atendidos: number;
  faltas: number;
  taxaFaltas: number;
  taxaConfirmacao: number;
  horariosLivres: number;
  taxaOcupacao: number;
  novosPacientes: number;
  consultasRegistradas: number;
  examesSolicitados: number;
  examesLaudados: number;
  examesAtrasados: number;
  tempoMedioLaudoDias?: number | null;
  financeiro: ResumoFinanceiro;
  porMedico: IndicadorMedico[];
  porTipoAtendimento: ItemIndicador[];
  diagnosticosFrequentes: ItemIndicador[];
};

export type PendenciaItem = {
  categoria: string;
  titulo: string;
  detalhe: string;
  data?: string | null;
  idPaciente?: number | null;
  idReferencia?: number | null;
};

export type Pendencias = {
  examesAtrasados: PendenciaItem[];
  laudosNaoEntregues: PendenciaItem[];
  carteirinhas: PendenciaItem[];
  cobrancasVencidas: PendenciaItem[];
  consultasAbertas: PendenciaItem[];
  retornosSemAgendamento: PendenciaItem[];
  total: number;
};

export type AcessoUsuario = {
  email: string;
  perfis: string[];
  emailConfirmado: boolean;
  bloqueado: boolean;
  ultimaAtividade?: string | null;
  diasSemAtividade?: number | null;
  revisarAcesso: boolean;
  motivoRevisao?: string | null;
};
