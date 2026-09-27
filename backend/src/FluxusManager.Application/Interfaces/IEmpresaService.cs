using FluxusManager.Application.DTOs.EmpresasDtos;
using FluxusManager.Domain.Common;

namespace FluxusManager.Application.Interfaces;

public interface IEmpresaService
{
    Task<EmpresaResponse> CriarAsync(CriarEmpresaRequest request, CancellationToken cancellationToken = default);

    Task<EmpresaResponse> ObterAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<EmpresaResponse>> ListarAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<EmpresaResponse> AtualizarAsync(Guid id, AtualizarEmpresaRequest request, CancellationToken cancellationToken = default);

    Task AtivarAsync(Guid id, CancellationToken cancellationToken = default);

    Task InativarAsync(Guid id, CancellationToken cancellationToken = default);
}
