using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Common;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.Infrastructure.Repositories;

public class PerfilRepository(AppDbContext context) : RepositoryBase<Perfil>(context), IPerfilRepository
{
    public override Task<PagedResult<Perfil>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => ToPagedResultAsync(Set.AsNoTracking().Include(perfil => perfil.Permissoes), page, pageSize, cancellationToken);

    public Task<Perfil?> ObterComPermissoesAsync(Guid id, CancellationToken cancellationToken = default)
        => Set.Include(perfil => perfil.Permissoes).FirstOrDefaultAsync(perfil => perfil.Id == id, cancellationToken);

    public Task<Perfil?> ObterAtivoAsync(Guid id, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(perfil => perfil.Id == id && perfil.Ativo, cancellationToken);

    public Task<Perfil?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
        => Set.FirstOrDefaultAsync(perfil => perfil.NomeNormalizado == nome.Trim().ToUpperInvariant(), cancellationToken);

    public Task<bool> TemConvitesPendentesAsync(Guid id, CancellationToken cancellationToken = default)
        => Context.Set<Convite>().AnyAsync(convite => convite.PerfilId == id && convite.Status == ConviteStatus.Pendente, cancellationToken);

    public Task<bool> TemVinculosAsync(Guid id, CancellationToken cancellationToken = default)
        => Context.Set<UsuarioEmpresa>().AnyAsync(vinculo => vinculo.PerfilId == id && vinculo.Ativo, cancellationToken);
}
