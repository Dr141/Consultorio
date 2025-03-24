using Consultorio.Identity.Modelo.DTOs.Response;
using Consultorio.Identity.Modelo.DTOs.Resquest;

namespace Consultorio.Identity.Modelo.Interfaces.Servicos;

/// <summary>
/// Interface para padronizar a implementação da classe de autenticação com Identity.
/// </summary>
public interface IIdentityService
{
    Task<bool> CadastrarUsuario(UsuarioCadastroRequest usuarioCadastro);
    Task<UsuarioLoginResponse> Login(UsuarioLoginRequest usuarioLogin);
    Task<bool> Logout(string refreshToken);
    Task<UsuarioLoginResponse> LoginSemSenha(string usuarioId);
    Task<bool> AdicionarRole(UsuarioRoleRequest usuarioRole);
    Task<bool> RemoverRole(UsuarioRoleRequest usuarioRole);
    Task<bool> AdicionarClaim(UsuarioClaimRequest usuarioClaim);
    Task<bool> RemoverClaim(UsuarioClaimRequest usuarioClaim);
    Task<bool> AtualizarSenha(UsuarioAtualizarSenhaResquest usuarioLoginAtualizarSenha, string email);
    Task<bool> AtualizarSenhaInterno(UsuarioCadastroRequest usuarioLoginAtualizarSenha);
    Task<UsuariosResponse> ObterTodosUsuarios();
}