using FluxusManager.Domain.Entities;

namespace FluxusManager.Application.DTOs.PerfisDtos;

public record PerfilResponse(Guid Id, Guid EmpresaId, string Nome, string? Descricao, bool Ativo,
    IReadOnlyCollection<string> Permissoes, DateTime CriadoEm, DateTime? AtualizadoEm)
{
    public static PerfilResponse DeEntidade(Perfil perfil)
        => new(perfil.Id, perfil.TenantId, perfil.Nome, perfil.Descricao, perfil.Ativo,
            perfil.Permissoes.Select(permissao => permissao.Codigo).Order(StringComparer.Ordinal).ToArray(),
            perfil.CriadoEm, perfil.AtualizadoEm);
}
