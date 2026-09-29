using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.Infrastructure.Repositories;

public class SolicitacaoCadastroEmailRepository(AppDbContext context)
    : RepositoryBase<SolicitacaoCadastroEmail>(context), ISolicitacaoCadastroEmailRepository
{
    public Task<List<Guid>> ListarDevidosAsync(DateTime agora, int limite, CancellationToken cancellationToken = default)
    {
        var interrompido = agora - SolicitacaoCadastroEmail.EnvioInterrompidoApos;
        return Set.AsNoTracking()
            .Where(e => e.Tentativas < SolicitacaoCadastroEmail.MaxTentativasAutomaticas
                && (e.Status == SolicitacaoCadastroEmailStatus.Pendente || e.Status == SolicitacaoCadastroEmailStatus.Falhou)
                && e.ProximaTentativaEm <= agora
                || e.Status == SolicitacaoCadastroEmailStatus.Enviando && e.UltimaTentativaEm <= interrompido)
            .OrderBy(e => e.ProximaTentativaEm)
            .Select(e => e.Id)
            .Take(limite)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Guid>> ListarNaoEntreguesAsync(Guid solicitacaoId, CancellationToken cancellationToken = default)
        => Set.AsNoTracking()
            .Where(e => e.SolicitacaoId == solicitacaoId && e.Status != SolicitacaoCadastroEmailStatus.Enviado)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);
}
