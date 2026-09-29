using FluxusManager.Domain.Entities;

namespace FluxusManager.Domain.Interfaces;

public interface ISolicitacaoCadastroEmailRepository : IRepository<SolicitacaoCadastroEmail>
{
    /// <summary>E-mails não entregues cuja próxima tentativa já venceu, dentro do limite de tentativas automáticas.</summary>
    Task<List<Guid>> ListarDevidosAsync(DateTime agora, int limite, CancellationToken cancellationToken = default);

    /// <summary>E-mails ainda não entregues de uma solicitação, independentemente do agendamento.</summary>
    Task<List<Guid>> ListarNaoEntreguesAsync(Guid solicitacaoId, CancellationToken cancellationToken = default);
}
