using Consultorio.Aplicacao.Configuracao;
using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;

namespace Consultorio.Aplicacao.Servicos;

/// <summary>
/// Emissão de documentos clínicos. O documento sai em nome de um médico ativo; quando o usuário logado
/// é médico (login vinculado), ele só emite e cancela documentos em seu próprio nome.
/// </summary>
public class DocumentoAppService(
    IDocumentoClinicoRepositorio documentoRepositorio,
    IPacienteRepositorio pacienteRepositorio,
    IMedicoRepositorio medicoRepositorio,
    IExameRepositorio exameRepositorio,
    OpcoesClinica opcoes) : IDocumentoAppService
{
    public async Task<DocumentoResponse> EmitirAsync(DocumentoRequest request, UsuarioAtual usuario)
    {
        var paciente = await pacienteRepositorio.ObterId(request.IdPaciente)
            ?? throw new DomainException($"Paciente com id {request.IdPaciente} não encontrado.");
        DomainException.When(!paciente.Ativo, "Paciente inativo. Reative o cadastro antes de emitir documentos.");

        var medico = await ResolverMedicoAsync(request.IdMedico, usuario);
        var cid = request.IncluirCid ? MapeadorClinico.CriarCid(request.CidCodigo, request.CidDescricao) : null;
        DomainException.When(request.IncluirCid && cid is null, "Informe o CID para incluí-lo no documento.");

        var documento = new DocumentoClinico(request.Tipo, paciente.Id, medico.Id, request.Conteudo, usuario.Identificacao,
            request.IdConsulta, request.Tipo == TipoDocumentoClinico.Atestado ? request.DiasAfastamento : null, cid);
        var salvo = await documentoRepositorio.Adicionar(documento);

        // Pedido de exames: cada linha vira um exame "Solicitado" na lista de exames do paciente.
        var examesCriados = 0;
        if (request.Tipo == TipoDocumentoClinico.PedidoExames && request.CriarExames)
        {
            foreach (var item in salvo.ItensDoPedido().Take(30))
            {
                var descricao = item.Length > 200 ? item[..200] : item;
                await exameRepositorio.Adicionar(new Exame(paciente.Id, descricao, idMedico: medico.Id));
                examesCriados++;
            }
        }

        var detalhado = await documentoRepositorio.ObterDetalhadoPorId(salvo.Id) ?? salvo;
        return Mapear(detalhado, opcoes) with { ExamesCriados = examesCriados };
    }

    public async Task<DocumentoResponse?> ObterPorIdAsync(int id)
    {
        var documento = await documentoRepositorio.ObterDetalhadoPorId(id);
        return documento is null ? null : Mapear(documento, opcoes);
    }

    public async Task<List<DocumentoResponse>> ObterPorPacienteAsync(int idPaciente)
    {
        var documentos = await documentoRepositorio.ObterPorPaciente(idPaciente);
        return documentos.Select(d => Mapear(d, opcoes)).ToList();
    }

    public async Task<DocumentoResponse> CancelarAsync(int id, string motivo, UsuarioAtual usuario)
    {
        var documento = await documentoRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Documento com id {id} não encontrado.");

        if (!usuario.EhAdministrador && documento.Medico?.EmailUsuario is not null)
        {
            DomainException.When(!documento.Medico.PertenceAoUsuario(usuario.Email),
                "Somente o médico que emitiu o documento pode cancelá-lo.");
        }

        documento.Cancelar(motivo);
        var atualizado = await documentoRepositorio.Atualizar(documento);
        return Mapear(atualizado, opcoes);
    }

    public async Task<VerificacaoDocumentoResponse> VerificarAsync(string codigo)
    {
        var documento = string.IsNullOrWhiteSpace(codigo) ? null : await documentoRepositorio.ObterPorCodigo(codigo);
        if (documento is null)
        {
            return new VerificacaoDocumentoResponse(false, null, null, null, null, null, null, false, opcoes.Nome);
        }

        return new VerificacaoDocumentoResponse(
            true,
            documento.Tipo.ObterNome(),
            documento.DtCriacao,
            documento.Medico?.Pessoa?.Nome,
            documento.Medico?.Crm?.Valor,
            Iniciais(documento.Paciente?.Pessoa?.Nome),
            documento.DiasAfastamento,
            documento.Cancelado,
            opcoes.Nome,
            documento.Assinado,
            documento.AssinadoPor,
            documento.DtAssinatura);
    }

    private async Task<Medico> ResolverMedicoAsync(int? idMedicoInformado, UsuarioAtual usuario)
    {
        var medicoDoUsuario = string.IsNullOrWhiteSpace(usuario.Email) ? null : await medicoRepositorio.ObterPorEmailUsuario(usuario.Email);
        var idMedico = idMedicoInformado ?? medicoDoUsuario?.Id
            ?? throw new DomainException("Selecione o médico responsável pelo documento.");

        if (!usuario.EhAdministrador && medicoDoUsuario is not null)
        {
            DomainException.When(medicoDoUsuario.Id != idMedico, "Você só pode emitir documentos em seu próprio nome.");
        }

        var medico = await medicoRepositorio.ObterId(idMedico)
            ?? throw new DomainException($"Médico com id {idMedico} não encontrado.");
        DomainException.When(!medico.Ativo, "Médico inativo não pode emitir documentos.");
        return medico;
    }

    /// <summary>"Maria da Silva Souza" → "M. d. S. S." (identifica sem expor o nome completo).</summary>
    private static string? Iniciais(string? nome) =>
        string.IsNullOrWhiteSpace(nome)
            ? null
            : string.Join(" ", nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => $"{char.ToUpperInvariant(p[0])}."));

    public static DocumentoResponse Mapear(DocumentoClinico d, OpcoesClinica opcoes)
    {
        var pessoa = d.Paciente?.Pessoa;
        return new DocumentoResponse(
            d.Id,
            d.Tipo.ToString(),
            d.Tipo.ObterNome(),
            d.IdPaciente,
            pessoa?.Nome ?? string.Empty,
            pessoa?.Cpf?.Formatado(),
            pessoa?.DtNasc,
            d.IdMedico,
            d.Medico?.Pessoa?.Nome ?? string.Empty,
            d.Medico?.Crm?.Valor ?? string.Empty,
            d.Medico?.Especialidades.Select(e => e.ObterNome()).ToList() ?? new List<string>(),
            d.IdConsulta,
            d.Conteudo,
            d.DiasAfastamento,
            d.CidCodigo,
            d.CidDescricao,
            d.CodigoVerificacao,
            $"{opcoes.UrlPublica.TrimEnd('/')}/api/publico/documento/{d.CodigoVerificacao}",
            d.DtCriacao,
            d.EmitidoPor,
            d.Cancelado,
            d.DtCancelamento,
            d.MotivoCancelamento,
            opcoes.Nome,
            opcoes.Endereco,
            opcoes.Telefone,
            Assinado: d.Assinado,
            DtAssinatura: d.DtAssinatura,
            AssinadoPor: d.AssinadoPor);
    }
}
