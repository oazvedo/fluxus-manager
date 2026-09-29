using FluxusManager.Domain.Entities;

namespace FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;

public record SolicitacaoDetalheResponse(
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
    bool EmailPendente,
    string? ResponsavelTelefone,
    UsuarioReferenciaResponse? DecididaPor,
    string? ObservacaoInterna,
    string? MotivoRecusa,
    Guid? EmpresaId,
    IReadOnlyList<SolicitacaoEventoResponse> Historico,
    IReadOnlyList<SolicitacaoEmailResponse> Emails)
{
    /// <param name="usuarios">Nomes dos administradores citados na decisão e no histórico.</param>
    public static SolicitacaoDetalheResponse DeEntidade(SolicitacaoCadastro s, IReadOnlyDictionary<Guid, string> usuarios)
    {
        var resumo = SolicitacaoResumoResponse.DeEntidade(s);
        UsuarioReferenciaResponse? Usuario(Guid? id)
            => id is { } valor ? new UsuarioReferenciaResponse(valor, usuarios.GetValueOrDefault(valor, "Usuário removido")) : null;

        return new SolicitacaoDetalheResponse(
            resumo.Id, resumo.RazaoSocial, resumo.NomeFantasia, resumo.Cnpj, resumo.ResponsavelNome, resumo.ResponsavelEmail,
            resumo.Status, resumo.CriadaEm, resumo.VerificadaEm, resumo.DecididaEm, resumo.EmailPendente,
            s.ResponsavelTelefone, Usuario(s.DecididaPorId), s.ObservacaoInterna, s.MotivoRecusa, s.EmpresaId,
            s.Eventos.OrderBy(e => e.CriadoEm).ThenBy(e => e.Id)
                .Select(e => new SolicitacaoEventoResponse(e.Tipo.ToString(), e.CriadoEm, Usuario(e.UsuarioId))).ToList(),
            s.Emails.OrderBy(e => e.CriadoEm).ThenBy(e => e.Id)
                .Select(e => new SolicitacaoEmailResponse(e.Tipo.ToString(),
                    e.Status == SolicitacaoCadastroEmailStatus.Enviando ? nameof(SolicitacaoCadastroEmailStatus.Pendente) : e.Status.ToString(),
                    e.Tentativas, e.UltimaTentativaEm)).ToList());
    }
}
