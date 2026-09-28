using FluxusManager.Domain.Entities;

namespace FluxusManager.Domain.Interfaces;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> ObterParaUsoAsync(string hash, DateTime agora, CancellationToken cancellationToken = default);
    void Add(PasswordResetToken token);
}
