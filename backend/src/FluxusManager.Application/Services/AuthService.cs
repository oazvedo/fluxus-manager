using FluxusManager.Application.DTOs.AuthDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Security;
using FluxusManager.Application.Options;
using Microsoft.Extensions.Options;
using FluxusManager.Domain.Common;
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
    IJwtTokenIssuer tokenIssuer,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    IOptions<RefreshTokenOptions> refreshOptions,
    IOptions<LoginOptions> loginOptions) : IAuthService
{
    public async Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var usuario = await usuarios.ObterPorEmailAsync(EnderecoEmail.Normalizar(request.Email), cancellationToken);
        var agora = clock.GetUtcNow().UtcDateTime;
        if (usuario is null || !usuario.Ativo || usuario.Bloqueado(agora))
        {
            // Também paga o custo do hash (o que domina o tempo de resposta), para não revelar se o e-mail existe.
            // Resta a diferença de um UPDATE na senha errada de um usuário real, bem menor que a do hash.
            passwordHasher.VerificarSemUsuario(request.Senha);
            return null;
        }

        if (!passwordHasher.Verificar(request.Senha, usuario.SenhaHash))
        {
            var opcoes = loginOptions.Value;
            await usuarios.RegistrarFalhaLoginAsync(usuario.Id, agora, opcoes.MaxTentativas,
                TimeSpan.FromMinutes(opcoes.BloqueioMinutos), cancellationToken);
            return null;
        }

        // Gravado junto com o refresh token, se o login for concluído.
        usuario.Desbloquear();

        var vinculosAtivos = (await vinculos.ListarPorUsuarioAsync(usuario.Id, cancellationToken)).Where(v => v.Ativo);
        if (request.EmpresaId is Guid empresaId)
            vinculosAtivos = vinculosAtivos.Where(v => v.EmpresaId == empresaId);

        foreach (var vinculo in vinculosAtivos)
        {
            var empresa = await empresas.GetByIdAsync(vinculo.EmpresaId, cancellationToken);
            if (empresa is { Ativo: true } && vinculo.Perfil is { Ativo: true } perfil && perfil.TenantId == empresa.Id)
            {
                var response = CriarToken(usuario, empresa.Id, perfil);
                var secret = SecretToken.Create();
                var expires = clock.ExpiracaoEmSegundos(TimeSpan.FromDays(refreshOptions.Value.DuracaoDias));
                refreshTokens.Add(new RefreshToken(usuario.Id, empresa.Id, Guid.CreateVersion7(), SecretToken.Hash(secret), expires));
                await unitOfWork.CommitAsync(cancellationToken);
                return response with { RefreshToken = secret, RefreshTokenExpiresAt = expires };
            }
        }

        return null;
    }

    public async Task<TokenResponse> SwitchTenantAsync(Guid userId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        var usuario = await usuarios.GetByIdAsync(userId, cancellationToken);
        var vinculo = await vinculos.ObterVinculoAsync(userId, empresaId, cancellationToken);
        var empresa = await empresas.GetByIdAsync(empresaId, cancellationToken);
        if (usuario is not { Ativo: true } || vinculo is not { Ativo: true }
            || empresa is not { Ativo: true } || vinculo.Perfil is not { Ativo: true }
            || vinculo.Perfil.TenantId != empresaId)
            throw new NotFoundException("Vínculo usuário-empresa ativo", $"{userId}/{empresaId}");

        return CriarToken(usuario, empresaId, vinculo.Perfil);
    }

    public async Task<TokenResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var original = await refreshTokens.ObterPorHashAsync(SecretToken.Hash(request.RefreshToken), cancellationToken);
        if (original is null)
            return null;

        // A revogação por reuso deve ser confirmada mesmo quando o resultado é 401; não lançar exceção
        // dentro da transação nesse caminho, pois isso desfaria a revogação da família.
        return await unitOfWork.ExecuteInTransactionAsync<TokenResponse?>(async ct =>
        {
            await refreshTokens.BloquearFamiliaAsync(original.FamiliaId, ct);
            var family = await refreshTokens.ListarFamiliaAsync(original.FamiliaId, ct);
            var token = family.SingleOrDefault(t => t.Id == original.Id);
            var now = clock.GetUtcNow().UtcDateTime;
            if (token is null || token.ExpiraEm <= now)
                return null;
            if (token.RevogadoEm is not null)
            {
                foreach (var member in family) member.Revogar(now);
                return null;
            }

            TokenResponse response;
            try
            {
                response = await SwitchTenantAsync(token.UsuarioId, request.EmpresaId ?? token.EmpresaId, ct);
            }
            catch (NotFoundException)
            {
                foreach (var member in family) member.Revogar(now);
                return null;
            }

            var secret = SecretToken.Create();
            var replacement = new RefreshToken(token.UsuarioId, response.TenantId, token.FamiliaId,
                SecretToken.Hash(secret), token.ExpiraEm);
            token.Substituir(replacement.Id, now);
            refreshTokens.Add(replacement);
            return response with { RefreshToken = secret, RefreshTokenExpiresAt = token.ExpiraEm };
        }, cancellationToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var original = await refreshTokens.ObterPorHashAsync(SecretToken.Hash(request.RefreshToken), cancellationToken);
        if (original is null)
            return;

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await refreshTokens.BloquearFamiliaAsync(original.FamiliaId, ct);
            var family = await refreshTokens.ListarFamiliaAsync(original.FamiliaId, ct);
            foreach (var token in family) token.Revogar(clock.GetUtcNow().UtcDateTime);
        }, cancellationToken);
    }

    private TokenResponse CriarToken(Usuario usuario, Guid empresaId, Perfil perfil)
    {
        var permissions = perfil.Permissoes.Select(permissao => permissao.Codigo).Distinct(StringComparer.Ordinal).ToArray();
        var (token, expiresIn) = tokenIssuer.Create(usuario.Id, usuario.Email, empresaId, perfil.Nome, permissions);
        return new TokenResponse(token, "Bearer", expiresIn, empresaId, perfil.Nome, permissions);
    }
}
