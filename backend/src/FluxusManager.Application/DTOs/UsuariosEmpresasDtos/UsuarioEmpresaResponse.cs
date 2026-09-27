using FluxusManager.Domain.Entities;

namespace FluxusManager.Application.DTOs.UsuariosEmpresasDtos;

public record UsuarioEmpresaResponse(Guid Id, Guid UsuarioId, Guid EmpresaId, string Perfil, bool Ativo, DateTime CriadoEm, DateTime? AtualizadoEm)
{
    public static UsuarioEmpresaResponse DeEntidade(UsuarioEmpresa vinculo)
        => new(vinculo.Id, vinculo.UsuarioId, vinculo.EmpresaId, vinculo.Perfil, vinculo.Ativo, vinculo.CriadoEm, vinculo.AtualizadoEm);
}
