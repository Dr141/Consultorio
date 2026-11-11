# Consultório - Sistema de Gerenciamento Clínico

Sistema completo de gerenciamento clínico e hospitalar para consultórios médicos, desenvolvido com arquitetura **Domain-Driven Design (DDD)** e metodologias de **Test-Driven Development (TDD)**, utilizando as versões mais recentes das plataformas **.NET 10** e **Angular 22** com **Ionic 9**, cache distribuído com **Redis 7** e orquestração de contêineres com **Docker Compose**.

---

## 🚀 Tecnologias Utilizadas

### Backend
- **.NET 10.0** e **C# 14.0**
- **ASP.NET Core Web API** com documentação via OpenAPI
- **Entity Framework Core 10.0** com provedor PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Redis 7** & **StackExchange.Redis**: Cache distribuído resiliente utilizando o padrão *Cache-Aside* com fallback transparente para evitar interrupções de serviço
- **ASP.NET Core Identity & JWT Bearer**: Autenticação segura com tokens JWT e controle de permissões baseado em Roles
- **xUnit**, **FluentAssertions** e **NSubstitute**: Suíte robusta de testes automatizados com cobertura TDD para Domínio, Aplicação, Infraestrutura e Apresentação (80 testes unitários)

### Frontend
- **Angular 22** (Zoneless, componentes standalone modernos)
- **Ionic 9** (Mobile & Web UI framework otimizado para alta performance)
- **Capacitor 8** (Integração multiplataforma Android, iOS e Web)
- **TypeScript 6** e **RxJS 7**

### Banco de Dados, Cache & DevOps
- **PostgreSQL 16+**
- **Redis 7 Alpine**
- **Docker** & **Docker Compose** (orquestração completa com healthchecks e volumes persistentes)
- **GitHub Actions** (Workflow de CI/CD para execução de testes, geração de releases com binários multiplataforma e publicação de imagens no GHCR)

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
│   │   ├── Paciente.cs                   # Paciente vinculado à Pessoa (ativo/inativo)
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
│   └── Interfaces/                       # Contratos abstratos de persistência e serviços
│       ├── Repositorios/                 # IRepositorio, IPessoaRepositorio, IPacienteRepositorio, etc.
│       └── Servicos/
│           └── ICacheService.cs          # Contrato de cache distribuído
│
├── Consultorio.Aplicacao/                # CAMADA DE APLICAÇÃO (Orquestração de Casos de Uso)
│   ├── DTOs/                             # Objetos de transferência de dados (Request e Response)
│   ├── Interfaces/                       # Contratos dos serviços de aplicação
│   └── Servicos/                         # Implementação dos fluxos de negócio com Cache-Aside Pattern
│
├── Consultorio.Infraestrutura/           # CAMADA DE INFRAESTRUTURA (Persistência, Cache e Acesso a Dados)
│   ├── Cache/
│   │   └── RedisCacheService.cs          # Implementação resiliente do cache Redis via IDistributedCache
│   ├── Contexto/
│   │   └── ConsultorioContexto.cs        # DbContext com Fluent API, mapeamento de VOs e relacionamentos
│   └── Repositorios/                     # Implementações assíncronas do repositório utilizando EF Core 10
│
├── Consultorio.Identity.Infraestrutura/  # MÓDULO DE IDENTIDADE (Segurança & Autenticação)
│   ├── Contexto/                         # DbContext específico do ASP.NET Core Identity
│   └── Servicos/                         # IdentityService, geração de tokens JWT e gestão de usuários/roles
│
├── Consultorio.Identity.Modelo/          # Modelos e DTOs de login, registro, troca de senha e roles
│
├── Consultorio.Server/                   # CAMADA DE APRESENTAÇÃO BACKEND (ASP.NET Core Web API)
│   ├── Controllers/                      # Endpoints RESTful para cada agregado do sistema
│   ├── IoC/                              # Injeção de dependências modular (EF Core, Identity, Redis)
│   └── Dockerfile                        # Multi-stage build com .NET 10 SDK e Node 22 / Angular 22
│
├── Consultorio.Dominio.Tests/            # TESTES DE UNIDADE DE DOMÍNIO (TDD)
├── Consultorio.Aplicacao.Tests/          # TESTES DE UNIDADE DE APLICAÇÃO E CACHE (xUnit + NSubstitute)
├── Consultorio.Server.Tests/             # TESTES DE UNIDADE DOS CONTROLLERS (Endpoints e Validações HTTP)
│
├── Consultorio.Cliente/                  # CAMADA DE APRESENTAÇÃO FRONTEND (Ionic 9 / Angular 22)
│   └── src/app/                          # Telas de Pacientes, Médicos, Agenda, Consultas, Exames e Auth
│
├── .github/workflows/
│   └── release.yml                       # Pipeline de CI/CD para Release e publicação de imagens
├── docker-compose.yml                    # Orquestração de containers (PostgreSQL, Redis, App)
└── .env.example                          # Modelo de variáveis de ambiente do compose
```

---

## 🔐 Perfis de Acesso e Preservação do Prontuário

| Perfil | O que pode fazer |
|---|---|
| **Administrador** | Tudo, incluindo usuários, perfis e a trilha de auditoria |
| **Medico** | Consultas, laudos e relatório de prontuário, além de pacientes e agenda |
| **Administrativo** | Pacientes, agenda, solicitação de exames, cadastro do corpo clínico e inativações |
| **Auxiliar** | Pacientes, agenda e solicitação de exames |

> Usuários sem perfil atribuído não acessam as telas do sistema. Atribua os perfis em **Gestão de Usuários**.

Regras do prontuário (Lei 13.787/2018 e LGPD):

- **Nada é apagado.** Pacientes e médicos são *inativados* (e podem ser reativados), consultas são *anuladas* com motivo e exames são *cancelados* com motivo. Os registros continuam no histórico e no relatório de prontuário.
- **Consulta finalizada é imutável.** Correções e complementos são feitos por *adendo*, com data, hora e autor.
- **Autoria.** Quando o médico tem um e-mail de login vinculado ao cadastro (tela Corpo Clínico), só ele ou um administrador altera, finaliza ou anula as consultas dele.
- **Auditoria.** Leituras e alterações de pacientes, consultas, exames e prontuários ficam registradas (usuário, ação, registro, data/hora e IP). Administradores consultam em **Menu → Auditoria de Acessos** ou em `GET /api/auditoria`.
- **Esquema do banco.** As colunas e tabelas novas são criadas automaticamente na inicialização (`Migration:RunMigration=true`) com comandos idempotentes, sem afetar os dados existentes.

---

## 📅 Agenda, Recepção e Lembretes

- **Situação do agendamento:** Agendado → Confirmado → Aguardando (chegou) → Em atendimento → Atendido, ou Faltou. Cada etapa guarda data e hora.
- **Recepção · Fila do dia** (`/recepcao`): check-in, sala de espera com tempo de espera, atendimentos em curso e encerrados. Atualiza sozinha a cada minuto. Ao iniciar o atendimento, o médico registra a consulta já vinculada ao horário; finalizar a consulta conclui o atendimento.
- **Gerar horários** (Agenda → Gerar horários): cria a grade de um médico em lote, por exemplo seg a sex, 8h–12h, de 30 em 30 minutos, com pausa opcional. Pula horários que já existem e dias bloqueados.
- **Bloqueios** (Agenda → Bloqueios): férias, feriados e congressos, para um médico ou para a clínica toda. Remove os horários livres do período e lista os pacientes que precisam ser remarcados.

### Rotinas automáticas

A cada `Rotinas:IntervaloMinutos` (padrão 15), o servidor:

1. **Envia lembretes** aos pacientes com atendimento nas próximas `Clinica:AntecedenciaLembreteHoras` (padrão 24h) que têm e-mail cadastrado. O e-mail traz links para **confirmar** ou **desmarcar**. A página aberta pelo link pede um clique para concluir, para que filtros de e-mail e antivírus não confirmem nada sozinhos.
2. **Marca faltas** de agendamentos sem check-in depois de `Clinica:ToleranciaFaltaMinutos` (padrão 180) do horário.

Configuração (`appsettings.json` ou variáveis de ambiente do Docker, ver `.env.example`):

```json
"Clinica": { "Nome": "Clínica Exemplo", "UrlPublica": "https://consultorio.exemplo.com.br", "Telefone": "(62) 3333-0000", "FusoHorario": "America/Sao_Paulo" },
"Notificacoes": {
  "SimularEnvio": false,
  "Email": { "Host": "smtp.seuprovedor.com", "Porta": 587, "UsarSsl": true, "Usuario": "...", "Senha": "...", "Remetente": "agenda@exemplo.com.br" }
}
```

- Sem `Notificacoes:Email:Host`, nenhum e-mail é enviado. Em desenvolvimento, `SimularEnvio: true` registra no log o que seria enviado.
- `Clinica:UrlPublica` precisa ser o endereço acessível pelo paciente, porque os links de confirmação apontam para ele.
- **WhatsApp/SMS:** implemente `INotificador` (`Consultorio.Dominio.Interfaces.Servicos`) chamando a API do provedor e registre a classe em `RegistrarDependencias`. As rotinas usam o primeiro canal disponível para cada paciente.

---

## 🧪 Práticas de TDD e Testes Automatizados

O sistema segue o ciclo de **Test-Driven Development (TDD)**, com cobertura para regras de domínio, orquestração de serviços, cache distribuído e controllers da API.

### Executando os Testes do Backend (.NET)
```bash
dotnet test Consultorio.sln
```

### Executando os Testes do Frontend (Angular / Ionic)
```bash
cd Consultorio.Cliente
npm test -- --watch=false --browsers=ChromeHeadless
```

---

## 🐳 Executando com Docker Compose (Recomendado)

A forma mais simples e rápida de subir todo o ecossistema (PostgreSQL, Redis e a aplicação Consultório completa com front-end embutido) é através do **Docker Compose**:

```bash
# 1. Copiar as variáveis de ambiente (ajuste se necessário)
cp .env.example .env

# 2. Subir os serviços em segundo plano com build automático
docker compose up -d --build

# 3. Acompanhar os logs da aplicação
docker compose logs -f app
```

A aplicação estará disponível em:
- **Web App & API**: `http://localhost:8080`
- **PostgreSQL**: `localhost:5432`
- **Redis**: `localhost:6379`

Para parar todos os contêineres:
```bash
docker compose down
```

---

## ⚙️ Execução Local para Desenvolvimento (Sem Docker)

### 1. Pré-requisitos
- [.NET SDK 10.0+](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/) e NPM
- [PostgreSQL 16+](https://www.postgresql.org/)
- [Redis 7+](https://redis.io/) (opcional: a aplicação faz fallback automático para cache em memória)

### 2. Configuração do `appsettings.Development.json`
```json
{
  "ConnectionStrings": {
    "IdentityConection": "Host=127.0.0.1;Port=5432;Database=Identity;Username=postgres;Password=1234",
    "ConsultorioConection": "Host=127.0.0.1;Port=5432;Database=Consultorio;Username=postgres;Password=1234"
  },
  "Redis": {
    "ConnectionString": "127.0.0.1:6379",
    "InstanceName": "Consultorio:",
    "Enabled": true,
    "DefaultExpirationMinutes": 30
  },
  "Migration": {
    "RunMigration": true
  }
}
```

### 3. Executando o Backend
```bash
cd Consultorio.Server
dotnet run
```

### 4. Executando o Frontend
```bash
cd Consultorio.Cliente
npm install
npm start
```
Acesse `http://localhost:4200` no navegador.

---

## 🚀 Pipeline de CI/CD e Releases (GitHub Actions)

O repositório possui automação completa configurada em `.github/workflows/release.yml`:

- **Gatilho Automático**: Ao enviar uma tag git semântica (`git tag v1.0.0 && git push origin v1.0.0`).
- **Gatilho Manual**: Via interface do GitHub Actions (`Actions` ➔ `Release` ➔ `Run workflow`).
- **Ações executadas**:
  1. Validação de qualidade com execução de todos os testes unitários do Backend (.NET) e Frontend (Karma/ChromeHeadless).
  2. Build de produção do cliente Angular 22.
  3. Publicação e compactação dos binários do ASP.NET Core (`consultorio-server-linux-x64.tar.gz` e `consultorio-server-win-x64.zip`).
  4. Criação da **GitHub Release** com changelog automático e anexo dos binários, `docker-compose.yml` e `.env.example`.
  5. Construção e publicação da imagem Docker no **GitHub Container Registry (`ghcr.io/dr141/consultorio`)**.

---

## 📋 Endpoints Principais da API

| Módulo | Método | Endpoint | Descrição |
|---|---|---|---|
| **Pessoas** | `GET` | `/api/pessoa` | Lista pessoas cadastradas (com cache Redis) |
| | `POST` | `/api/pessoa` | Cadastra pessoa com CPF validado |
| | `GET` | `/api/pessoa/{id}` | Detalhes da pessoa |
| **Pacientes** | `GET` | `/api/paciente` | Lista pacientes (com cache Redis) |
| | `POST` | `/api/paciente` | Cadastra paciente |
| **Médicos** | `GET` | `/api/medico` | Lista médicos e especialidades (com cache Redis) |
| | `POST` | `/api/medico` | Cadastra médico com CRM validado |
| **Agenda** | `GET` | `/api/agenda/medico/{medicoId}` | Horários da agenda do médico (com cache Redis) |
| | `POST` | `/api/agenda` | Cria novos horários |
| **Consultas** | `GET` | `/api/consulta` | Lista consultas |
| | `POST` | `/api/consulta` | Agenda consulta |
| | `PUT` | `/api/consulta/{id}/concluir` | Diagnóstico, prescrição e conclusão |
| **Exames** | `GET` | `/api/exame/paciente/{pacienteId}` | Lista exames do paciente |
| | `POST` | `/api/exame` | Solicitação de exame |
| | `PUT` | `/api/exame/{id}/laudo` | Registra laudo técnico |
| **Autenticação** | `POST` | `/api/usuario/login` | Login com retorno de token JWT |
| | `POST` | `/api/usuario/registrar` | Registro de novo usuário |

---

## 📄 Licença

Este projeto é de propriedade de **Dr141** e está distribuído sob os termos da licença [MIT](LICENSE).
