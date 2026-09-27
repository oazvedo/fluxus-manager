using FluxusManager.Application.DTOs.ConvitesDtos;
using FluxusManager.Domain.Common;

namespace FluxusManager.Application.Interfaces;

public interface IConviteService
{
    Task<PagedResult<ConviteResponse>> ListarAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ConviteResponse> ObterAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ConviteResponse> CriarAsync(CriarConviteRequest request, CancellationToken cancellationToken = default);
    Task<ConviteResponse> ReenviarAsync(Guid id, CancellationToken cancellationToken = default);
    Task CancelarAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Público: quem abre o link ainda não está autenticado na empresa do convite.</summary>
    Task<ConviteDetalhesResponse> ConsultarAsync(ConsultarConviteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Público: vincula o usuário do e-mail (criando-o, se preciso) à empresa com o perfil do convite.</summary>
    Task AceitarAsync(AceitarConviteRequest request, CancellationToken cancellationToken = default);
}
