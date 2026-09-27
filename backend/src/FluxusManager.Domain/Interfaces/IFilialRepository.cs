using FluxusManager.Domain.Entities;

namespace FluxusManager.Domain.Interfaces;

public interface IFilialRepository : IRepository<Filial>
{
    Task<bool> CnpjEmUsoAsync(string cnpj, CancellationToken cancellationToken = default);
}
