using FluxusManager.Application.DTOs.EmpresasDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Exceptions;
using FluxusManager.Domain.Interfaces;

namespace FluxusManager.Application.Services;

public class EmpresaService(IEmpresaRepository empresas, IUnitOfWork unitOfWork) : IEmpresaService
{
    public async Task<EmpresaResponse> CriarAsync(CriarEmpresaRequest request, CancellationToken cancellationToken = default)
    {
        var cnpj = Cnpj.Normalizar(request.Cnpj);

        if (await empresas.CnpjEmUsoAsync(cnpj, cancellationToken))
            throw new ConflictException($"Já existe uma empresa com o CNPJ {Cnpj.Formatar(cnpj)}.");

        var empresa = new Empresa(request.RazaoSocial.Trim(), NomeFantasia(request.NomeFantasia), cnpj);

        empresas.Add(empresa);
        await unitOfWork.CommitAsync(cancellationToken);

        return EmpresaResponse.DeEntidade(empresa);
    }

    public async Task<EmpresaResponse> ObterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var empresa = await BuscarAsync(id, cancellationToken);

        return EmpresaResponse.DeEntidade(empresa);
    }

    public async Task<PagedResult<EmpresaResponse>> ListarAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var pagina = await empresas.ListAsync(page, pageSize, cancellationToken);

        return pagina.Map(EmpresaResponse.DeEntidade);
    }

    public async Task<EmpresaResponse> AtualizarAsync(Guid id, AtualizarEmpresaRequest request, CancellationToken cancellationToken = default)
    {
        var empresa = await BuscarAsync(id, cancellationToken);

        empresa.Atualizar(request.RazaoSocial.Trim(), NomeFantasia(request.NomeFantasia));
        await unitOfWork.CommitAsync(cancellationToken);

        return EmpresaResponse.DeEntidade(empresa);
    }

    public async Task AtivarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var empresa = await BuscarAsync(id, cancellationToken);

        empresa.Ativar();
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task InativarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var empresa = await BuscarAsync(id, cancellationToken);

        empresa.Inativar();
        await unitOfWork.CommitAsync(cancellationToken);
    }

    private async Task<Empresa> BuscarAsync(Guid id, CancellationToken cancellationToken)
        => await empresas.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Empresa", id);

    // Nome fantasia é opcional: em branco vira null.
    private static string? NomeFantasia(string? nomeFantasia)
        => string.IsNullOrWhiteSpace(nomeFantasia) ? null : nomeFantasia.Trim();
}
