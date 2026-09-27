using FluxusManager.Domain.Entities;

namespace FluxusManager.Application.DTOs.UsuariosDtos;

/// <summary>
/// Dados do usuário devolvidos pela API. Nunca inclui a senha. <c>BloqueadoAte</c> vem preenchido enquanto o login
/// estiver bloqueado por senhas erradas (desbloqueio em <c>PATCH /usuarios/{id}/desbloquear</c>).
/// </summary>
public record UsuarioResponse(
    Guid Id,
    string Nome,
    string Email,
    bool Ativo,
    DateTime CriadoEm,
    DateTime? AtualizadoEm,
    DateTime? BloqueadoAte = null)
{
    public static UsuarioResponse DeEntidade(Usuario usuario) => new(
        usuario.Id,
        usuario.Nome,
        usuario.Email,
        usuario.Ativo,
        usuario.CriadoEm,
        usuario.AtualizadoEm,
        usuario.BloqueadoAte > DateTime.UtcNow ? usuario.BloqueadoAte : null);
}
