using FluxusManager.Application.DTOs.FiliaisDtos;
using FluxusManager.Domain.Common;

namespace FluxusManager.Application.Interfaces;

public interface IFilialService
{
    Task<FilialResponse> CriarAsync(CriarFilialRequest request, CancellationToken cancellationToken = default);
    Task<FilialResponse> ObterAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<FilialResponse>> ListarAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<FilialResponse> AtualizarAsync(Guid id, AtualizarFilialRequest request, CancellationToken cancellationToken = default);
    Task AtivarAsync(Guid id, CancellationToken cancellationToken = default);
    Task InativarAsync(Guid id, CancellationToken cancellationToken = default);
}
