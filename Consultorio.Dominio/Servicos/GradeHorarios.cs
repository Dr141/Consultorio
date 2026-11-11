using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.Servicos;

/// <summary>Parâmetros para gerar horários em lote (ex.: "seg/qua, 8h–12h, de 20 em 20 minutos").</summary>
public record ParametrosGrade(
    DateTime DataInicio,
    DateTime DataFim,
    IReadOnlyCollection<DayOfWeek> DiasSemana,
    TimeSpan HoraInicio,
    TimeSpan HoraFim,
    int IntervaloMinutos,
    TimeSpan? PausaInicio = null,
    TimeSpan? PausaFim = null);

public record HorarioGerado(DateTime Data, TimeSpan Hora);

/// <summary>
/// Calcula os horários de uma grade, descartando os que caem em bloqueios ou que já existem.
/// É uma função pura: não acessa banco, o que facilita testar todas as regras.
/// </summary>
public static class GradeHorarios
{
    public const int MaximoDias = 186;
    public const int MaximoHorarios = 3000;

    public static void Validar(ParametrosGrade p)
    {
        DomainException.When(p.DataFim.Date < p.DataInicio.Date, "A data final deve ser igual ou posterior à data inicial.");
        DomainException.When((p.DataFim.Date - p.DataInicio.Date).TotalDays > MaximoDias, $"Gere no máximo {MaximoDias} dias por vez.");
        DomainException.When(p.DiasSemana.Count == 0, "Selecione ao menos um dia da semana.");
        DomainException.When(p.IntervaloMinutos < 5 || p.IntervaloMinutos > 240, "O intervalo deve ficar entre 5 e 240 minutos.");
        DomainException.When(p.HoraInicio < TimeSpan.Zero || p.HoraFim > TimeSpan.FromHours(24) || p.HoraFim <= p.HoraInicio,
            "O horário final deve ser posterior ao inicial.");
        if (p.PausaInicio.HasValue || p.PausaFim.HasValue)
        {
            DomainException.When(!p.PausaInicio.HasValue || !p.PausaFim.HasValue, "Informe início e fim da pausa.");
            DomainException.When(p.PausaFim <= p.PausaInicio, "O fim da pausa deve ser posterior ao início.");
        }
    }

    /// <summary>Horários do expediente de um dia (o último começa antes do fim do expediente e termina até ele).</summary>
    public static IEnumerable<TimeSpan> HorariosDoDia(ParametrosGrade p)
    {
        var intervalo = TimeSpan.FromMinutes(p.IntervaloMinutos);
        for (var hora = p.HoraInicio; hora + intervalo <= p.HoraFim; hora += intervalo)
        {
            var fim = hora + intervalo;
            var cruzaPausa = p.PausaInicio.HasValue && p.PausaFim.HasValue && hora < p.PausaFim && fim > p.PausaInicio;
            if (!cruzaPausa) yield return hora;
        }
    }

    public static (List<HorarioGerado> Novos, int IgnoradosExistentes, int IgnoradosBloqueio) Gerar(
        ParametrosGrade parametros,
        int idMedico,
        IEnumerable<BloqueioAgenda> bloqueios,
        IEnumerable<(DateTime Data, TimeSpan Hora)> existentes)
    {
        Validar(parametros);

        var listaBloqueios = bloqueios.ToList();
        var ocupados = existentes.Select(e => (e.Data.Date, e.Hora)).ToHashSet();
        var horariosDia = HorariosDoDia(parametros).ToList();
        var dias = parametros.DiasSemana.ToHashSet();

        var novos = new List<HorarioGerado>();
        int jaExistiam = 0, bloqueados = 0;

        for (var dia = parametros.DataInicio.Date; dia <= parametros.DataFim.Date; dia = dia.AddDays(1))
        {
            if (!dias.Contains(dia.DayOfWeek)) continue;

            if (listaBloqueios.Any(b => b.Abrange(dia, idMedico)))
            {
                bloqueados += horariosDia.Count;
                continue;
            }

            foreach (var hora in horariosDia)
            {
                if (ocupados.Contains((dia, hora)))
                {
                    jaExistiam++;
                    continue;
                }

                novos.Add(new HorarioGerado(DateTime.SpecifyKind(dia, DateTimeKind.Utc), hora));
                DomainException.When(novos.Count > MaximoHorarios, $"A grade geraria mais de {MaximoHorarios} horários. Reduza o período.");
            }
        }

        return (novos, jaExistiam, bloqueados);
    }
}
