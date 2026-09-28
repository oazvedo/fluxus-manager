using FluxusManager.Domain.Entities;

namespace FluxusManager.Domain.Interfaces;

public interface IUsuarioRepository : IRepository<Usuario>
{
    /// <summary>
    /// Indica se o e-mail já pertence a algum usuário.
    /// Na atualização, informe <paramref name="ignorarId"/> para não contar o próprio usuário.
    /// </summary>
    Task<bool> EmailEmUsoAsync(string email, Guid? ignorarId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Conta uma senha errada num único UPDATE atômico (grava direto, sem o <see cref="IUnitOfWork"/>).
    /// Falhas mais antigas que <paramref name="bloqueio"/> não contam; ao chegar a <paramref name="maxTentativas"/>,
    /// bloqueia por <paramref name="bloqueio"/> e zera a contagem.
    /// </summary>
    Task RegistrarFalhaLoginAsync(Guid id, DateTime agora, int maxTentativas, TimeSpan bloqueio, CancellationToken cancellationToken = default);

    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default);

    Task RevogarRefreshTokensAsync(Guid usuarioId, DateTime agora, CancellationToken cancellationToken = default);
}
