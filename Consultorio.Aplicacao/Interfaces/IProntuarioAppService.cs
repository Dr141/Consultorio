using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;

namespace Consultorio.Aplicacao.Interfaces;

public interface IProntuarioAppService
{
    Task<ProntuarioResponse> GerarProntuarioAsync(ProntuarioFiltroRequest filtro);
}
