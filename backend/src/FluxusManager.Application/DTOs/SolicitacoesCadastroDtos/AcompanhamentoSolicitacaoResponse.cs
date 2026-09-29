namespace FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;

/// <summary>O que o solicitante vê pelo link de acompanhamento: sem CNPJ, dados do responsável ou anotações internas.</summary>
public record AcompanhamentoSolicitacaoResponse(
    string Status,
    string RazaoSocial,
    DateTime CriadaEm,
    DateTime? VerificadaEm,
    DateTime? DecididaEm,
    string? MotivoRecusa);
