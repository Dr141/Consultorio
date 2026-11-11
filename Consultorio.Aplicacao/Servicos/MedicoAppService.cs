using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.Interfaces.Servicos;
using Consultorio.Dominio.ValueObjects;

namespace Consultorio.Aplicacao.Servicos;

public class MedicoAppService(
    IMedicoRepositorio medicoRepositorio,
    IPessoaRepositorio pessoaRepositorio,
    ICacheService? cacheService = null) : IMedicoAppService
{
    private const string CacheKeyTodos = "medicos:todos";

    public async Task<List<MedicoResponse>> ObterTodosAsync(bool incluirInativos = false)
    {
        List<MedicoResponse>? response = null;
        if (cacheService is not null)
        {
            response = await cacheService.ObterAsync<List<MedicoResponse>>(CacheKeyTodos);
        }

        if (response is null)
        {
            var medicos = await medicoRepositorio.ObterTodosDetalhado();
            response = medicos.Select(MapearResponse).ToList();

            if (cacheService is not null)
            {
                await cacheService.DefinirAsync(CacheKeyTodos, response);
            }
        }

        return incluirInativos ? response : response.Where(m => m.Ativo).ToList();
    }

    public async Task<MedicoResponse?> ObterPorUsuarioAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var medico = await medicoRepositorio.ObterPorEmailUsuario(email);
        return medico is null ? null : MapearResponse(medico);
    }

    public async Task<MedicoResponse?> ObterPorIdAsync(int id)
    {
        string cacheKey = $"medicos:{id}";
        if (cacheService is not null)
        {
            var emCache = await cacheService.ObterAsync<MedicoResponse>(cacheKey);
            if (emCache is not null)
                return emCache;
        }

        var medico = await medicoRepositorio.ObterDetalhadoPorId(id);
        if (medico is null) return null;

        var response = MapearResponse(medico);

        if (cacheService is not null)
        {
            await cacheService.DefinirAsync(cacheKey, response);
        }

        return response;
    }

    public async Task<MedicoResponse> AdicionarAsync(MedicoRequest request)
    {
        var crmVo = new Crm(request.Crm);
        if (await medicoRepositorio.ExisteCrm(crmVo.Valor))
        {
            throw new DomainException($"Já existe um médico cadastrado com o CRM {crmVo.Valor}.");
        }

        Pessoa pessoa;

        if (request.IdPessoa.HasValue && request.IdPessoa.Value > 0)
        {
            pessoa = await pessoaRepositorio.ObterId(request.IdPessoa.Value)
                ?? throw new DomainException($"Pessoa com id {request.IdPessoa.Value} não encontrada.");

            var medicoExistente = await medicoRepositorio.ObterPorIdPessoa(request.IdPessoa.Value);
            if (medicoExistente is not null)
            {
                throw new DomainException($"A pessoa {pessoa.Nome} já está cadastrada como médico.");
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
                var cadastroExistente = pessoaExistente is null ? null : await medicoRepositorio.ObterPorIdPessoa(pessoaExistente.Id);
                if (cadastroExistente is not null && !cadastroExistente.Ativo)
                {
                    throw new DomainException($"Já existe um médico inativo com o CPF {cpfVo.Formatado()} ({pessoaExistente!.Nome}). Reative o cadastro em vez de criar outro.");
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

        var medico = new Medico(pessoa, crmVo, request.Especialidades);
        await VincularUsuarioAsync(medico, request.EmailUsuario);
        var salvo = await medicoRepositorio.Adicionar(medico);

        if (cacheService is not null)
        {
            await cacheService.RemoverAsync(CacheKeyTodos);
            await cacheService.RemoverAsync($"medicos:{salvo.Id}");
        }

        return MapearResponse(salvo);
    }

    public async Task<MedicoResponse> AtualizarAsync(int id, MedicoRequest request)
    {
        var medico = await medicoRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Médico com id {id} não encontrado.");

        var crmVo = new Crm(request.Crm);
        if (await medicoRepositorio.ExisteCrm(crmVo.Valor, excluirId: id))
        {
            throw new DomainException($"Já existe outro médico cadastrado com o CRM {crmVo.Valor}.");
        }

        if (medico.Pessoa is not null && !string.IsNullOrWhiteSpace(request.Nome))
        {
            var cpf = !string.IsNullOrWhiteSpace(request.Cpf) ? new Cpf(request.Cpf) : medico.Pessoa.Cpf;
            var tel = !string.IsNullOrWhiteSpace(request.Telefone) ? new Telefone(request.Telefone) : medico.Pessoa.Telefone;
            var dtNasc = request.DtNasc ?? medico.Pessoa.DtNasc;

            if (await pessoaRepositorio.ExisteCpf(cpf.Valor, excluirId: medico.Pessoa.Id))
            {
                throw new DomainException($"Já existe outra pessoa cadastrada com o CPF {cpf.Formatado()}.");
            }

            medico.Pessoa.Atualizar(request.Nome, cpf, dtNasc, tel, request.Observacao ?? medico.Pessoa.Observacao);
            await pessoaRepositorio.Atualizar(medico.Pessoa);
        }

        medico.Atualizar(crmVo, request.Especialidades);
        await VincularUsuarioAsync(medico, request.EmailUsuario, excluirId: id);
        var atualizado = await medicoRepositorio.Atualizar(medico);

        if (cacheService is not null)
        {
            await cacheService.RemoverAsync(CacheKeyTodos);
            await cacheService.RemoverAsync($"medicos:{id}");
        }

        return MapearResponse(atualizado);
    }

    public async Task RemoverAsync(int id)
    {
        var medico = await medicoRepositorio.ObterId(id)
            ?? throw new DomainException($"Médico com id {id} não encontrado.");

        medico.Inativar();
        await medicoRepositorio.Atualizar(medico);
        await LimparCache(id);
    }

    public async Task<MedicoResponse> ReativarAsync(int id)
    {
        var medico = await medicoRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Médico com id {id} não encontrado.");

        medico.Reativar();
        var atualizado = await medicoRepositorio.Atualizar(medico);
        await LimparCache(id);
        return MapearResponse(atualizado);
    }

    /// <summary>Um mesmo login não pode representar dois médicos diferentes.</summary>
    private async Task VincularUsuarioAsync(Medico medico, string? email, int? excluirId = null)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var outro = await medicoRepositorio.ObterPorEmailUsuario(email);
            if (outro is not null && outro.Id != (excluirId ?? 0))
            {
                throw new DomainException($"O e-mail {email.Trim()} já está vinculado a outro médico ({outro.Pessoa?.Nome}).");
            }
        }

        medico.VincularUsuario(email);
    }

    private async Task LimparCache(int id)
    {
        if (cacheService is not null)
        {
            await cacheService.RemoverAsync(CacheKeyTodos);
            await cacheService.RemoverAsync($"medicos:{id}");
        }
    }

    public static MedicoResponse MapearResponse(Medico m) =>
        new(
            m.Id,
            m.IdPessoa,
            m.Pessoa?.Nome ?? string.Empty,
            m.Pessoa?.Cpf?.Formatado() ?? string.Empty,
            m.Pessoa?.DtNasc ?? DateTime.MinValue,
            m.Pessoa?.Telefone?.Formatado() ?? string.Empty,
            m.Crm?.Valor ?? string.Empty,
            m.Especialidades.ToList(),
            m.DtCriacao,
            m.Pessoa?.Observacao,
            m.Ativo,
            m.EmailUsuario
        );
}
