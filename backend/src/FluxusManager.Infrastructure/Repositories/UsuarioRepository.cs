using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.Infrastructure.Repositories;

public class UsuarioRepository(AppDbContext context) : RepositoryBase<Usuario>(context), IUsuarioRepository
{
    public Task<bool> EmailEmUsoAsync(string email, Guid? ignorarId = null, CancellationToken cancellationToken = default)
        => Set.AnyAsync(u => u.Email == email && (ignorarId == null || u.Id != ignorarId), cancellationToken);

    public Task RegistrarFalhaLoginAsync(Guid id, DateTime agora, int maxTentativas, TimeSpan bloqueio, CancellationToken cancellationToken = default)
    {
        var inicioJanela = agora - bloqueio;
        var bloqueadoAte = agora + bloqueio;

        // O PostgreSQL trava a linha no UPDATE e reavalia as expressões sobre a versão mais recente: tentativas
        // simultâneas são contadas uma a uma. Por isso a nova contagem é repetida inline, e não calculada num CTE
        // (que leria a linha antes da trava).
        return Context.Database.ExecuteSqlAsync($"""
            UPDATE usuarios SET
                tentativas_login_falhas = CASE
                    WHEN (CASE WHEN ultima_falha_login_em > {inicioJanela} THEN tentativas_login_falhas + 1 ELSE 1 END) >= {maxTentativas}
                    THEN 0
                    ELSE (CASE WHEN ultima_falha_login_em > {inicioJanela} THEN tentativas_login_falhas + 1 ELSE 1 END)
                END,
                bloqueado_ate = CASE
                    WHEN (CASE WHEN ultima_falha_login_em > {inicioJanela} THEN tentativas_login_falhas + 1 ELSE 1 END) >= {maxTentativas}
                    THEN {bloqueadoAte}
                    ELSE bloqueado_ate
                END,
                ultima_falha_login_em = {agora}
            WHERE id = {id}
            """, cancellationToken);
    }

    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task RevogarRefreshTokensAsync(Guid usuarioId, DateTime agora, CancellationToken cancellationToken = default)
        => Context.Database.ExecuteSqlAsync($"""
            UPDATE refresh_tokens SET revogado_em = {agora}, atualizado_em = {agora}
            WHERE usuario_id = {usuarioId} AND revogado_em IS NULL
            """, cancellationToken);
}
