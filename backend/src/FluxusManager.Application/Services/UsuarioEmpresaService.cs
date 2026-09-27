using FluxusManager.Application.DTOs.UsuariosEmpresasDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Exceptions;
using FluxusManager.Domain.Interfaces;

namespace FluxusManager.Application.Services;

public class UsuarioEmpresaService(
    IUsuarioEmpresaRepository vinculos,
    IUsuarioRepository usuarios,
    IEmpresaRepository empresas,
    IUnitOfWork unitOfWork) : IUsuarioEmpresaService
{
    public async Task<UsuarioEmpresaResponse> VincularAsync(VincularUsuarioEmpresaRequest request, CancellationToken cancellationToken = default)
    {
        if (await usuarios.GetByIdAsync(request.UsuarioId, cancellationToken) is null)
            throw new NotFoundException("Usuário", request.UsuarioId);
        if (await empresas.GetByIdAsync(request.EmpresaId, cancellationToken) is null)
            throw new NotFoundException("Empresa", request.EmpresaId);
        if (await vinculos.ObterVinculoAsync(request.UsuarioId, request.EmpresaId, cancellationToken) is not null)
            throw new ConflictException("Este usuário já está vinculado à empresa.");

        var vinculo = new UsuarioEmpresa(request.UsuarioId, request.EmpresaId, request.Perfil.Trim());
        vinculos.Add(vinculo);
        await unitOfWork.CommitAsync(cancellationToken);
        return UsuarioEmpresaResponse.DeEntidade(vinculo);
    }

    public async Task<IReadOnlyList<UsuarioEmpresaResponse>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        if (!await usuarios.ExistsAsync(usuarioId, cancellationToken)) throw new NotFoundException("Usuário", usuarioId);
        return (await vinculos.ListarPorUsuarioAsync(usuarioId, cancellationToken)).Select(UsuarioEmpresaResponse.DeEntidade).ToArray();
    }

    public async Task<IReadOnlyList<UsuarioEmpresaResponse>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        if (!await empresas.ExistsAsync(empresaId, cancellationToken)) throw new NotFoundException("Empresa", empresaId);
        return (await vinculos.ListarPorEmpresaAsync(empresaId, cancellationToken)).Select(UsuarioEmpresaResponse.DeEntidade).ToArray();
    }

    public async Task<UsuarioEmpresaResponse> AtualizarPerfilAsync(Guid usuarioId, Guid empresaId, AtualizarPerfilUsuarioEmpresaRequest request, CancellationToken cancellationToken = default)
    {
        var vinculo = await BuscarAsync(usuarioId, empresaId, cancellationToken);
        vinculo.AtualizarPerfil(request.Perfil.Trim());
        await unitOfWork.CommitAsync(cancellationToken);
        return UsuarioEmpresaResponse.DeEntidade(vinculo);
    }

    public async Task DesvincularAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        vinculos.Remove(await BuscarAsync(usuarioId, empresaId, cancellationToken));
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task AtivarAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        (await BuscarAsync(usuarioId, empresaId, cancellationToken)).Ativar();
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task InativarAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        (await BuscarAsync(usuarioId, empresaId, cancellationToken)).Inativar();
        await unitOfWork.CommitAsync(cancellationToken);
    }

    private async Task<UsuarioEmpresa> BuscarAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken)
        => await vinculos.ObterVinculoAsync(usuarioId, empresaId, cancellationToken)
            ?? throw new NotFoundException("Vínculo usuário-empresa", $"{usuarioId}/{empresaId}");
}
