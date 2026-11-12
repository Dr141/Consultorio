using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;

namespace Consultorio.Aplicacao.Interfaces;

public interface IPacienteAppService
{
    Task<List<PacienteResponse>> ObterTodosAsync(bool incluirInativos = false);
    Task<PacienteResponse?> ObterPorIdAsync(int id);
    Task<PacienteResponse> AdicionarAsync(PacienteRequest request);
    Task<PacienteResponse> AtualizarAsync(int id, PacienteRequest request);
    /// <summary>Inativa o cadastro. O paciente e seu prontuário nunca são apagados.</summary>
    Task RemoverAsync(int id);
    Task<PacienteResponse> ReativarAsync(int id);
    Task<PacienteResponse> AtualizarAlertasAsync(int id, AlertasClinicosRequest request);
}
