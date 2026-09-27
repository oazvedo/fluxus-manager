namespace FluxusManager.Domain.Entities;

/// <summary>
/// Empresa cliente do sistema; é o tenant dos dados multi-tenant. O CNPJ não muda depois do cadastro:
/// outro CNPJ é outra pessoa jurídica, portanto outra empresa.
/// </summary>
public class Empresa : BaseEntity
{
    public string RazaoSocial { get; private set; }
    public string? NomeFantasia { get; private set; }
    public string Cnpj { get; private set; }
    public bool Ativo { get; private set; } = true;

    public Empresa(string razaoSocial, string? nomeFantasia, string cnpj)
    {
        RazaoSocial = razaoSocial;
        NomeFantasia = nomeFantasia;
        Cnpj = cnpj;
    }

    public void Atualizar(string razaoSocial, string? nomeFantasia)
    {
        RazaoSocial = razaoSocial;
        NomeFantasia = nomeFantasia;
    }

    public void Ativar() => Ativo = true;

    public void Inativar() => Ativo = false;
}
