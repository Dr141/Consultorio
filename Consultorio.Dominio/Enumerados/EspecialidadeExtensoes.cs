namespace Consultorio.Dominio.Enumerados;

/// <summary>
/// Extensões para exibir as especialidades com nomes legíveis (acentuados e com espaços).
/// </summary>
public static class EspecialidadeExtensoes
{
    public static string ObterNome(this Especialidade especialidade) => especialidade switch
    {
        Especialidade.ClinicoGeral => "Clínico Geral",
        Especialidade.Cardiologista => "Cardiologista",
        Especialidade.Urologista => "Urologista",
        Especialidade.Ginecologista => "Ginecologista",
        Especialidade.Ortopedista => "Ortopedista",
        Especialidade.Pediatra => "Pediatra",
        Especialidade.Dermatologista => "Dermatologista",
        Especialidade.Neurologista => "Neurologista",
        Especialidade.Oftalmologista => "Oftalmologista",
        _ => especialidade.ToString()
    };
}
