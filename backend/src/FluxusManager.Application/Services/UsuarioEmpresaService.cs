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
    IPerfilRepository perfis,
    ITenantContext tenant,
    IUnitOfWork unitOfWork) : IUsuarioEmpresaService
{
    public async Task<UsuarioEmpresaResponse> VincularAsync(VincularUsuarioEmpresaRequest request, CancellationToken cancellationToken = default)
    {
        ValidarTenant(request.EmpresaId);
        if (await usuarios.GetByIdAsync(request.UsuarioId, cancellationToken) is null)
            throw new NotFoundException("Usuário", request.UsuarioId);
        if (await empresas.GetByIdAsync(request.EmpresaId, cancellationToken) is null)
            throw new NotFoundException("Empresa", request.EmpresaId);
        if (await vinculos.ObterVinculoAsync(request.UsuarioId, request.EmpresaId, cancellationToken) is not null)
            throw new ConflictException("Este usuário já está vinculado à empresa.");

        var perfil = await PerfilAtivoAsync(request.PerfilId, request.EmpresaId, cancellationToken);
        var vinculo = new UsuarioEmpresa(request.UsuarioId, request.EmpresaId, perfil.Id);
        vinculo.AssociarPerfil(perfil);
        vinculos.Add(vinculo);
        await unitOfWork.CommitAsync(cancellationToken);
        return UsuarioEmpresaResponse.DeEntidade(vinculo);
    }

    public async Task<IReadOnlyList<UsuarioEmpresaResponse>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        if (!await usuarios.ExistsAsync(usuarioId, cancellationToken)) throw new NotFoundException("Usuário", usuarioId);
        return (await vinculos.ListarPorUsuarioAsync(usuarioId, cancellationToken)).Where(v => v.EmpresaId == tenant.TenantId)
            .Select(UsuarioEmpresaResponse.DeEntidade).ToArray();
    }

    public async Task<IReadOnlyList<UsuarioEmpresaResponse>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        ValidarTenant(empresaId);
        if (!await empresas.ExistsAsync(empresaId, cancellationToken)) throw new NotFoundException("Empresa", empresaId);
        return (await vinculos.ListarPorEmpresaAsync(empresaId, cancellationToken)).Select(UsuarioEmpresaResponse.DeEntidade).ToArray();
    }

    public async Task<UsuarioEmpresaResponse> AtualizarPerfilAsync(Guid usuarioId, Guid empresaId, AtualizarPerfilUsuarioEmpresaRequest request, CancellationToken cancellationToken = default)
    {
        ValidarTenant(empresaId);
        var vinculo = await BuscarAsync(usuarioId, empresaId, cancellationToken);
        vinculo.AtualizarPerfil(await PerfilAtivoAsync(request.PerfilId, empresaId, cancellationToken));
        await unitOfWork.CommitAsync(cancellationToken);
        return UsuarioEmpresaResponse.DeEntidade(vinculo);
    }

    public async Task DesvincularAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        ValidarTenant(empresaId);
        vinculos.Remove(await BuscarAsync(usuarioId, empresaId, cancellationToken));
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task AtivarAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        ValidarTenant(empresaId);
        var vinculo = await BuscarAsync(usuarioId, empresaId, cancellationToken);
        await PerfilAtivoAsync(vinculo.PerfilId, empresaId, cancellationToken);
        vinculo.Ativar();
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task InativarAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken = default)
    {
        ValidarTenant(empresaId);
        (await BuscarAsync(usuarioId, empresaId, cancellationToken)).Inativar();
        await unitOfWork.CommitAsync(cancellationToken);
    }

    private async Task<UsuarioEmpresa> BuscarAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken)
        => await vinculos.ObterVinculoAsync(usuarioId, empresaId, cancellationToken)
            ?? throw new NotFoundException("Vínculo usuário-empresa", $"{usuarioId}/{empresaId}");

    private async Task<Perfil> PerfilAtivoAsync(Guid perfilId, Guid empresaId, CancellationToken cancellationToken)
    {
        var perfil = await perfis.GetByIdAsync(perfilId, cancellationToken);
        if (perfil is not { Ativo: true } || perfil.TenantId != empresaId)
            throw new NotFoundException("Perfil ativo", perfilId);
        return perfil;
    }

    private void ValidarTenant(Guid empresaId)
    {
        if (tenant.TenantId != empresaId)
            throw new NotFoundException("Vínculo usuário-empresa", empresaId);
    }
}
