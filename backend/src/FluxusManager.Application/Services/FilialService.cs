using FluxusManager.Application.DTOs.FiliaisDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Options;
using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Exceptions;
using FluxusManager.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace FluxusManager.Application.Services;

public class FilialService(
    IFilialRepository filiais,
    IEmpresaRepository empresas,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork,
    IOptions<FilialOptions> options) : IFilialService
{
    public async Task<FilialResponse> CriarAsync(CriarFilialRequest request, CancellationToken cancellationToken = default)
    {
        var empresa = await EmpresaAtualAsync(cancellationToken);
        var cnpj = Cnpj.Normalizar(request.Cnpj);
        if (options.Value.ValidarRaizCnpjDaEmpresa && cnpj[..8] != empresa.Cnpj[..8])
            throw new BusinessRuleException("O CNPJ da filial deve ter a mesma raiz de CNPJ da empresa.");
        if (await filiais.CnpjEmUsoAsync(cnpj, cancellationToken))
            throw new ConflictException($"Já existe uma filial com o CNPJ {Cnpj.Formatar(cnpj)}.");

        var filial = new Filial(request.Nome.Trim(), cnpj, request.Endereco.Trim());
        filiais.Add(filial);
        await unitOfWork.CommitAsync(cancellationToken);
        return FilialResponse.DeEntidade(filial);
    }

    public async Task<FilialResponse> ObterAsync(Guid id, CancellationToken cancellationToken = default)
        => FilialResponse.DeEntidade(await BuscarAsync(id, cancellationToken));

    public async Task<PagedResult<FilialResponse>> ListarAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => (await filiais.ListAsync(page, pageSize, cancellationToken)).Map(FilialResponse.DeEntidade);

    public async Task<FilialResponse> AtualizarAsync(Guid id, AtualizarFilialRequest request, CancellationToken cancellationToken = default)
    {
        var filial = await BuscarAsync(id, cancellationToken);
        filial.Atualizar(request.Nome.Trim(), request.Endereco.Trim());
        await unitOfWork.CommitAsync(cancellationToken);
        return FilialResponse.DeEntidade(filial);
    }

    public async Task AtivarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        (await BuscarAsync(id, cancellationToken)).Ativar();
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task InativarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        (await BuscarAsync(id, cancellationToken)).Inativar();
        await unitOfWork.CommitAsync(cancellationToken);
    }

    private async Task<Filial> BuscarAsync(Guid id, CancellationToken cancellationToken)
        => await filiais.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Filial", id);

    private async Task<Empresa> EmpresaAtualAsync(CancellationToken cancellationToken)
        => await empresas.GetByIdAsync(tenantContext.TenantId ?? Guid.Empty, cancellationToken)
            ?? throw new NotFoundException("Empresa", tenantContext.TenantId ?? Guid.Empty);
}
