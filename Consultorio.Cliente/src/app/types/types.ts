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
};
