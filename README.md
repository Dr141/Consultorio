# Consultório - Sistema de Gerenciamento

Este é um sistema de gerenciamento para um consultório médico, desenvolvido em .NET 9 e C# 13.0. Ele fornece APIs para gerenciar pacientes, médicos, consultas, exames e outros recursos relacionados.

## Funcionalidades

- **Gerenciamento de Pacientes**: Cadastro, atualização, listagem e exclusão de pacientes.
- **Gerenciamento de Médicos**: Cadastro, atualização, listagem e exclusão de médicos.
- **Gerenciamento de Consultas**: Agendamento, listagem e gerenciamento de consultas médicas.
- **Gerenciamento de Exames**: Registro e consulta de exames realizados.
- **Autenticação e Autorização**: Configuração de autenticação baseada em Identity.
- **Documentação da API**: Suporte a OpenAPI para documentação interativa.

## Tecnologias Utilizadas

- **.NET 9**
- **C# 13.0**
- **Entity Framework Core** com suporte a PostgreSQL
- **ASP.NET Core** para construção de APIs
- **Microsoft Identity** para autenticação e autorização

## Estrutura do Projeto

- **Controllers**: Contém os endpoints da API, como `PessoaController`, `MedicoController`, etc.
- **IoC**: Configuração de injeção de dependências.
- **Infraestrutura**: Repositórios e contexto do banco de dados.
- **Modelo**: Entidades e DTOs utilizados no sistema.

## Configuração e Execução

1. **Pré-requisitos**:
   - .NET SDK 9 instalado.
   - Banco de dados PostgreSQL configurado.

2. **Configuração do Banco de Dados**:
   - Atualize as strings de conexão no arquivo `appsettings.json`:
    <code>
     {
       "ConnectionStrings": {
         "IdentityConection": "Host=localhost;Database=IdentityDB;Username=postgres;Password=senha",
         "ConsultorioConection": "Host=localhost;Database=ConsultorioDB;Username=postgres;Password=senha"
       }
     } 
     </code>

3. **Executar Migrações**:
   - Certifique-se de que a configuração `RunMigration` no `appsettings.json` está habilitada:
     <code>
      {
         "RunMigration": true
      }
     </code>
 
   - Execute o projeto para aplicar as migrações automaticamente.

4. **Executar o Projeto**:
   - Use o comando: `dotnet run`
     

5. **Acessar a Documentação da API**:
   - Acesse `https://localhost:<port>/openapi/v1.json` para visualizar a documentação interativa.

## Estrutura de Dependências

- **Injeção de Dependências**:
  - Configurada no método `RegisterServices` em `RegistrarDependencias.cs`.
  - Exemplos:
    - `IRepositorio<T>` é implementado por `Repositorio<T>`.

## Contribuição

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues ou enviar pull requests.

## Licença

Este projeto está licenciado sob a [MIT License](LICENSE).

---

**Nota**: Certifique-se de ajustar as informações, como portas, URLs e configurações específicas, conforme necessário.
