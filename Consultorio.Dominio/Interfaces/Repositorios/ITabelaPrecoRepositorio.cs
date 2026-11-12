using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface ITabelaPrecoRepositorio : IRepositorio<TabelaPreco>
{
    Task<TabelaPreco?> ObterPor(TipoAtendimento tipo, int? idConvenio);
    Task<List<TabelaPreco>> ObterTodosDetalhado();
}
