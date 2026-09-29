using FluxusManager.Domain.Entities;

namespace FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;

public record SolicitacaoResumoResponse(
    Guid Id,
    string RazaoSocial,
    string? NomeFantasia,
    string Cnpj,
    string ResponsavelNome,
    string ResponsavelEmail,
    string Status,
    DateTime CriadaEm,
    DateTime? VerificadaEm,
    DateTime? DecididaEm,
    bool EmailPendente)
{
    public static SolicitacaoResumoResponse DeEntidade(SolicitacaoCadastro s) => new(
        s.Id, s.RazaoSocial, s.NomeFantasia, s.Cnpj, s.ResponsavelNome, s.ResponsavelEmail, s.Status.ToString(),
        s.CriadoEm, s.VerificadaEm, s.DecididaEm, s.Emails.Any(e => e.Status != SolicitacaoCadastroEmailStatus.Enviado));
}
