namespace FluxusManager.Application.Interfaces;

public interface IJwtTokenIssuer
{
    (string Token, int ExpiresIn) Create(Guid userId, string email, Guid tenantId, string role, IReadOnlyCollection<string> permissions);
}
