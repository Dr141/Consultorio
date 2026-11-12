using System.ComponentModel.DataAnnotations;
using Consultorio.Dominio.Enumerados;

namespace Consultorio.Aplicacao.DTOs.Request;

public record DocumentoRequest(
    [Required] TipoDocumentoClinico Tipo,
    [Required] int IdPaciente,
    [Required(ErrorMessage = "Informe o conteúdo do documento")]
    [StringLength(8000, MinimumLength = 2, ErrorMessage = "O conteúdo deve ter entre 2 e 8000 caracteres")]
    string Conteudo,
    /// <summary>Opcional: se omitido, usa o médico vinculado ao usuário logado.</summary>
    int? IdMedico = null,
    int? IdConsulta = null,
    [Range(1, 365, ErrorMessage = "Dias de afastamento entre 1 e 365")]
    int? DiasAfastamento = null,
    /// <summary>Incluir o CID no documento (em atestados, apenas com autorização do paciente).</summary>
    bool IncluirCid = false,
    [StringLength(10)] string? CidCodigo = null,
    [StringLength(200)] string? CidDescricao = null,
    /// <summary>Pedido de exames: cria também os exames (um por linha) na lista de exames do paciente.</summary>
    bool CriarExames = true
);
