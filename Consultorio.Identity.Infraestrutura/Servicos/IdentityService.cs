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

    /// <summary>
    /// Remove Role ao usuário.
    /// </summary>
    /// <param name="usuarioRole">Fornecer um objeto do tipo <see cref="UsuarioRoleRequest"/></param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="UsuarioCadastroResponse"/>
    /// ao final da operação.
    /// </returns>
    public async Task<UsuarioCadastroResponse> RemoverRole(UsuarioRoleRequest usuarioRole)
    {
        try
        {
            var role = Enum.GetName(usuarioRole.Role) ?? string.Empty;
            var user = await _userManager.FindByEmailAsync(usuarioRole.Email);

            if (user is IdentityUser)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    throw new Exception($"Role {role} não está cadastrada no sistema.");

                var result = await _userManager.RemoveFromRoleAsync(user, role);

                return new UsuarioCadastroResponse(result.Succeeded, result.Errors.Select(r => r.Description));
            }

            throw new Exception($"Usuário com e-mail {usuarioRole.Email}, não foi encontrado.");
        }
        catch (Exception ex)
        {
            return new UsuarioCadastroResponse(false, new List<string> { ex.Message });
        }
    }

    /// <summary>
    /// Adiciona Claim ao usuário.
    /// </summary>
    /// <param name="usuarioClaim">Fornecer um objeto do tipo <see cref="UsuarioClaimRequest"/></param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="UsuarioCadastroResponse"/>
    /// ao final da operação.
    /// </returns>
    public async Task<UsuarioCadastroResponse> AdicionarClaim(UsuarioClaimRequest usuarioClaim)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(usuarioClaim.Email);
            if (user is IdentityUser)
            {
                Claim claim = new Claim(Enum.GetName(usuarioClaim.ClaimType), Enum.GetName(usuarioClaim.ClaimValue));
                var result = await _userManager.AddClaimAsync(user, claim);

                return new UsuarioCadastroResponse(result.Succeeded, result.Errors.Select(r => r.Description));
            }

            throw new Exception($"Usuário com e-mail {usuarioClaim.Email}, não foi encontrado.");
        }
        catch (Exception ex)
        {
            return new UsuarioCadastroResponse(false, new List<string> { ex.Message });
        }
    }

    /// <summary>
    /// Remove Claim ao usuário.
    /// </summary>
    /// <param name="usuarioClaim">Fornecer um objeto do tipo <see cref="UsuarioClaimRequest"/></param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="UsuarioCadastroResponse"/>
    /// ao final da operação.
    /// </returns>
    public async Task<UsuarioCadastroResponse> RemoverClaim(UsuarioClaimRequest usuarioClaim)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(usuarioClaim.Email);
            if (user is IdentityUser)
            {
                Claim claim = new Claim(Enum.GetName(usuarioClaim.ClaimType), Enum.GetName(usuarioClaim.ClaimValue));
                var result = await _userManager.RemoveClaimAsync(user, claim);

                return new UsuarioCadastroResponse(result.Succeeded, result.Errors.Select(r => r.Description));
            }

            throw new Exception($"Usuário com e-mail {usuarioClaim.Email}, não foi encontrado.");
        }
        catch (Exception ex)
        {
            return new UsuarioCadastroResponse(false, new List<string> { ex.Message });
        }
    }

    /// <summary>
    /// Adiciona Role ao usuário.
    /// </summary>
    /// <param name="usuarioRole">Fornecer um objeto do tipo <see cref="UsuarioRoleRequest"/></param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="UsuarioCadastroResponse"/>
    /// ao final da operação.
    /// </returns>
    public async Task<UsuarioCadastroResponse> AdicionarRole(UsuarioRoleRequest usuarioRole)
    {
        try
        {
            var role = Enum.GetName(usuarioRole.Role) ?? string.Empty;
            var user = await _userManager.FindByEmailAsync(usuarioRole.Email);

            if (user is IdentityUser)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    await _roleManager.CreateAsync(new IdentityRole(role));

                var result = await _userManager.AddToRoleAsync(user, role);

                return new UsuarioCadastroResponse(result.Succeeded, result.Errors.Select(r => r.Description));
            }

            throw new Exception($"Usuário com e-mail {usuarioRole.Email}, não foi encontrado.");
        }
        catch (Exception ex)
        {
            return new UsuarioCadastroResponse(false, new List<string> { ex.Message });
        }
    }

    /// <summary>
    /// Altera a senha de usuário.
    /// </summary>
    /// <param name="usuarioLoginAtualizarSenha">Fornecer um objeto do tipo <see cref="UsuarioAtualizarSenhaResquest"/>.</param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="UsuarioCadastroResponse"/>
    /// ao final da operação.
    /// </returns>
    public async Task<UsuarioCadastroResponse> AtualizarSenha(UsuarioAtualizarSenhaResquest usuarioLoginAtualizarSenha, string email)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is IdentityUser)
        {
            var result = await _userManager.ChangePasswordAsync(user, usuarioLoginAtualizarSenha.SenhaAtual, usuarioLoginAtualizarSenha.NovaSenha);
            var usuarioResponse = new UsuarioCadastroResponse(result.Succeeded, result.Errors.Select(r => r.Description));

            return usuarioResponse;
        }

        return new UsuarioCadastroResponse(false, new List<string> { $"Usuário com e-mail {email}, não foi encontrado." });
    }

    /// <summary>
    /// Alterar a senha sem necessidade de informar a senha atual.
    /// </summary>
    /// <param name="usuarioLoginAtualizarSenha">Fornecer um objeto do tipo <see cref="UsuarioCadastroRequest"/></param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="UsuarioCadastroResponse"/>
    /// ao final da operação.
    /// </returns>
    public async Task<UsuarioCadastroResponse> AtualizarSenhaInterno(UsuarioCadastroRequest usuarioLoginAtualizarSenha)
    {
        var user = await _userManager.FindByEmailAsync(usuarioLoginAtualizarSenha.Email);

        if (user is IdentityUser)
        {
            await _userManager.RemovePasswordAsync(user);
            var result = await _userManager.AddPasswordAsync(user, usuarioLoginAtualizarSenha.Senha);
            var usuarioResponse = new UsuarioCadastroResponse(result.Succeeded, result.Errors.Select(r => r.Description));

            return usuarioResponse;
        }

        return new UsuarioCadastroResponse(false, new List<string> { $"Usuário com e-mail {usuarioLoginAtualizarSenha.Email}, não foi encontrado." });
    }

    /// <summary>
    /// Cadastra um novo usuário.
    /// </summary>
    /// <param name="usuarioCadastro">Fornecer um objeto do tipo <see cref="UsuarioCadastroRequest"/></param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="UsuarioCadastroResponse"/>
    /// ao final da operação.
    /// </returns>
    public async Task<UsuarioCadastroResponse> CadastrarUsuario(UsuarioCadastroRequest usuarioCadastro)
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

        return new UsuarioCadastroResponse(result.Succeeded, result.Errors.Select(r => r.Description));
    }

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
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                errors.Add("Essa conta está bloqueada");
            else if (result.IsNotAllowed)
                errors.Add("Essa conta não tem permissão para fazer login");
            else if (result.RequiresTwoFactor)
                errors.Add("É necessário confirmar o login no seu segundo fator de autenticação");
            else
                errors.Add("Usuário ou senha estão incorretos");
        }

        return new UsuarioLoginResponse(result.Succeeded, string.Empty, string.Empty, errors);
    }

    /// <summary>
    /// Método para renovar token.
    /// </summary>
    /// <param name="usuarioId">Fornecer uma <see cref="string"/> com o ID do cliente</param>
    /// <returns>
    /// A <see cref="Task"/> é uma operação assíncrona que retorna um <see cref="UsuarioLoginResponse"/>
    /// ao final da operação.
    /// </returns>
    public async Task<UsuarioLoginResponse> LoginSemSenha(string usuarioId)
    {
        var usuario = await _userManager.FindByIdAsync(usuarioId);

        List<string> errors = new List<string>();
        if (await _userManager.IsLockedOutAsync(usuario))
            errors.Add("Essa conta está bloqueada");
        else if (!await _userManager.IsEmailConfirmedAsync(usuario))
            errors.Add("Essa conta precisa confirmar seu e-mail antes de realizar o login");

        return errors.Count == 0 ? await GerarCredenciais(usuario.Email) : new UsuarioLoginResponse(false, string.Empty, string.Empty, errors);
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
            return new UsuariosResponse(true, string.Empty, result.ToDictionary(user => user.Email, user => user.EmailConfirmed));

        return new UsuariosResponse(false, "Não foi encontrado usuários cadastrado.", new Dictionary<string, bool>());
    }

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
    private async Task<UsuarioLoginResponse> GerarCredenciais(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        var accessTokenClaims = await ObterClaims(user, adicionarClaimsUsuario: true);
        var refreshTokenClaims = await ObterClaims(user, adicionarClaimsUsuario: false);

        var dataExpiracaoAccessToken = DateTime.Now.AddMinutes(_jwtOptions.AccessTokenExpiration);
        var dataExpiracaoRefreshToken = DateTime.Now.AddMinutes(_jwtOptions.RefreshTokenExpiration);

        var accessToken = GerarToken(accessTokenClaims, dataExpiracaoAccessToken);
        var refreshToken = GerarToken(refreshTokenClaims, dataExpiracaoRefreshToken);

        return new UsuarioLoginResponse(true, accessToken, refreshToken, new List<string>());
    }
}