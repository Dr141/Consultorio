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
- **Ícones:** só os ícones usados são registrados (`src/app/icones.ts`), em vez do pacote inteiro do Ionicons. Isso reduz o pacote inicial. Ao usar um ícone novo em um template, inclua-o nesse arquivo; um ícone não registrado aparece em branco e gera um aviso no console.
- **Build:** ainda usa o builder `@angular-devkit/build-angular:browser` (webpack), que o Angular marca como obsoleto. A migração para `@angular/build:application` (esbuild) é feita com `ng update @angular/cli --name use-application-builder`. Depois da migração, use `"outputPath": { "base": "../Consultorio.Server/wwwroot", "browser": "" }` para manter os arquivos direto em `wwwroot`, porque o novo builder grava por padrão em uma subpasta `browser`.

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

1. **Envia lembretes** aos pacientes com atendimento nas próximas `Clinica:AntecedenciaLembreteHoras` (padrão 24h), por WhatsApp (celular) ou e-mail. A mensagem traz links para **confirmar** ou **desmarcar**. A página aberta pelo link pede um clique para concluir, para que filtros de e-mail e antivírus não confirmem nada sozinhos.
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
- **Outros canais (SMS etc.):** implemente `INotificador` (`Consultorio.Dominio.Interfaces.Servicos`) e registre a classe em `RegistrarDependencias`. As rotinas tentam os canais na ordem de registro e passam ao próximo se um falhar.

### Notificações por WhatsApp

O envio usa a **WhatsApp Cloud API** oficial da Meta. Lembretes, resultados de exame e avisos de retorno vão por WhatsApp quando o paciente tem **celular** cadastrado (DDD + 9 dígitos começando por 9). Os demais casos seguem por e-mail. Se o WhatsApp recusar a mensagem, o sistema tenta o e-mail automaticamente. O código de acesso ao portal do paciente continua indo só por e-mail.

1. No [Meta for Developers](https://developers.facebook.com/), crie um app do tipo **Business** e adicione o produto **WhatsApp**. Cadastre e verifique o número da clínica.
2. Em **Gerenciador do WhatsApp → Modelos de mensagem**, crie um modelo da categoria **Utilidade**, idioma **Português (BR)**, chamado `aviso_clinica`, com o corpo:

   ```
   Olá {{1}}! {{2}}
   ```

   `{{1}}` recebe o primeiro nome do paciente e `{{2}}` o texto do aviso, em uma linha (os links de confirmar/desmarcar vão junto). Aguarde a aprovação.
3. Gere um **token permanente**: Configurações do negócio → Usuários do sistema → gerar token com as permissões `whatsapp_business_messaging` e `whatsapp_business_management`.
4. Copie o **Phone number ID** (identificador do número, não o número) da tela *API Setup*.
5. Configure (no `.env` do Docker ou no `appsettings.json`):

```json
"Notificacoes": {
  "CanalPreferido": "WhatsApp",
  "WhatsApp": { "Habilitado": true, "Token": "EAAG...", "PhoneNumberId": "1234567890", "Template": "aviso_clinica", "Idioma": "pt_BR" }
}
```

- `CanalPreferido: "Email"` inverte a prioridade para quem tem e-mail e celular.
- Falhas aparecem no log com o código de erro da Meta, por exemplo modelo não aprovado ou número sem WhatsApp. O número e o texto do paciente não são registrados.
- Conversas iniciadas pela empresa são cobradas pela Meta por conversa. Consulte a tabela de preços da categoria Utilidade.

---

## 🩺 Prontuário Clínico, Documentos e Exames

- **Evolução estruturada:** a consulta aceita texto livre e/ou SOAP (Subjetivo, Objetivo, Avaliação, Plano), sinais vitais (PA, FC, FR, temperatura, SpO₂, peso, altura com IMC calculado, glicemia) e diagnóstico CID-10 com busca por código ou descrição (`GET /api/cid?busca=`). Os limites dos sinais vitais recusam valores impossíveis (erro de digitação), não valores clínicos anormais.
- **Alertas clínicos do paciente:** alergias, medicamentos em uso e condições crônicas (um item por linha) aparecem em destaque no registro da consulta, na emissão de documentos e no prontuário. Só perfis clínicos editam.
- **Documentos:** receita simples, receita de controle especial (impressa em duas vias), atestado com dias de afastamento, declaração de comparecimento, pedido de exames, encaminhamento e relatório. A tela `/documento/:id` imprime ou salva em PDF pelo navegador.
  - O médico logado só emite e cancela em nome próprio. Documento emitido não é apagado: é cancelado com motivo.
  - O CID só sai no documento quando o médico marca "Incluir CID" (no atestado, com autorização do paciente).
  - Pedido de exames pode criar automaticamente um exame "Solicitado" por linha.
  - Cada documento tem um código de verificação. A página pública `GET /api/publico/documento/{codigo}` confirma a autenticidade mostrando tipo, data, médico/CRM, iniciais do paciente e se foi cancelado, nunca o conteúdo.
- **Ciclo do exame:** Solicitado → Realizado → Laudado → Entregue, com data de cada etapa. Resultados podem ser anexados (PDF, JPG ou PNG até 10 MB); o tipo é conferido pela assinatura do arquivo, não pela extensão.
- **PDF e assinatura digital (ICP-Brasil):** o botão **Baixar PDF** gera o documento em PDF (A4) no servidor. Com o certificado do médico cadastrado, o botão **Assinar digitalmente** produz um PDF com assinatura **PAdES** (CAdES destacada, SHA-256, atributo *signing-certificate-v2*). O Adobe Acrobat e o validador do ITI ([validar.iti.gov.br](https://validar.iti.gov.br)) reconhecem essa assinatura. Detalhes na seção a seguir.

### Assinatura digital de receitas e atestados

1. **Cadastrar o certificado:** em **Corpo Clínico → Certificado digital**, envie o arquivo **A1** (`.pfx`/`.p12`, e-CPF do médico) e a senha. Pode fazer isso o próprio médico (login vinculado ao cadastro) ou um administrador.
   - O sistema confere a senha, a chave privada e a validade.
   - Também indica se o certificado é da **ICP-Brasil**. Só certificados ICP-Brasil têm validade jurídica (MP 2.200-2/2001); para receitas de antimicrobianos e de controle especial, a farmácia exige assinatura ICP-Brasil.
   - Certificados **A3** (token ou cartão) não podem ser usados por um servidor e não são aceitos.
2. **Assinar:** na tela do documento, **Assinar digitalmente**. Só o próprio médico assina. Se o médico não tem login vinculado, um administrador pode assinar em seu nome (consultório de um médico só).
   - O PDF assinado fica guardado (tabela `DocumentoAssinatura`) e não muda mais.
   - **Baixar PDF** passa a entregar essa via, inclusive no portal do paciente.
   - A página pública de verificação mostra quem assinou e quando.
   - Documento cancelado depois de assinado sai sem assinatura e com a marca **CANCELADO**.
3. **Segurança:** o `.pfx` e a senha são gravados criptografados com o **ASP.NET Core Data Protection**.
   - As chaves ficam em `DataProtection:Pasta`; no Docker é o volume `dataprotection_keys`, montado em `/app/keys`.
   - **Faça backup desse volume** junto com o banco:
     ```bash
     docker run --rm -v consultorio_dataprotection_keys:/k -v "$PWD/backups":/b alpine tar czf /b/chaves-dataprotection.tgz -C /k .
     ```
     O nome do volume leva o prefixo do projeto do compose (`docker volume ls` mostra o nome exato).
   - Sem as chaves, os certificados guardados não podem ser abertos e precisam ser enviados de novo. Documentos já assinados não são afetados.
   - Ao remover ou substituir um certificado, o arquivo e a senha antigos são apagados do banco.

---

## 💰 Financeiro, Convênios, Indicadores e Rotinas

- **Convênios:** cadastro das operadoras (nome, registro ANS, contato). Cada paciente pode ter convênio, número e validade da carteirinha; sem convênio = particular. A fila do dia destaca carteirinhas vencidas.
- **Tabela de preços:** valor por tipo de atendimento (consulta, retorno, exame, procedimento), particular e por convênio.
- **Cobranças:** geradas automaticamente quando o atendimento é concluído, com o valor do convênio do paciente (carteirinha válida) ou o particular. Também podem ser lançadas à mão.
  - Recebimento por Pix, dinheiro, cartão, transferência, boleto ou repasse do convênio, com desconto opcional.
  - Nada é apagado: pendente é cancelada e paga é estornada, sempre com motivo. Estorno e resumo financeiro são da gestão.
  - Recibo para impressão em `/financeiro/recibo/:id`, com o valor por extenso.
- **Indicadores** (gestão): atendimentos, taxa de faltas, confirmação, ocupação da agenda, pacientes novos, exames e tempo médio de laudo, recebido / a receber / vencido, ticket médio, recebimentos por forma e por pagador e diagnósticos (CID) mais frequentes. Filtro por período e por médico.
- **Pendências** (toda a equipe): exames atrasados, laudos prontos não entregues, carteirinhas vencidas ou vencendo, cobranças vencidas, consultas abertas há mais de 24 h e retornos sem horário marcado.
- **Retorno:** o médico indica a data do retorno na consulta (atalhos de 7 dias a 6 meses).
- **Revisão de acessos** (administrador, em Auditoria): usuários, perfis e última atividade; contas sem uso, sem perfil ou com perfil de administrador aparecem primeiro.

### Rotinas automáticas da Fase 4

Rodam junto com as da agenda (a cada `Rotinas:IntervaloMinutos`), cada uma isolada das demais:

| Rotina | Quando | O que faz |
|---|---|---|
| Cobranças | A cada ciclo | Cria a cobrança dos atendimentos concluídos nos últimos 7 dias que ainda não têm (se houver preço) |
| Laudo pronto | A cada ciclo | Avisa o paciente que o resultado está disponível, sem o conteúdo do laudo |
| Lembrete de retorno | A cada ciclo | Alguns dias antes da data de retorno, lembra quem ainda não agendou |
| Resumo do dia | 1x por dia, a partir de `HoraResumoDiario` | Médicos: quantidade e horários do dia (sem nomes de pacientes) e exames aguardando laudo. Gestão: pendências |
| Indicadores do mês | 1x por mês | Indicadores do mês anterior para Administrador e Administrativo |
| Revisão de acessos | 1x por mês | Lista aos administradores as contas que merecem revisão |

As rotinas diárias e mensais registram a execução na tabela `ExecucaoRotina`, então um reinício do servidor não repete o envio. Configuração (seção `Clinica`):

```json
"Clinica": {
  "GerarCobrancaAutomatica": true,
  "PrazoPagamentoConvenioDias": 30,
  "PrazoExameAtrasadoDias": 7,
  "PrazoEntregaLaudoDias": 15,
  "AntecedenciaLembreteRetornoDias": 5,
  "AvisoValidadeCarteirinhaDias": 30,
  "HoraResumoDiario": 7,
  "DiasInatividadeRevisaoAcessos": 90
}
```

### Backup e restauração

O serviço `backup` do `docker-compose` faz `pg_dump` diário dos bancos `Consultorio` e `Identity` em `./backups` (configurável por `BACKUP_PASTA`), no horário `BACKUP_HORA_UTC` (padrão 5 = 02:00 em Brasília), mantendo `BACKUP_RETENCAO_DIAS` dias (padrão 14).

```bash
# Backup imediato
docker compose exec backup sh /scripts/backup.sh agora

# Restaurar (substitui os dados atuais do banco!)
docker compose stop app
docker compose exec -T db pg_restore -U postgres -d Consultorio --clean --if-exists < backups/Consultorio-AAAAMMDD-HHMMSS.dump
docker compose exec -T db pg_restore -U postgres -d Identity --clean --if-exists < backups/Identity-AAAAMMDD-HHMMSS.dump
docker compose start app
```

Os arquivos contêm dados de pacientes: guarde uma cópia fora do servidor (outro disco ou nuvem com criptografia) e teste a restauração periodicamente.

Guarde também uma cópia do volume `dataprotection_keys`. Ele contém as chaves que abrem os certificados digitais dos médicos; o comando está em [Assinatura digital de receitas e atestados](#assinatura-digital-de-receitas-e-atestados).

---

## 🧑‍⚕️ Portal do Paciente

Área pública em **`/portal`**, com link na tela de login da equipe. O paciente consulta e resolve o que é dele sem ligar para a clínica.

- **Acesso sem senha:** o paciente informa CPF e data de nascimento e recebe um código de 6 dígitos no e-mail cadastrado na clínica. O código vale 10 minutos e aceita até 5 tentativas.
- **Sessão:** curta (padrão 30 min), guardada só na aba do navegador (`sessionStorage`), com o token enviado no cabeçalho `X-Portal-Token`. Código e token ficam no banco apenas como hash.
- **Privacidade:**
  - a resposta ao pedido de código é sempre a mesma, exista ou não o cadastro, então não revela quem é paciente;
  - o paciente da sessão vem do servidor, nunca do navegador;
  - pedir dados de outra pessoa responde "não encontrado";
  - a evolução clínica nunca aparece no portal.
- **Proteções:** cada IP faz no máximo 10 tentativas de entrada a cada 5 minutos, e cada paciente pede no máximo 5 códigos por hora. Todo acesso autenticado entra na trilha de auditoria como `portal:paciente:{id}`.
- **O que o paciente faz:**
  - vê os próximos atendimentos e o histórico;
  - confirma presença e desmarca, com antecedência mínima;
  - agenda horários livres de consulta ou retorno, filtrando por especialidade;
  - vê os resultados de exames já laudados (texto e arquivos);
  - abre e imprime documentos emitidos;
  - vê as cobranças e imprime recibos.

```json
"Clinica": {
  "PortalHabilitado": true,
  "PortalSessaoMinutos": 30,
  "PortalDiasAgendamento": 30,
  "PortalAntecedenciaAgendamentoHoras": 2,
  "PortalAntecedenciaCancelamentoHoras": 12,
  "PortalMaximoAgendamentosFuturos": 2,
  "PortalLiberarResultados": true
}
```

O portal depende do envio de e-mails (seção `Notificacoes:Email`). Pacientes sem e-mail cadastrado precisam pedir a inclusão na recepção.

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
| | `PUT` | `/api/consulta/{id}/evolucao` | Atualiza SOAP, CID e sinais vitais de consulta aberta |
| | `PATCH` | `/api/exame/{id}/realizado` · `/entregue` | Avança o ciclo do exame |
| | `POST` / `GET` | `/api/exame/{id}/anexos` | Envia e lista arquivos do resultado |
| **Pacientes** | `PUT` | `/api/paciente/{id}/alertas` | Alergias, medicamentos em uso e condições crônicas |
| **Documentos** | `POST` | `/api/documento` | Emite receita, atestado, pedido de exames etc. |
| | `GET` | `/api/documento/paciente/{id}` | Documentos emitidos para o paciente |
| | `PATCH` | `/api/documento/{id}/cancelar` | Cancela com motivo (continua no histórico) |
| | `GET` | `/api/documento/{id}/pdf` | PDF do documento (a via assinada, se houver) |
| | `POST` | `/api/documento/{id}/assinar` | Assina com o certificado A1 do médico (PAdES) |
| | `GET` `POST` `DELETE` | `/api/medico/{id}/certificado` | Certificado digital do médico (envio em `multipart/form-data`: `arquivo` + `senha`) |
| | `GET` | `/api/publico/documento/{codigo}` | Verificação pública de autenticidade |
| **CID-10** | `GET` | `/api/cid?busca=` | Busca de códigos frequentes |
| **Portal do paciente** | `POST` | `/api/portal/acesso/codigo` · `/acesso/sessao` | Código por e-mail e abertura da sessão |
| | `GET` | `/api/portal/resumo` · `/horarios` · `/exames` · `/documentos` · `/cobrancas` | Dados do próprio paciente (cabeçalho `X-Portal-Token`) |
| | `POST` / `PATCH` / `DELETE` | `/api/portal/agendamentos/{idAgenda}` | Agendar, confirmar e desmarcar |
| **Convênios** | `GET` / `POST` | `/api/convenio` | Lista e cadastra convênios |
| | `GET` / `PUT` | `/api/convenio/precos` | Tabela de preços (particular e por convênio) |
| | `PUT` | `/api/paciente/{id}/convenio` | Convênio e carteirinha do paciente |
| **Financeiro** | `GET` / `POST` | `/api/financeiro/cobrancas` | Lista e lança cobranças |
| | `POST` | `/api/financeiro/atendimento/{idAgenda}` | Gera a cobrança de um atendimento |
| | `PATCH` | `/api/financeiro/cobrancas/{id}/pagar` · `/cancelar` · `/estornar` | Recebimento, cancelamento e estorno |
| | `GET` | `/api/financeiro/cobrancas/{id}/recibo` | Dados do recibo |
| **Indicadores** | `GET` | `/api/indicadores?inicio=&fim=&idMedico=` | Painel de gestão |
| | `GET` | `/api/indicadores/pendencias` | Pendências operacionais |
| **Auditoria** | `GET` | `/api/auditoria/revisao-acessos` | Revisão de contas e perfis |
| **Autenticação** | `POST` | `/api/usuario/login` | Login com retorno de token JWT |
| | `POST` | `/api/usuario/registrar` | Registro de novo usuário |

---

## 📄 Licença

Este projeto é de propriedade de **Dr141** e está distribuído sob os termos da licença [MIT](LICENSE).
