namespace FluxusManager.Domain.Entities;

/// <summary>Unidade operacional pertencente a uma empresa (tenant).</summary>
public class Filial : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Nome { get; private set; }
    public string Cnpj { get; private set; }
    public string Endereco { get; private set; }
    public bool Ativo { get; private set; } = true;

    public Filial(string nome, string cnpj, string endereco)
    {
        Nome = nome;
        Cnpj = cnpj;
        Endereco = endereco;
    }

    public void Atualizar(string nome, string endereco)
    {
        Nome = nome;
        Endereco = endereco;
    }

    public void Ativar() => Ativo = true;
    public void Inativar() => Ativo = false;
}
