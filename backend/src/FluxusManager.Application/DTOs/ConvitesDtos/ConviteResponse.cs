using FluxusManager.Domain.Entities;

namespace FluxusManager.Application.DTOs.ConvitesDtos;

/// <summary>Status: <c>Pendente</c>, <c>Expirado</c>, <c>Aceito</c> ou <c>Cancelado</c>. O token nunca sai da API.</summary>
public record ConviteResponse(Guid Id, string Email, Guid PerfilId, string Perfil, string Status, DateTime ExpiraEm,
    DateTime? AceitoEm, DateTime? CanceladoEm, DateTime CriadoEm, DateTime? AtualizadoEm)
{
    public static ConviteResponse DeEntidade(Convite convite, DateTime agora)
        => new(convite.Id, convite.Email, convite.PerfilId, convite.Perfil.Nome,
            convite.Expirado(agora) ? "Expirado" : convite.Status.ToString(), convite.ExpiraEm,
            convite.AceitoEm, convite.CanceladoEm, convite.CriadoEm, convite.AtualizadoEm);
}
