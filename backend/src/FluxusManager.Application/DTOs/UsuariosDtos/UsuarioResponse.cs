using FluxusManager.Domain.Entities;

namespace FluxusManager.Application.DTOs.UsuariosDtos;

/// <summary>Dados do usuário devolvidos pela API. Nunca inclui a senha.</summary>
public record UsuarioResponse(
    Guid Id,
    string Nome,
    string Email,
    bool Ativo,
    DateTime CriadoEm,
    DateTime? AtualizadoEm)
{
    public static UsuarioResponse DeEntidade(Usuario usuario) => new(
        usuario.Id,
        usuario.Nome,
        usuario.Email,
        usuario.Ativo,
        usuario.CriadoEm,
        usuario.AtualizadoEm);
}
