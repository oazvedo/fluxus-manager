using FluxusManager.Application.DTOs.AuthDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Security;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Exceptions;
using FluxusManager.Domain.Interfaces;

namespace FluxusManager.Application.Services;

/// <summary>Autentica usuários e só emite tokens para vínculos ativos com empresas ativas.</summary>
public class AuthService(
    IUsuarioRepository usuarios,
    IUsuarioEmpresaRepository vinculos,
    IRepository<Empresa> empresas,
    IPasswordHasher passwordHasher,
    IJwtTokenIssuer tokenIssuer) : IAuthService
{
    public async Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var usuario = await usuarios.ObterPorEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (usuario is null || !usuario.Ativo || !passwordHasher.Verificar(request.Senha, usuario.SenhaHash))
            return null;

        var vinculosAtivos = (await vinculos.ListarPorUsuarioAsync(usuario.Id, cancellationToken)).Where(v => v.Ativo);
        if (request.EmpresaId is Guid empresaId)
            vinculosAtivos = vinculosAtivos.Where(v => v.EmpresaId == empresaId);

        foreach (var vinculo in vinculosAtivos)
        {
            var empresa = await empresas.GetByIdAsync(vinculo.EmpresaId, cancellationToken);
            if (empresa is { Ativo: true })
                return CriarToken(usuario, empresa.Id, vinculo.Perfil);
        }

        return null;
    }

    public async Task<TokenResponse> SwitchTenantAsync(Guid userId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        var usuario = await usuarios.GetByIdAsync(userId, cancellationToken);
        var vinculo = await vinculos.ObterVinculoAsync(userId, empresaId, cancellationToken);
        var empresa = await empresas.GetByIdAsync(empresaId, cancellationToken);
        if (usuario is not { Ativo: true } || vinculo is not { Ativo: true } || empresa is not { Ativo: true })
            throw new NotFoundException("Vínculo usuário-empresa ativo", $"{userId}/{empresaId}");

        return CriarToken(usuario, empresaId, vinculo.Perfil);
    }

    private TokenResponse CriarToken(Usuario usuario, Guid empresaId, string perfil)
    {
        var permissions = perfil.Trim().ToLowerInvariant() switch
        {
            "administrador" or "admin" => PermissionCatalog.All,
            "consulta" or "leitura" or "read-only" => PermissionCatalog.ReadOnly,
            _ => []
        };
        var (token, expiresIn) = tokenIssuer.Create(usuario.Id, usuario.Email, empresaId, perfil, permissions);
        return new TokenResponse(token, "Bearer", expiresIn, empresaId, perfil, permissions);
    }
}
