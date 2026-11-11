namespace Consultorio.Dominio.Enumerados;

/// <summary>
/// Ciclo de vida do registro da consulta no prontuário.
/// Aberta: o médico ainda pode editar a evolução.
/// Finalizada: o texto fica imutável; correções só por adendo.
/// Anulada: registro feito por engano; continua no histórico com o motivo (prontuário não é apagado).
/// </summary>
public enum StatusConsulta
{
    Aberta = 1,
    Finalizada = 2,
    Anulada = 3
}
