using FluxusManager.Application.DTOs.PerfisDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Security;
using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Exceptions;
using FluxusManager.Domain.Interfaces;

namespace FluxusManager.Application.Services;

public class PerfilService(IPerfilRepository perfis, ITenantContext tenant, IUnitOfWork unitOfWork) : IPerfilService
{
    public async Task<PagedResult<PerfilResponse>> ListarAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => (await perfis.ListAsync(page, pageSize, cancellationToken)).Map(PerfilResponse.DeEntidade);

    public async Task<PerfilResponse> ObterAsync(Guid id, CancellationToken cancellationToken = default)
        => PerfilResponse.DeEntidade(await BuscarCompletoAsync(id, cancellationToken));

    public async Task<PerfilResponse> CriarAsync(CriarPerfilRequest request, CancellationToken cancellationToken = default)
    {
        var nome = request.Nome.Trim();
        if (await perfis.ObterPorNomeAsync(nome, cancellationToken) is not null)
            throw new ConflictException($"Já existe um perfil com o nome '{nome}' nesta empresa.");
        ValidarPermissoes(request.Permissoes);
        var tenantId = tenant.TenantId ?? throw new InvalidOperationException("Não há empresa selecionada para criar o perfil.");
        var descricao = NormalizarDescricao(request.Descricao);
        var perfil = new Perfil(tenantId, nome, descricao);
        perfil.Atualizar(nome, descricao, request.Permissoes);
        perfis.Add(perfil);
        await unitOfWork.CommitAsync(cancellationToken);
        return PerfilResponse.DeEntidade(perfil);
    }

    public async Task<PerfilResponse> AtualizarAsync(Guid id, AtualizarPerfilRequest request, CancellationToken cancellationToken = default)
    {
        var perfil = await BuscarCompletoAsync(id, cancellationToken);
        var nome = request.Nome.Trim();
        var duplicado = await perfis.ObterPorNomeAsync(nome, cancellationToken);
        if (duplicado is not null && duplicado.Id != perfil.Id)
            throw new ConflictException($"Já existe um perfil com o nome '{nome}' nesta empresa.");
        ValidarPermissoes(request.Permissoes);
        perfil.Atualizar(nome, NormalizarDescricao(request.Descricao), request.Permissoes);
        await unitOfWork.CommitAsync(cancellationToken);
        return PerfilResponse.DeEntidade(perfil);
    }

    public async Task AtivarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        (await BuscarCompletoAsync(id, cancellationToken)).Ativar();
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task InativarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var perfil = await BuscarCompletoAsync(id, cancellationToken);
        if (await perfis.TemVinculosAsync(id, cancellationToken))
            throw new BusinessRuleException("Reatribua os vínculos ativos antes de inativar este perfil.");
        perfil.Inativar();
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task ExcluirAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var perfil = await BuscarCompletoAsync(id, cancellationToken);
        if (await perfis.TemVinculosAsync(id, cancellationToken))
            throw new BusinessRuleException("Reatribua os vínculos ativos antes de excluir este perfil.");
        perfis.Remove(perfil);
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public IReadOnlyCollection<PermissaoResponse> ListarPermissoes()
        => PermissaoCatalogo.Todos.Select(permissao => new PermissaoResponse(permissao.Codigo, permissao.Nome, permissao.Descricao)).ToArray();

    private async Task<Perfil> BuscarCompletoAsync(Guid id, CancellationToken cancellationToken)
        => await perfis.ObterComPermissoesAsync(id, cancellationToken) ?? throw new NotFoundException("Perfil", id);

    private static void ValidarPermissoes(IEnumerable<string> codigos)
    {
        var desconhecidas = codigos.Except(PermissaoCatalogo.Codigos, StringComparer.Ordinal).ToArray();
        if (desconhecidas.Length > 0)
            throw new BusinessRuleException($"Permissão não reconhecida: {desconhecidas[0]}.");
    }

    private static string? NormalizarDescricao(string? descricao)
        => string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
}
