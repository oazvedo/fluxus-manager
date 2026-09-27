using FluxusManager.Domain.Entities;

namespace FluxusManager.Domain.Interfaces;

public interface IEmpresaRepository : IRepository<Empresa>
{
    /// <summary>Indica se o CNPJ (normalizado) já pertence a alguma empresa não excluída.</summary>
    Task<bool> CnpjEmUsoAsync(string cnpj, CancellationToken cancellationToken = default);
}
