using Consultorio.Identity.Modelo.Configuracao;
using Consultorio.Identity.Modelo.DTOs.Response;
using Consultorio.Identity.Modelo.DTOs.Resquest;
using Consultorio.Identity.Modelo.Interfaces.Servicos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Consultorio.Identity.Infraestrutura.Servicos;

/// <summary>
/// Classe para gerenciar autenticação com Identity.
/// </summary>
public class IdentityService : IIdentityService
{
    #region Propriedades
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly JwtOptions _jwtOptions;
    private readonly RoleManager<IdentityRole> _roleManager;
    #endregion

    #region Construtor
    /// <summary>
    /// Construtor para iniciar a classe <see cref="IdentityService"/>
    /// com todos os objetos necessário.
    /// </summary>
    /// <param name="signInManager">Aguarda um objeto <see cref="SignInManager"/> do tipo <see cref="IdentityUser"/></param>
    /// <param name="userManager">Aguarda um objeto <see cref="UserManager"/> do tipo <see cref="IdentityUser"/></param>
    /// <param name="jwtOptions">Aguarda um objeto <see cref="IOptions"/> do tipo <see cref="JwtOptions"/></param>
    public IdentityService(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IOptions<JwtOptions> jwtOptions, RoleManager<IdentityRole> roleManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _jwtOptions = jwtOptions.Value;
        _roleManager = roleManager;
    }
    #endregion

    #region Métodos Claims e Roles
    /// <summary>
    /// Retorna todas a roles cadastradas.
    /// </summary>
    /// <returns>Retorna uma <see cref="List{string}"/> com todas as roles.</returns>
    public async Task<List<string>> ObterRoles()
    {
        return await _roleManager.Roles.AsNoTracking().Select(role => role.Name).ToListAsync() ?? [];
    }
        
    /// <summary>
    /// Remove Roles ao usuário.
    /// </summary>
    /// <param name="usuarioRole">Fornecer um objeto do tipo <see cref="UsuarioRoleRequest"/></param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="bool"/>
    /// ao final da operação.
    /// </returns>
    public async Task<bool> RemoverRole(UsuarioRoleRequest usuarioRole)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(usuarioRole.Email);

            if (user is IdentityUser)
            {
                var result = await _userManager.RemoveFromRoleAsync(user, usuarioRole.Roles.FirstOrDefault());
                return result.Succeeded ? true : throw new Exception(result.Errors?.FirstOrDefault()?.Description);
            }

            throw new Exception($"Usuário com e-mail {usuarioRole.Email}, não foi encontrado.");
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Retorna todas a claims cadastradas.
    /// </summary>
    /// <returns>Retorna uma <see cref="List{string}"/> com todas as claims.</returns>
    public Task<List<string>> ObterClaim()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Adiciona Claim ao usuário.
    /// </summary>
    /// <param name="usuarioClaim">Fornecer um objeto do tipo <see cref="UsuarioClaimRequest"/></param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="bool"/>
    /// ao final da operação.
    /// </returns>
    public async Task<bool> AdicionarClaim(UsuarioClaimRequest usuarioClaim)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(usuarioClaim.Email);
            if (user is IdentityUser)
            {
                var result = await _userManager.AddClaimsAsync(user, usuarioClaim.Claims.Select(x => new Claim(x.Key, x.Value)));
                return result.Succeeded;
            }

            throw new Exception($"Usuário com e-mail {usuarioClaim.Email}, não foi encontrado.");
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Remove Claim ao usuário.
    /// </summary>
    /// <param name="usuarioClaim">Fornecer um objeto do tipo <see cref="UsuarioClaimRequest"/></param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="bool"/>
    /// ao final da operação.
    /// </returns>
    public async Task<bool> RemoverClaim(UsuarioClaimRequest usuarioClaim)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(usuarioClaim.Email);
            if (user is IdentityUser)
            {
                var result = await _userManager.RemoveClaimsAsync(user, usuarioClaim.Claims.Select(x => new Claim(x.Key, x.Value)));
                return result.Succeeded;
            }

            throw new Exception($"Usuário com e-mail {usuarioClaim.Email}, não foi encontrado.");
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Adiciona Roles ao usuário.
    /// </summary>
    /// <param name="usuarioRole">Fornecer um objeto do tipo <see cref="UsuarioRoleRequest"/></param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="bool"/>
    /// ao final da operação.
    /// </returns>
    public async Task<bool> AdicionarRole(UsuarioRoleRequest usuarioRole)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(usuarioRole.Email);

            if (user is IdentityUser)
            {
                var result = await _userManager.AddToRolesAsync(user, usuarioRole.Roles);
                return result.Succeeded;
            }

            throw new Exception($"Usuário com e-mail {usuarioRole.Email}, não foi encontrado.");
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region Métodos para gerenciamento de usuários
    /// <summary>
    /// Altera a senha de usuário.
    /// </summary>
    /// <param name="usuarioLoginAtualizarSenha">Fornecer um objeto do tipo <see cref="UsuarioAtualizarSenhaResquest"/>.</param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="bool"/>
    /// ao final da operação.
    /// </returns>
    public async Task<bool> AtualizarSenha(UsuarioAtualizarSenhaResquest usuarioLoginAtualizarSenha, string email)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is IdentityUser)
        {
            var result = await _userManager.ChangePasswordAsync(user, usuarioLoginAtualizarSenha.SenhaAtual, usuarioLoginAtualizarSenha.NovaSenha);            

            return result.Succeeded;
        }

        throw new UnauthorizedAccessException($"Usuário com e-mail {email}, não foi encontrado.");
    }

    /// <summary>
    /// Alterar a senha sem necessidade de informar a senha atual.
    /// </summary>
    /// <param name="usuarioLoginAtualizarSenha">Fornecer um objeto do tipo <see cref="UsuarioCadastroRequest"/></param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="bool"/>
    /// ao final da operação.
    /// </returns>
    public async Task<bool> AtualizarSenhaInterno(UsuarioCadastroRequest usuarioLoginAtualizarSenha)
    {
        var user = await _userManager.FindByEmailAsync(usuarioLoginAtualizarSenha.Email);

        if (user is IdentityUser)
        {
            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, usuarioLoginAtualizarSenha.Senha);

            return result.Succeeded;
        }

        throw new ArgumentException($"Usuário com e-mail {usuarioLoginAtualizarSenha.Email}, não foi encontrado.");
    }

    /// <summary>
    /// Cadastra um novo usuário.
    /// </summary>
    /// <param name="usuarioCadastro">Fornecer um objeto do tipo <see cref="UsuarioCadastroRequest"/></param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="bool"/>
    /// ao final da operação.
    /// </returns>
    public async Task<bool> CadastrarUsuario(UsuarioCadastroRequest usuarioCadastro)
    {
        IdentityUser identityUser = new IdentityUser
        {
            UserName = usuarioCadastro.Email,
            Email = usuarioCadastro.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(identityUser, usuarioCadastro.Senha);
        if (result.Succeeded)
            await _userManager.SetLockoutEnabledAsync(identityUser, false);

        return result.Succeeded;
    }

    /// <summary>
    /// Método para obter todos os usuário cadastrados.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="UsuariosResponse"/>
    /// ao final da operação.
    /// </returns>
    public async Task<UsuariosResponse> ObterTodosUsuarios()
    {
        var result = await _userManager.Users.AsNoTracking().ToListAsync();

        if (result is List<IdentityUser>)
            return new UsuariosResponse(result.Select(x => new UsuarioDto(x.Email, x.EmailConfirmed,
                                                                                   _userManager.GetClaimsAsync(x)
                                                                                               .Result,
                                                                                   _userManager.GetRolesAsync(x)
                                                                                               .Result
                                                                         )
                                                            )
            );

        throw new Exception("Não foi encontrado usuários cadastrado.");
    }
    #endregion

    #region Métodos para Autenticação
    /// <summary>
    /// Método para autenticação do usuário.
    /// </summary>
    /// <param name="usuarioLogin">Fornecer um objeto do tipo <see cref="UsuarioLoginRequest"/></param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="UsuarioLoginResponse"/>
    /// ao final da operação.
    /// </returns>
    public async Task<UsuarioLoginResponse> Login(UsuarioLoginRequest usuarioLogin)
    {
        var result = await _signInManager.PasswordSignInAsync(usuarioLogin.Email, usuarioLogin.Senha, false, true);
        if (result.Succeeded)
            return await GerarCredenciais(usuarioLogin.Email);
               
        List<string> errors = new List<string>();
        if (result.IsLockedOut)
            throw new UnauthorizedAccessException("Essa conta está bloqueada");
        else if (result.IsNotAllowed)
            throw new UnauthorizedAccessException("Essa conta não tem permissão para fazer login");
        else if (result.RequiresTwoFactor)
            throw new UnauthorizedAccessException("É necessário confirmar o login no seu segundo fator de autenticação");
        else
            throw new ArgumentException("Usuário ou senha estão incorretos");
    }

    /// <summary>
    /// Método para realizar logout.
    /// </summary>
    /// /// <param name="refreshToken">Fornecer uma <see cref="string"/> com o refresh token</param>
    /// <returns>O método retorna um <see cref="bool"/></returns>
    public async Task<bool> Logout(string email)
    {
        var user = await _userManager.Users.Where(use => use.Email.Equals(email)).FirstAsync();

        if (user is IdentityUser)
        {
            await _userManager.RemoveAuthenticationTokenAsync(user, "JWT", "RefreshToken");
        }

        return true;
    }

    /// <summary>
    /// Método para renovar token.
    /// </summary>
    /// <param name="usuarioId">Fornecer uma <see cref="string"/> com o refresh token</param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="UsuarioLoginResponse"/>
    /// ao final da operação.
    /// </returns>
    public async Task<UsuarioLoginResponse> LoginSemSenha(string usuarioId)
    {
        IdentityUser? user = _userManager.Users.AsNoTracking()
                                               .FirstOrDefault(u => u.Id.Equals(usuarioId));

        if(user is not IdentityUser)
            throw new UnauthorizedAccessException("Token de atualização inválido");

        if (await _userManager.IsLockedOutAsync(user))
            throw new UnauthorizedAccessException("Essa conta está bloqueada");

        if (!await _userManager.IsEmailConfirmedAsync(user))
            throw new UnauthorizedAccessException("Essa conta precisa confirmar seu e-mail antes de realizar o login");

        return await GerarCredenciais(user.Email);
    }
    #endregion
        
    #region Métodos Privados para gerar token
    /// <summary>
    /// Método para gerar token.
    /// </summary>
    /// <param name="claims">Fornecer as politicas que se aplicaram ao usuário.</param>
    /// <param name="dataExpiracao">Fornecer o periodo de validade do token.</param>
    /// <returns>
    /// O método retornar uma <see cref="string"/> com o token.
    /// </returns>
    private string GerarToken(IEnumerable<Claim> claims, DateTime dataExpiracao)
    {
        var jwt = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: dataExpiracao,
            signingCredentials: _jwtOptions.SigningCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    /// <summary>
    /// Método para obter as politicas de usuário.
    /// </summary>
    /// <param name="user">Fornecer um usuário do tipo <see cref="IdentityUser"/></param>
    /// <param name="adicionarClaimsUsuario">Se <see cref="true"/> pega todas as politicas cadastrada na base de dados, se não aplica apenas as politicas padrão.</param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna uma <see cref="IList"/> de <see cref="Claim"/>
    /// ao final da operação.
    /// </returns>
    private async Task<IList<Claim>> ObterClaims(IdentityUser user, bool adicionarClaimsUsuario)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Nbf, ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds().ToString())
        };

        if (adicionarClaimsUsuario)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            claims.AddRange(userClaims);

            foreach (var role in roles)
                claims.Add(new Claim("role", role));
        }

        return claims;
    }

    /// <summary>
    /// Método para gerar as credencias do usuário.
    /// </summary>
    /// <param name="email">Fornecer o e-mail do usuário.</param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="UsuarioLoginResponse"/>
    /// ao final da operação.
    /// </returns>
    private async Task<UsuarioLoginResponse> GerarCredenciais(string email, bool gerarRefreshToken = true)
    {
        var user = await _userManager.FindByEmailAsync(email);
        var accessTokenClaims = await ObterClaims(user, adicionarClaimsUsuario: true);
        var dataExpiracaoAccessToken = DateTime.Now.AddHours(_jwtOptions.AccessTokenExpiration);
        var accessToken = GerarToken(accessTokenClaims, dataExpiracaoAccessToken);
        
        var refreshTokenClaims = await ObterClaims(user, adicionarClaimsUsuario: false);
        var dataExpiracaoRefreshToken = DateTime.Now.AddHours(_jwtOptions.RefreshTokenExpiration);
        // Para evitar a geração de token de refresh, caso o usuário estar usando um token de refresh.
        var refreshToken = gerarRefreshToken ? GerarToken(refreshTokenClaims, dataExpiracaoRefreshToken) : string.Empty;
        //por enquanto o token não sera salvo no db
        //await _userManager.SetAuthenticationTokenAsync(user, "JWT", "RefreshToken", refreshToken);
        var testet = accessTokenClaims.Where(claim => claim.Type.Equals("role"))
                                                                                    .Select(claim => claim.Value).ToList();
        return new UsuarioLoginResponse(accessToken, refreshToken, accessTokenClaims.Where(claim => claim.Type.Equals("role"))
                                                                                    .Select(claim => claim.Value)
                                                                                    .ToList()
        );
    }
    #endregion
}