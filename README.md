# Consultório - Sistema de Gerenciamento Clínico

Sistema completo de gerenciamento clínico e hospitalar para consultórios médicos, desenvolvido com arquitetura **Domain-Driven Design (DDD)** e metodologias de **Test-Driven Development (TDD)**, utilizando as versões mais recentes das plataformas **.NET 10** e **Angular 22** com **Ionic 9**.

---

## 🚀 Tecnologias Utilizadas

### Backend
- **.NET 10.0** e **C# 14.0**
- **ASP.NET Core Web API** com documentação via OpenAPI
- **Entity Framework Core 10.0** com provedor PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **ASP.NET Core Identity & JWT Bearer**: Autenticação segura com tokens JWT e controle de permissões baseado em Roles
- **xUnit**, **FluentAssertions** e **NSubstitute**: Suíte robusta de testes automatizados com cobertura TDD para Domínio, Aplicação e Apresentação

### Frontend
- **Angular 22** (Zoneless, componentes standalone modernos)
- **Ionic 9** (Mobile & Web UI framework otimizado para alta performance)
- **Capacitor 8** (Integração multiplataforma Android, iOS e Web)
- **TypeScript 6** e **RxJS 7**

### Banco de Dados & Infraestrutura
- **PostgreSQL 16+**
- **Docker** & **Docker Compose** (suporte a containers multi-stage com .NET 10 e Node 22)

---

## 🏛️ Arquitetura do Projeto (DDD)

A solução está estruturada estritamente de acordo com os princípios do **Domain-Driven Design (DDD)**, garantindo separação clara de responsabilidades, alta coesão e baixo acoplamento:

```
Consultorio/
├── Consultorio.sln
│
├── Consultorio.Dominio/                  # CAMADA DE DOMÍNIO RICO (Isolada, sem dependências externas)
│   ├── Entidades/                        # Entidades com invariantes e métodos de negócio
│   │   ├── Base/Entidade.cs              # Classe base com identificador e data de criação/atualização
│   │   ├── Pessoa.cs                     # Cadastro base (Nome, CPF, RG, Telefone, Endereço)
│   │   ├── Paciente.cs                   # Paciente vinculado à Pessoa (Convênio, Histórico, Carteirinha)
│   │   ├── Medico.cs                     # Médico vinculado à Pessoa (CRM, Especialidade, Atendimento)
│   │   ├── Agenda.cs                     # Disponibilidade de horários do médico
│   │   ├── Consulta.cs                   # Agendamento, diagnóstico, prescrição, cancelamento e conclusão
│   │   └── Exame.cs                      # Solicitação, laudo e status de exames
│   ├── ValueObjects/                     # Objetos de Valor com validação e imutabilidade
│   │   ├── Cpf.cs                        # Validação de dígitos verificadores e formato de CPF
│   │   ├── Crm.cs                        # Validação de formato de registro médico com UF (ex: 123456/SP)
│   │   └── Telefone.cs                   # Validação de números de telefone (fixo ou celular)
│   ├── Enumerados/                       # Enums do domínio
│   │   └── Especialidade.cs              # Especialidades médicas suportadas
│   ├── Excecoes/                         # Exceções de regras de negócio
│   │   └── DomainException.cs            # Disparada sempre que uma regra ou invariante é violada
│   └── Interfaces/Repositorios/          # Contratos abstratos de persistência
│       ├── IRepositorio.cs               # Repositório genérico base
│       ├── IPessoaRepositorio.cs
│       ├── IPacienteRepositorio.cs
│       ├── IMedicoRepositorio.cs
│       ├── IAgendaRepositorio.cs
│       ├── IConsultaRepositorio.cs
│       └── IExameRepositorio.cs
│
├── Consultorio.Aplicacao/                # CAMADA DE APLICAÇÃO (Orquestração de Casos de Uso)
│   ├── DTOs/                             # Objetos de transferência de dados (Request e Response)
│   │   ├── Request/                      # Modelos de entrada validados
│   │   └── Response/                     # Projeções de retorno para a interface
│   ├── Interfaces/                       # Contratos dos serviços de aplicação
│   │   ├── IPessoaAppService.cs
│   │   ├── IPacienteAppService.cs
│   │   ├── IMedicoAppService.cs
│   │   ├── IAgendaAppService.cs
│   │   ├── IConsultaAppService.cs
│   │   └── IExameAppService.cs
│   └── Servicos/                         # Implementação dos fluxos de negócio e chamadas ao domínio
│
├── Consultorio.Infraestrutura/           # CAMADA DE INFRAESTRUTURA (Persistência e Acesso a Dados)
│   ├── Contexto/
│   │   └── ConsultorioContexto.cs        # DbContext com Fluent API, mapeamento de VOs e relacionamentos
│   └── Repositorios/                     # Implementações assíncronas do repositório utilizando EF Core
│
├── Consultorio.Identity.Infraestrutura/  # MÓDULO DE IDENTIDADE (Segurança & Autenticação)
│   ├── Contexto/                         # DbContext específico do ASP.NET Core Identity
│   ├── Servicos/                         # IdentityService, geração de tokens JWT e gestão de usuários/roles
│   └── Extensions/                       # Configurações de injeção de dependência e autenticação JWT
│
├── Consultorio.Identity.Modelo/          # Modelos e DTOs de login, registro, troca de senha e roles
│
├── Consultorio.Server/                   # CAMADA DE APRESENTAÇÃO BACKEND (ASP.NET Core Web API)
│   ├── Controllers/                      # Endpoints RESTful para cada agregado do sistema
│   │   ├── PessoaController.cs
│   │   ├── PacienteController.cs
│   │   ├── MedicoController.cs
│   │   ├── AgendaController.cs
│   │   ├── ConsultaController.cs
│   │   ├── ExameController.cs
│   │   └── UsuarioController.cs
│   ├── IoC/                              # Injeção de dependências modular
│   └── Program.cs                        # Inicialização, pipeline de middlewares, migrações automáticas
│
├── Consultorio.Dominio.Tests/            # TESTES DE UNIDADE DE DOMÍNIO (TDD)
├── Consultorio.Aplicacao.Tests/          # TESTES DE UNIDADE DE APLICAÇÃO (Mocks e Regras de Negócio)
├── Consultorio.Server.Tests/             # TESTES DE UNIDADE DOS CONTROLLERS (Endpoints e Validações HTTP)
│
└── Consultorio.Cliente/                  # CAMADA DE APRESENTAÇÃO FRONTEND (Ionic 9 / Angular 22)
    ├── src/app/
    │   ├── pacientes/                    # Telas e modais de gerenciamento de pacientes
    │   ├── medicos/                      # Telas e modais de gerenciamento de médicos e especialidades
    │   ├── agenda/                       # Telas de visualização e agendamento de horários médicos
    │   ├── consultas/                    # Gestão de consultas (agendamento, laudos, cancelamento)
    │   ├── exames/                       # Solicitação e acompanhamento de exames clínicos
    │   ├── user/                         # Telas de login, cadastro, recuperação de senha e gestão de usuários
    │   ├── home/                         # Dashboard com atalhos para todas as áreas clínicas
    │   └── services/                     # Serviços de integração HTTP com o backend (.NET 10 API)
```

---

## 🧪 Práticas de TDD e Testes Automatizados

O sistema segue rigorosamente o ciclo de **Test-Driven Development (TDD)**, com 100% de testes passando para todas as regras de negócio, invariantes de domínio e fluxos de aplicação.

### Executando os Testes do Backend (.NET)
Para executar toda a suíte de testes da solução:
```bash
dotnet test Consultorio.sln
```

Ou execute projetos específicos:
```bash
# Testes do Domínio Rico (Entidades, Value Objects e Invariantes)
dotnet test Consultorio.Dominio.Tests

# Testes da Aplicação (Casos de Uso e Orquestração de Serviços)
dotnet test Consultorio.Aplicacao.Tests

# Testes da API (Controllers e Respostas HTTP)
dotnet test Consultorio.Server.Tests
```

### Executando os Testes do Frontend (Angular / Ionic)
```bash
cd Consultorio.Cliente
npm test -- --watch=false --browsers=ChromeHeadless
```

---

## ⚙️ Configuração e Execução

### 1. Pré-requisitos
- [.NET SDK 10.0+](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) e NPM
- [PostgreSQL 16+](https://www.postgresql.org/)

### 2. Configuração das Strings de Conexão
Edite o arquivo `Consultorio.Server/appsettings.json` com os dados do seu PostgreSQL:
```json
{
  "ConnectionStrings": {
    "IdentityConection": "Host=localhost;Port=5432;Database=IdentityDB;Username=postgres;Password=suasenha",
    "ConsultorioConection": "Host=localhost;Port=5432;Database=ConsultorioDB;Username=postgres;Password=suasenha"
  },
  "RunMigration": true,
  "Jwt": {
    "Key": "CHAVE_SECRETA_SUPER_SEGURA_COM_NO_MINIMO_32_CARACTERES_AQUI!",
    "Issuer": "ConsultorioAPI",
    "Audience": "ConsultorioCliente"
  }
}
```

> **Nota:** Quando `"RunMigration": true`, as migrações do banco de dados são aplicadas automaticamente ao iniciar o backend.

### 3. Executando o Backend
```bash
cd Consultorio.Server
dotnet run
```
A API estará disponível em:
- Documentação OpenAPI: `https://localhost:7196/openapi/v1.json` ou através do Swagger/Scalar integrado.

### 4. Executando o Frontend (Angular / Ionic)
```bash
cd Consultorio.Cliente
npm install
npm start
```
Acesse a aplicação no navegador em: `http://localhost:4200`

---

## 🐳 Executando com Docker

O projeto inclui suporte completo a Docker com compilação multi-stage em .NET 10 e Node 22:

```bash
docker build -t consultorio-app -f Consultorio.Server/Dockerfile .
docker run -p 8080:8080 consultorio-app
```

---

## 📋 Endpoints Principais da API

| Módulo | Método | Endpoint | Descrição |
|---|---|---|---|
| **Pessoas** | `GET` | `/api/pessoa` | Lista todas as pessoas cadastradas |
| | `POST` | `/api/pessoa` | Cadastra uma nova pessoa com CPF validado |
| | `GET` | `/api/pessoa/{id}` | Obtém detalhes de uma pessoa |
| **Pacientes** | `GET` | `/api/paciente` | Lista todos os pacientes |
| | `POST` | `/api/paciente` | Cadastra novo paciente vinculado a uma pessoa |
| **Médicos** | `GET` | `/api/medico` | Lista médicos com especialidade e CRM |
| | `POST` | `/api/medico` | Cadastra médico com validação de CRM e especialidade |
| **Agenda** | `GET` | `/api/agenda/medico/{medicoId}` | Lista agenda de horários de um médico |
| | `POST` | `/api/agenda` | Cria novos horários de atendimento |
| **Consultas** | `GET` | `/api/consulta` | Lista consultas do consultório |
| | `POST` | `/api/consulta` | Agenda uma nova consulta médica |
| | `PUT` | `/api/consulta/{id}/concluir` | Registra diagnóstico, prescrição e conclui consulta |
| | `PUT` | `/api/consulta/{id}/cancelar` | Cancela consulta informando motivo |
| **Exames** | `GET` | `/api/exame/paciente/{pacienteId}` | Lista exames de um paciente |
| | `POST` | `/api/exame` | Registra solicitação de exame |
| | `PUT` | `/api/exame/{id}/laudo` | Adiciona laudo técnico e conclui exame |
| **Autenticação** | `POST` | `/api/usuario/login` | Autentica usuário e retorna token JWT |
| | `POST` | `/api/usuario/registrar` | Registra novo usuário no sistema |

---

## 📄 Licença

Este projeto é distribuído sob os termos da licença [MIT](LICENSE).
