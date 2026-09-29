namespace FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;

public record SolicitacaoEventoResponse(string Evento, DateTime Em, UsuarioReferenciaResponse? Por);
