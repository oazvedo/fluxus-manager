namespace FluxusManager.Application.DTOs.PerfisDtos;

public interface IPerfilRequest
{
    string Nome { get; }
    string? Descricao { get; }
    IReadOnlyCollection<string> Permissoes { get; }
}

public record CriarPerfilRequest(string Nome, string? Descricao, IReadOnlyCollection<string> Permissoes) : IPerfilRequest;
