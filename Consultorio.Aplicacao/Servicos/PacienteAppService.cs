using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.Interfaces.Servicos;
using Consultorio.Dominio.ValueObjects;

namespace Consultorio.Aplicacao.Servicos;

public class PacienteAppService(
    IPacienteRepositorio pacienteRepositorio,
    IPessoaRepositorio pessoaRepositorio,
    ICacheService? cacheService = null) : IPacienteAppService
{
    private const string CacheKeyTodos = "pacientes:todos";

    public async Task<List<PacienteResponse>> ObterTodosAsync(bool incluirInativos = false)
    {
        List<PacienteResponse>? response = null;
        if (cacheService is not null)
        {
            response = await cacheService.ObterAsync<List<PacienteResponse>>(CacheKeyTodos);
        }

        if (response is null)
        {
            var pacientes = await pacienteRepositorio.ObterTodosDetalhado();
            response = pacientes.Select(MapearResponse).ToList();

            if (cacheService is not null)
            {
                await cacheService.DefinirAsync(CacheKeyTodos, response);
            }
        }

        return incluirInativos ? response : response.Where(p => p.Ativo).ToList();
    }

    public async Task<PacienteResponse?> ObterPorIdAsync(int id)
    {
        string cacheKey = $"pacientes:{id}";
        if (cacheService is not null)
        {
            var emCache = await cacheService.ObterAsync<PacienteResponse>(cacheKey);
            if (emCache is not null)
                return emCache;
        }

        var paciente = await pacienteRepositorio.ObterDetalhadoPorId(id);
        if (paciente is null) return null;

        var response = MapearResponse(paciente);

        if (cacheService is not null)
        {
            await cacheService.DefinirAsync(cacheKey, response);
        }

        return response;
    }

    public async Task<PacienteResponse> AdicionarAsync(PacienteRequest request)
    {
        Pessoa pessoa;

        if (request.IdPessoa.HasValue && request.IdPessoa.Value > 0)
        {
            pessoa = await pessoaRepositorio.ObterId(request.IdPessoa.Value)
                ?? throw new DomainException($"Pessoa com id {request.IdPessoa.Value} não encontrada.");

            var pacienteExistente = await pacienteRepositorio.ObterPorIdPessoa(request.IdPessoa.Value);
            if (pacienteExistente is not null)
            {
                throw new DomainException($"A pessoa {pessoa.Nome} já está cadastrada como paciente.");
            }
        }
        else
        {
            DomainException.When(string.IsNullOrWhiteSpace(request.Nome), "Nome é obrigatório.");
            DomainException.When(string.IsNullOrWhiteSpace(request.Cpf), "CPF é obrigatório.");
            DomainException.When(!request.DtNasc.HasValue, "Data de nascimento é obrigatória.");
            DomainException.When(string.IsNullOrWhiteSpace(request.Telefone), "Telefone é obrigatório.");

            var cpfVo = new Cpf(request.Cpf!);
            var telVo = new Telefone(request.Telefone!);
            if (await pessoaRepositorio.ExisteCpf(cpfVo.Valor))
            {
                var pessoaExistente = await pessoaRepositorio.ObterPorCpf(cpfVo.Valor);
                var cadastroExistente = pessoaExistente is null ? null : await pacienteRepositorio.ObterPorIdPessoa(pessoaExistente.Id);
                if (cadastroExistente is not null && !cadastroExistente.Ativo)
                {
                    throw new DomainException($"Já existe um paciente inativo com o CPF {cpfVo.Formatado()} ({pessoaExistente!.Nome}). Reative o cadastro em vez de criar outro.");
                }
                if (pessoaExistente is null || cadastroExistente is not null)
                {
                    throw new DomainException($"Já existe uma pessoa cadastrada com o CPF {cpfVo.Formatado()}.");
                }

                pessoaExistente.Atualizar(request.Nome!, cpfVo, request.DtNasc!.Value, telVo, request.Observacao);
                pessoa = await pessoaRepositorio.Atualizar(pessoaExistente);
            }
            else
            {
                pessoa = new Pessoa(request.Nome!, cpfVo, request.DtNasc!.Value, telVo, request.Observacao);
                pessoa = await pessoaRepositorio.Adicionar(pessoa);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            pessoa.DefinirEmail(request.Email);
            pessoa = await pessoaRepositorio.Atualizar(pessoa);
        }

        var paciente = new Paciente(pessoa);
        var salvo = await pacienteRepositorio.Adicionar(paciente);

        if (cacheService is not null)
        {
            await cacheService.RemoverAsync(CacheKeyTodos);
            await cacheService.RemoverAsync($"pacientes:{salvo.Id}");
        }

        return MapearResponse(salvo);
    }

    public async Task<PacienteResponse> AtualizarAsync(int id, PacienteRequest request)
    {
        var paciente = await pacienteRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Paciente com id {id} não encontrado.");

        if (paciente.Pessoa is not null && !string.IsNullOrWhiteSpace(request.Nome))
        {
            var cpf = !string.IsNullOrWhiteSpace(request.Cpf) ? new Cpf(request.Cpf) : paciente.Pessoa.Cpf;
            var tel = !string.IsNullOrWhiteSpace(request.Telefone) ? new Telefone(request.Telefone) : paciente.Pessoa.Telefone;
            var dtNasc = request.DtNasc ?? paciente.Pessoa.DtNasc;

            if (await pessoaRepositorio.ExisteCpf(cpf.Valor, excluirId: paciente.Pessoa.Id))
            {
                throw new DomainException($"Já existe outra pessoa cadastrada com o CPF {cpf.Formatado()}.");
            }

            paciente.Pessoa.Atualizar(request.Nome, cpf, dtNasc, tel, request.Observacao ?? paciente.Pessoa.Observacao);
            paciente.Pessoa.DefinirEmail(request.Email);
            await pessoaRepositorio.Atualizar(paciente.Pessoa);
        }

        var atualizado = await pacienteRepositorio.Atualizar(paciente);

        if (cacheService is not null)
        {
            await cacheService.RemoverAsync(CacheKeyTodos);
            await cacheService.RemoverAsync($"pacientes:{id}");
        }

        return MapearResponse(atualizado);
    }

    public async Task RemoverAsync(int id)
    {
        var paciente = await pacienteRepositorio.ObterId(id)
            ?? throw new DomainException($"Paciente com id {id} não encontrado.");

        // Exclusão lógica: o prontuário deve ser preservado (Lei 13.787/2018, mínimo de 20 anos).
        paciente.Inativar();
        await pacienteRepositorio.Atualizar(paciente);
        await LimparCache(id);
    }

    public async Task<PacienteResponse> ReativarAsync(int id)
    {
        var paciente = await pacienteRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Paciente com id {id} não encontrado.");

        paciente.Reativar();
        var atualizado = await pacienteRepositorio.Atualizar(paciente);
        await LimparCache(id);
        return MapearResponse(atualizado);
    }

    public async Task<PacienteResponse> AtualizarAlertasAsync(int id, AlertasClinicosRequest request)
    {
        var paciente = await pacienteRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Paciente com id {id} não encontrado.");

        paciente.AtualizarAlertas(request.Alergias, request.MedicamentosEmUso, request.CondicoesCronicas);
        var atualizado = await pacienteRepositorio.Atualizar(paciente);
        await LimparCache(id);
        return MapearResponse(atualizado);
    }

    private async Task LimparCache(int id)
    {
        if (cacheService is not null)
        {
            await cacheService.RemoverAsync(CacheKeyTodos);
            await cacheService.RemoverAsync($"pacientes:{id}");
        }
    }

    public static PacienteResponse MapearResponse(Paciente p) =>
        new(
            p.Id,
            p.IdPessoa,
            p.Pessoa?.Nome ?? string.Empty,
            p.Pessoa?.Cpf?.Formatado() ?? string.Empty,
            p.Pessoa?.DtNasc ?? DateTime.MinValue,
            p.Pessoa?.Telefone?.Formatado() ?? string.Empty,
            p.Pessoa?.Observacao,
            p.DtCriacao,
            p.Ativo,
            p.DtInativacao,
            p.Pessoa?.Email,
            p.Alergias,
            p.MedicamentosEmUso,
            p.CondicoesCronicas
        );
}
