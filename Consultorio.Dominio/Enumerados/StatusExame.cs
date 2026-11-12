namespace Consultorio.Dominio.Enumerados;

/// <summary>Ciclo do exame: Solicitado → Realizado (coleta/execução) → Laudado → Entregue ao paciente.</summary>
public enum StatusExame
{
    Solicitado = 1,
    Realizado = 2,
    Laudado = 3,
    Entregue = 4
}
