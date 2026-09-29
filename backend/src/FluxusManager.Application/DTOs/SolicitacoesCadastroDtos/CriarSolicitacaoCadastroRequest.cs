namespace FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;

/// <summary>CNPJ com ou sem pontuação. Nada é criado antes da verificação do e-mail e da aprovação.</summary>
public record CriarSolicitacaoCadastroRequest(
    string RazaoSocial,
    string? NomeFantasia,
    string Cnpj,
    string ResponsavelNome,
    string ResponsavelEmail,
    string? ResponsavelTelefone);
