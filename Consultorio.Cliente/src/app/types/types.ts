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
};

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
