using Consultorio.Aplicacao.Configuracao;
using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;

namespace Consultorio.Aplicacao.Servicos;

public class ProntuarioAppService(
    IPacienteRepositorio pacienteRepositorio,
    IMedicoRepositorio medicoRepositorio,
    IConsultaRepositorio consultaRepositorio,
    IExameRepositorio exameRepositorio,
    IDocumentoClinicoRepositorio? documentoRepositorio = null,
    OpcoesClinica? opcoes = null) : IProntuarioAppService
{
    public async Task<ProntuarioResponse> GerarProntuarioAsync(ProntuarioFiltroRequest filtro)
    {
        DomainException.When(filtro.IdPaciente <= 0, "Id do paciente inválido.");

        if (filtro.DataInicio.HasValue && filtro.DataFim.HasValue && filtro.DataInicio.Value > filtro.DataFim.Value)
        {
            throw new DomainException("A data inicial não pode ser maior que a data final.");
        }

        var paciente = await pacienteRepositorio.ObterDetalhadoPorId(filtro.IdPaciente)
            ?? throw new DomainException($"Paciente com id {filtro.IdPaciente} não encontrado.");

        var pacienteResponse = PacienteAppService.MapearResponse(paciente);

        MedicoResumoProntuarioResponse? medicoFiltroResponse = null;
        if (filtro.IdMedico.HasValue && filtro.IdMedico.Value > 0)
        {
            var medico = await medicoRepositorio.ObterDetalhadoPorId(filtro.IdMedico.Value)
                ?? throw new DomainException($"Médico com id {filtro.IdMedico.Value} não encontrado.");

            medicoFiltroResponse = new MedicoResumoProntuarioResponse(
                medico.Id,
                medico.Pessoa?.Nome ?? string.Empty,
                medico.Crm?.Valor ?? string.Empty,
                medico.Especialidades.Select(e => e.ObterNome()).ToList()
            );
        }

        var consultas = await consultaRepositorio.ObterPorPacienteFiltrado(
            filtro.IdPaciente,
            filtro.IdMedico,
            filtro.DataInicio,
            filtro.DataFim
        );

        var consultasMapeadas = consultas.Select(c =>
        {
            var dataAtendimento = c.Agenda is not null
                ? c.Agenda.Data.Add(c.Agenda.Hora)
                : c.DtCriacao;

            var especialidades = c.Medico?.Especialidades.Select(e => e.ObterNome()).ToList() ?? [];

            return new ConsultaProntuarioResponse(
                c.Id,
                c.IdMedico,
                c.Medico?.Pessoa?.Nome ?? "Não informado",
                c.Medico?.Crm?.Valor ?? "Não informado",
                especialidades,
                c.Observacao,
                dataAtendimento,
                c.DtCriacao,
                c.Status.ToString(),
                c.MotivoAnulacao,
                c.Adendos
                    .OrderBy(a => a.DtCriacao)
                    .Select(a => new ConsultaAdendoResponse(a.Id, a.Texto, a.Autor, a.DtCriacao))
                    .ToList(),
                c.Subjetivo,
                c.Objetivo,
                c.Avaliacao,
                c.Plano,
                c.CidCodigo,
                c.CidDescricao,
                MapeadorClinico.MapearSinaisVitais(c.SinaisVitais)
            );
        }).ToList();

        var exames = await exameRepositorio.ObterPorPacienteFiltrado(
            filtro.IdPaciente,
            filtro.IdMedico,
            filtro.DataInicio,
            filtro.DataFim
        );

        var examesMapeados = exames.Select(e =>
        {
            var dataExame = e.Agenda is not null
                ? e.Agenda.Data.Add(e.Agenda.Hora)
                : e.DtCriacao;

            var medico = e.Medico ?? e.Agenda?.Medico;
            int? idMedico = e.IdMedico ?? e.Agenda?.IdMedico;
            bool possuiLaudo = !string.IsNullOrWhiteSpace(e.Resultado);

            return new ExameProntuarioResponse(
                e.Id,
                e.Descricao,
                e.Resultado,
                possuiLaudo,
                idMedico,
                medico?.Pessoa?.Nome,
                medico?.Crm?.Valor,
                dataExame,
                e.DtCriacao,
                e.Cancelado,
                e.MotivoCancelamento,
                e.Status.ToString()
            );
        }).ToList();

        // Registros anulados/cancelados continuam no documento, mas não entram nos totais.
        int totalConsultas = consultasMapeadas.Count(c => c.Status != "Anulada");
        int totalExames = examesMapeados.Count(e => !e.Cancelado);
        int totalLaudosEmitidos = examesMapeados.Count(e => e.PossuiLaudo && !e.Cancelado);
        int totalExamesPendentes = totalExames - totalLaudosEmitidos;

        var resumo = new ProntuarioResumoResponse(
            totalConsultas,
            totalExames,
            totalLaudosEmitidos,
            totalExamesPendentes
        );

        var dataEmissao = DateTime.UtcNow;
        var numeroOficio = $"OF-{dataEmissao:yyyy}-{filtro.IdPaciente:D4}-{dataEmissao:MMddHHmm}";

        // Documentos emitidos no período (receitas, atestados, pedidos), respeitando os mesmos filtros.
        List<DocumentoResponse>? documentos = null;
        if (documentoRepositorio is not null)
        {
            var opcoesClinica = opcoes ?? new OpcoesClinica();
            var inicio = filtro.DataInicio?.Date;
            var fim = filtro.DataFim?.Date.AddDays(1);
            documentos = (await documentoRepositorio.ObterPorPaciente(filtro.IdPaciente))
                .Where(d => (!filtro.IdMedico.HasValue || filtro.IdMedico.Value <= 0 || d.IdMedico == filtro.IdMedico.Value)
                            && (!inicio.HasValue || d.DtCriacao >= inicio.Value)
                            && (!fim.HasValue || d.DtCriacao < fim.Value))
                .Select(d => DocumentoAppService.Mapear(d, opcoesClinica))
                .ToList();
        }

        return new ProntuarioResponse(
            numeroOficio,
            dataEmissao,
            pacienteResponse,
            medicoFiltroResponse,
            filtro.DataInicio,
            filtro.DataFim,
            resumo,
            consultasMapeadas,
            examesMapeados,
            documentos
        );
    }
}
