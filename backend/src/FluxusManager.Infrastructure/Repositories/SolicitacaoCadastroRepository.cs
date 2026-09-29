using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.Infrastructure.Repositories;

public class SolicitacaoCadastroRepository(AppDbContext context)
    : RepositoryBase<SolicitacaoCadastro>(context), ISolicitacaoCadastroRepository
{
    private static readonly SolicitacaoCadastroStatus[] Ativas =
        [SolicitacaoCadastroStatus.AguardandoVerificacao, SolicitacaoCadastroStatus.PendenteAnalise];

    public Task<List<SolicitacaoCadastro>> ListarAtivasPorCnpjOuEmailAsync(string cnpj, string email, CancellationToken cancellationToken = default)
        => Set.Include(s => s.Emails).Include(s => s.Eventos)
            .Where(s => Ativas.Contains(s.Status) && (s.Cnpj == cnpj || s.ResponsavelEmail == email))
            .ToListAsync(cancellationToken);

    public Task<SolicitacaoCadastro?> ObterAguardandoVerificacaoPorEmailAsync(string email, CancellationToken cancellationToken = default)
        => Set.Include(s => s.Emails).Include(s => s.Eventos).FirstOrDefaultAsync(
            s => s.ResponsavelEmail == email && s.Status == SolicitacaoCadastroStatus.AguardandoVerificacao, cancellationToken);

    public Task<SolicitacaoCadastro?> ObterPorVerificacaoHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => Set.Include(s => s.Emails).Include(s => s.Eventos)
            .FirstOrDefaultAsync(s => s.VerificacaoTokenHash == tokenHash, cancellationToken);

    public Task<SolicitacaoCadastro?> ObterPorAcompanhamentoHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => Set.AsNoTracking().FirstOrDefaultAsync(s => s.AcompanhamentoTokenHash == tokenHash, cancellationToken);

    public Task<SolicitacaoCadastro?> ObterDetalheAsync(Guid id, bool somenteLeitura = false, CancellationToken cancellationToken = default)
    {
        var query = Set.Include(s => s.Emails).Include(s => s.Eventos).AsSplitQuery();
        return (somenteLeitura ? query.AsNoTracking() : query).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public Task BloquearAsync(Guid id, CancellationToken cancellationToken = default)
        => Context.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({"solicitacao-cadastro:" + id}, 0))", cancellationToken);

    public Task<PagedResult<SolicitacaoCadastro>> ListarAsync(SolicitacaoCadastroStatus? status, DateTime? de, DateTime? antesDe,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().Include(s => s.Emails).AsQueryable();
        if (status is { } filtro)
            query = query.Where(s => s.Status == filtro);
        if (de is { } inicio)
            query = query.Where(s => s.CriadoEm >= inicio);
        if (antesDe is { } fim)
            query = query.Where(s => s.CriadoEm < fim);

        return ToPagedResultAsync(query, page, pageSize, cancellationToken);
    }
}
