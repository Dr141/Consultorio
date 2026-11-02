using Consultorio.Identity.Modelo.Configuracao;
using Consultorio.Identity.Modelo.DTOs.Response;
using Consultorio.Identity.Modelo.DTOs.Resquest;
using Consultorio.Identity.Modelo.DTOs.Share;
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
        var roles = await _roleManager.Roles.AsNoTracking().Select(role => role.Name!).ToListAsync();
        return roles ?? [];
    }
        
    /// <summary>
    /// Remove Roles ao usuário.
    /// </summary>
    public async Task<bool> RemoverRole(UsuarioRoleRequest usuarioRole)
    {
        var user = await _userManager.FindByEmailAsync(usuarioRole.Email);

        if (user is IdentityUser)
        {
            var role = usuarioRole.Roles?.FirstOrDefault();
            if (string.IsNullOrEmpty(role)) return false;
            var result = await _userManager.RemoveFromRoleAsync(user, role);
            return result.Succeeded ? true : throw new Exception(result.Errors?.FirstOrDefault()?.Description);
        }

        throw new Exception($"Usuário com e-mail {usuarioRole.Email}, não foi encontrado.");
    }

    /// <summary>
    /// Retorna todas as claims cadastradas.
    /// </summary>
    public Task<List<string>> ObterClaim()
    {
        var claims = new List<string>
        {
            "Categoria-Ler", "Categoria-Atualizar", "Categoria-Apagar",
            "Produto-Ler", "Produto-Atualizar", "Produto-Apagar"
        };
        return Task.FromResult(claims);
    }

    /// <summary>
    /// Adiciona Claim ao usuário.
    /// </summary>
    public async Task<bool> AdicionarClaim(UsuarioClaimRequest usuarioClaim)
    {
        var user = await _userManager.FindByEmailAsync(usuarioClaim.Email);
        if (user is IdentityUser)
        {
            var result = await _userManager.AddClaimsAsync(user, usuarioClaim.Claims.Select(x => new Claim(x.Key, x.Value)));
            return result.Succeeded;
        }

        throw new Exception($"Usuário com e-mail {usuarioClaim.Email}, não foi encontrado.");
    }

    /// <summary>
    /// Remove Claim ao usuário.
    /// </summary>
    public async Task<bool> RemoverClaim(UsuarioClaimRequest usuarioClaim)
    {
        var user = await _userManager.FindByEmailAsync(usuarioClaim.Email);
        if (user is IdentityUser)
        {
            var result = await _userManager.RemoveClaimsAsync(user, usuarioClaim.Claims.Select(x => new Claim(x.Key, x.Value)));
            return result.Succeeded;
        }

        throw new Exception($"Usuário com e-mail {usuarioClaim.Email}, não foi encontrado.");
    }

    /// <summary>
    /// Adiciona Roles ao usuário.
    /// </summary>
    public async Task<bool> AdicionarRole(UsuarioRoleRequest usuarioRole)
    {
        var user = await _userManager.FindByEmailAsync(usuarioRole.Email);

        if (user is IdentityUser)
        {
            var result = await _userManager.AddToRolesAsync(user, usuarioRole.Roles);
            return result.Succeeded;
        }

        throw new Exception($"Usuário com e-mail {usuarioRole.Email}, não foi encontrado.");
    }
    #endregion

    #region Métodos para gerenciamento de usuários
    /// <summary>
    /// Altera a senha de usuário.
    /// </summary>
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
    public async Task<bool> CadastrarUsuario(UsuarioCadastroRequest usuarioCadastro)
    {
        var identityUser = new IdentityUser
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
    /// Método para obter todos os usuários cadastrados.
    /// </summary>
    public async Task<UsuariosResponse> ObterTodosUsuarios()
    {
        var users = await _userManager.Users.AsNoTracking().ToListAsync();
        var dtos = new List<UsuarioDto>();

        foreach (var user in users)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            dtos.Add(new UsuarioDto(user.Email ?? string.Empty, user.EmailConfirmed, claims, roles));
        }

        return new UsuariosResponse(dtos);
    }
    #endregion

    #region Métodos para Autenticação
    /// <summary>
    /// Método para autenticação do usuário.
    /// </summary>
    public async Task<UsuarioLoginResponse> Login(UsuarioLoginRequest usuarioLogin)
    {
        var result = await _signInManager.PasswordSignInAsync(usuarioLogin.Email, usuarioLogin.Senha, false, true);
        if (result.Succeeded)
            return await GerarCredenciais(usuarioLogin.Email);
               
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
    public async Task<bool> Logout(string email)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(use => use.Email == email);
        if (user is not null)
        {
            await _userManager.RemoveAuthenticationTokenAsync(user, "JWT", "RefreshToken");
        }

        return true;
    }

    /// <summary>
    /// Método para renovar token.
    /// </summary>
    public async Task<UsuarioLoginResponse> LoginSemSenha(string usuarioId)
    {
        IdentityUser? user = await _userManager.Users.AsNoTracking()
                                               .FirstOrDefaultAsync(u => u.Id.Equals(usuarioId));

        if (user is null)
            throw new UnauthorizedAccessException("Token de atualização inválido");

        if (await _userManager.IsLockedOutAsync(user))
            throw new UnauthorizedAccessException("Essa conta está bloqueada");

        if (!await _userManager.IsEmailConfirmedAsync(user))
            throw new UnauthorizedAccessException("Essa conta precisa confirmar seu e-mail antes de realizar o login");

        return await GerarCredenciais(user.Email ?? string.Empty);
    }
    #endregion
        
    #region Métodos Privados para gerar token
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

    private async Task<IList<Claim>> ObterClaims(IdentityUser user, bool adicionarClaimsUsuario)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
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

    private async Task<UsuarioLoginResponse> GerarCredenciais(string email, bool gerarRefreshToken = true)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            throw new UnauthorizedAccessException($"Usuário com e-mail {email} não encontrado.");

        var accessTokenClaims = await ObterClaims(user, adicionarClaimsUsuario: true);
        var dataExpiracaoAccessToken = DateTime.Now.AddHours(_jwtOptions.AccessTokenExpiration);
        var accessToken = GerarToken(accessTokenClaims, dataExpiracaoAccessToken);
        
        var refreshTokenClaims = await ObterClaims(user, adicionarClaimsUsuario: false);
        var dataExpiracaoRefreshToken = DateTime.Now.AddDays(_jwtOptions.RefreshTokenExpiration);
        var refreshToken = gerarRefreshToken ? GerarToken(refreshTokenClaims, dataExpiracaoRefreshToken) : string.Empty;

        var roles = accessTokenClaims.Where(claim => claim.Type.Equals("role"))
                                     .Select(claim => claim.Value)
                                     .ToList();

        return new UsuarioLoginResponse(accessToken, refreshToken, roles);
    }
    #endregion
}
