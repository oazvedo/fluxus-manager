using FluxusManager.Domain.Entities;

namespace FluxusManager.Domain.Interfaces;

public interface IUsuarioRepository : IRepository<Usuario>
{
    /// <summary>
    /// Indica se o e-mail já pertence a algum usuário.
    /// Na atualização, informe <paramref name="ignorarId"/> para não contar o próprio usuário.
    /// </summary>
    Task<bool> EmailEmUsoAsync(string email, Guid? ignorarId = null, CancellationToken cancellationToken = default);
}

