using Consultorio.Dominio.Interfaces.Servicos;
using Microsoft.AspNetCore.DataProtection;

namespace Consultorio.Server.Servicos;

/// <summary>
/// Criptografia dos segredos guardados no banco (arquivo e senha do certificado digital) com o
/// ASP.NET Core Data Protection. As chaves ficam na pasta "DataProtection:Pasta" (volume do Docker):
/// sem elas os certificados guardados não podem ser lidos e precisam ser enviados de novo.
/// </summary>
public class ProtetorDados(IDataProtectionProvider provedor) : IProtetorDados
{
    private readonly IDataProtector _protetor = provedor.CreateProtector("Consultorio.CertificadoDigital.v1");

    public byte[] Proteger(byte[] dados) => _protetor.Protect(dados);

    public byte[] Desproteger(byte[] dadosProtegidos) => _protetor.Unprotect(dadosProtegidos);
}
