namespace FluxusManager.Application.DTOs.PerfisDtos;

public record AtualizarPerfilRequest(string Nome, string? Descricao, IReadOnlyCollection<string> Permissoes) : IPerfilRequest;
