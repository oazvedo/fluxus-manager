using FluxusManager.Application.DTOs.AuthDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Security;
using FluxusManager.Application.Options;
using FluxusManager.Application.Email;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
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
    IOptions<LoginOptions> loginOptions,
    IPasswordResetTokenRepository passwordResetTokens,
    IEmailSender emailSender,
    IOptions<FrontendOptions> frontendOptions,
    IOptions<PasswordResetOptions> passwordResetOptions,
    IAdministradorPlataformaRepository administradoresPlataforma,
    ILogger<AuthService> logger) : IAuthService
{
    private DateTime Agora => clock.GetUtcNow().UtcDateTime;

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
        foreach (var vinculo in vinculosAtivos)
        {
            var empresa = await empresas.GetByIdAsync(vinculo.EmpresaId, cancellationToken);
            if (empresa is { Ativo: true } && vinculo.Perfil is { Ativo: true } perfil && perfil.TenantId == empresa.Id)
            {
                var response = await CriarTokenAsync(usuario, empresa.Id, perfil, cancellationToken);
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

        return await CriarTokenAsync(usuario, empresaId, vinculo.Perfil, cancellationToken);
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

    public async Task EsquecerSenhaAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var usuario = await usuarios.ObterPorEmailAsync(EnderecoEmail.Normalizar(request.Email), cancellationToken);
        if (usuario is not { Ativo: true })
            return;

        var token = SecretToken.Create();
        var validade = TimeSpan.FromMinutes(passwordResetOptions.Value.ValidadeMinutos);
        var expiraEm = clock.ExpiracaoEmSegundos(validade);
        var mensagem = EmailTemplates.RecuperacaoSenha(usuario.Email, usuario.Nome,
            frontendOptions.Value.Link($"redefinir-senha?token={token}"), validade);

        passwordResetTokens.Add(new PasswordResetToken(usuario.Id, SecretToken.Hash(token), expiraEm));
        await unitOfWork.CommitAsync(cancellationToken);
        try
        {
            await emailSender.EnviarAsync(mensagem, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A resposta do endpoint público não revela se o e-mail existe nem se houve falha SMTP.
            logger.LogError("Falha ao enviar e-mail de recuperação de senha.");
        }
    }

    public async Task RedefinirSenhaAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var agora = Agora;
            var token = await passwordResetTokens.ObterParaUsoAsync(SecretToken.Hash(request.Token), agora, ct)
                ?? throw new BusinessRuleException("O link de redefinição é inválido ou expirou. Solicite outro.");
            var usuario = await usuarios.GetByIdAsync(token.UsuarioId, ct);
            if (usuario is not { Ativo: true })
                throw new BusinessRuleException("O link de redefinição é inválido ou expirou. Solicite outro.");

            token.Usar(agora);
            usuario.AlterarSenha(passwordHasher.Hash(request.NovaSenha));
            await usuarios.RevogarRefreshTokensAsync(usuario.Id, agora, ct);
        }, cancellationToken);
    }

    public async Task TrocarSenhaAsync(Guid usuarioId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var usuario = await usuarios.GetByIdAsync(usuarioId, cancellationToken);
        if (usuario is not { Ativo: true } || !passwordHasher.Verificar(request.SenhaAtual, usuario.SenhaHash))
            throw new BusinessRuleException("A senha atual está incorreta.");

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var agora = Agora;
            usuario.AlterarSenha(passwordHasher.Hash(request.NovaSenha));
            await usuarios.RevogarRefreshTokensAsync(usuario.Id, agora, ct);
        }, cancellationToken);
    }

    private async Task<TokenResponse> CriarTokenAsync(Usuario usuario, Guid empresaId, Perfil perfil, CancellationToken cancellationToken)
    {
        var permissions = perfil.Permissoes.Select(permissao => permissao.Codigo).Distinct(StringComparer.Ordinal).ToArray();
        var (token, expiresIn) = tokenIssuer.Create(usuario.Id, usuario.Email, empresaId, perfil.Nome, permissions);
        var administradorPlataforma = await administradoresPlataforma.EhAdministradorAsync(usuario.Id, cancellationToken);
        return new TokenResponse(token, "Bearer", expiresIn, empresaId, perfil.Nome, permissions,
            AdministradorPlataforma: administradorPlataforma);
    }
}
