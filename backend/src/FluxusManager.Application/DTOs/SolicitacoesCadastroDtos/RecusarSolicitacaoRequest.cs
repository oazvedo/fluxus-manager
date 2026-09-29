namespace FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;

/// <summary>O motivo é enviado ao solicitante; a observação interna, não.</summary>
public record RecusarSolicitacaoRequest(string Motivo, string? ObservacaoInterna);
