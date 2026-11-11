using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Excecoes;
using Consultorio.Server.Extensoes;
using Consultorio.Server.Filtros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Controllers.Agenda;

[Route("api/[controller]")]
[Authorize(Policy = PoliticasAcesso.Recepcao)]
[ApiController]
public class AgendaController(IAgendaAppService agendaAppService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obter todos os horários de agenda")]
    [ProducesResponseType(typeof(List<AgendaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AgendaResponse>>> ObterTodos()
    {
        return Ok(await agendaAppService.ObterTodosAsync());
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Obter horário de agenda por id")]
    [ProducesResponseType(typeof(AgendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgendaResponse>> ObterPorId(int id)
    {
        var agenda = await agendaAppService.ObterPorIdAsync(id);
        if (agenda is null) return NotFound(new { message = $"Horário de agenda com id {id} não encontrado." });
        return Ok(agenda);
    }

    [HttpGet("medico/{idMedico:int}")]
    [EndpointSummary("Obter agenda por médico e data")]
    [ProducesResponseType(typeof(List<AgendaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AgendaResponse>>> ObterPorMedicoEData(int idMedico, [FromQuery] DateTime? data)
    {
        var dataFiltro = data ?? DateTime.UtcNow.Date;
        return Ok(await agendaAppService.ObterPorMedicoEDataAsync(idMedico, dataFiltro));
    }

    [HttpGet("paciente/{idPaciente:int}")]
    [EndpointSummary("Obter agenda por paciente")]
    [ProducesResponseType(typeof(List<AgendaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AgendaResponse>>> ObterPorPaciente(int idPaciente)
    {
        return Ok(await agendaAppService.ObterPorPacienteAsync(idPaciente));
    }

    [HttpPost]
    [EndpointSummary("Cadastrar novo horário de agenda")]
    [ProducesResponseType(typeof(AgendaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgendaResponse>> Adicionar([FromBody] AgendaRequest request)
    {
        try
        {
            var criada = await agendaAppService.AdicionarAsync(request);
            return CreatedAtAction(nameof(ObterPorId), new { id = criada.Id }, criada);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Atualizar horário de agenda")]
    [ProducesResponseType(typeof(AgendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgendaResponse>> Atualizar(int id, [FromBody] AgendaRequest request)
    {
        try
        {
            var atualizada = await agendaAppService.AtualizarAsync(id, request);
            return Ok(atualizada);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/agendar/{idPaciente:int}")]
    [EndpointSummary("Agendar paciente em horário")]
    [ProducesResponseType(typeof(AgendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgendaResponse>> AgendarPaciente(int id, int idPaciente)
    {
        try
        {
            var atualizada = await agendaAppService.AgendarPacienteAsync(id, idPaciente);
            return Ok(atualizada);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/liberar")]
    [EndpointSummary("Liberar horário agendado")]
    [ProducesResponseType(typeof(AgendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgendaResponse>> LiberarHorario(int id)
    {
        try
        {
            var atualizada = await agendaAppService.LiberarHorarioAsync(id);
            return Ok(atualizada);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Excluir horário de agenda")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Remover(int id)
    {
        try
        {
            await agendaAppService.RemoverAsync(id);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("dia")]
    [EndpointSummary("Fila do dia: agendamentos de uma data (padrão: hoje), opcionalmente de um médico")]
    [ProducesResponseType(typeof(List<AgendaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AgendaResponse>>> ObterDoDia([FromQuery] DateTime? data, [FromQuery] int? idMedico)
    {
        return Ok(await agendaAppService.ObterDoDiaAsync(data ?? DateTime.Today, idMedico));
    }

    [HttpPatch("{id:int}/confirmar")]
    [EndpointSummary("Confirmar presença do paciente")]
    [ProducesResponseType(typeof(AgendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgendaResponse>> Confirmar(int id)
    {
        try
        {
            return Ok(await agendaAppService.ConfirmarAsync(id));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/chegada")]
    [EndpointSummary("Registrar chegada do paciente (check-in)")]
    [ProducesResponseType(typeof(AgendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgendaResponse>> RegistrarChegada(int id)
    {
        try
        {
            return Ok(await agendaAppService.RegistrarChegadaAsync(id));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/iniciar")]
    [EndpointSummary("Iniciar atendimento")]
    [ProducesResponseType(typeof(AgendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgendaResponse>> IniciarAtendimento(int id)
    {
        try
        {
            return Ok(await agendaAppService.IniciarAtendimentoAsync(id));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/concluir")]
    [EndpointSummary("Concluir atendimento")]
    [ProducesResponseType(typeof(AgendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgendaResponse>> ConcluirAtendimento(int id)
    {
        try
        {
            return Ok(await agendaAppService.ConcluirAtendimentoAsync(id));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/falta")]
    [EndpointSummary("Registrar falta do paciente")]
    [ProducesResponseType(typeof(AgendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgendaResponse>> RegistrarFalta(int id)
    {
        try
        {
            return Ok(await agendaAppService.RegistrarFaltaAsync(id));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/lembrete")]
    [EndpointSummary("Enviar agora o lembrete com link de confirmação ao paciente")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EnviarLembrete(int id, [FromServices] IRotinasAgendaAppService rotinas, CancellationToken cancellationToken)
    {
        try
        {
            await rotinas.EnviarLembreteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("grade")]
    [Authorize(Policy = PoliticasAcesso.Gestao)]
    [EndpointSummary("Gerar horários em lote a partir de uma grade semanal")]
    [ProducesResponseType(typeof(GradeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GradeResponse>> GerarGrade([FromBody] GradeRequest request)
    {
        try
        {
            return Ok(await agendaAppService.GerarGradeAsync(request));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("bloqueios")]
    [EndpointSummary("Bloqueios de agenda vigentes e futuros")]
    [ProducesResponseType(typeof(List<BloqueioResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BloqueioResponse>>> ObterBloqueios()
    {
        return Ok(await agendaAppService.ObterBloqueiosAsync());
    }

    [HttpPost("bloqueios")]
    [Authorize(Policy = PoliticasAcesso.Gestao)]
    [EndpointSummary("Bloquear agenda (férias, feriado, congresso). Remove horários livres e devolve os conflitos com pacientes")]
    [ProducesResponseType(typeof(BloqueioCriadoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BloqueioCriadoResponse>> CriarBloqueio([FromBody] BloqueioRequest request)
    {
        try
        {
            return Ok(await agendaAppService.CriarBloqueioAsync(request));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("bloqueios/{id:int}")]
    [Authorize(Policy = PoliticasAcesso.Gestao)]
    [EndpointSummary("Remover bloqueio de agenda")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoverBloqueio(int id)
    {
        try
        {
            await agendaAppService.RemoverBloqueioAsync(id);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
