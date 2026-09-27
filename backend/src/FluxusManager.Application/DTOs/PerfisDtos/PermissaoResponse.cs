using FluxusManager.Application.Security;

namespace FluxusManager.Application.DTOs.PerfisDtos;

public record PermissaoResponse(string Codigo, string Nome, string Descricao)
{
    public static PermissaoResponse DeCatalogo(PermissaoCatalogoItem permissao)
        => new(permissao.Codigo, permissao.Nome, permissao.Descricao);
}
