using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxusManager.Infrastructure.DatabaseConfigs;

public class EmpresaConfig : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("empresas");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RazaoSocial).HasMaxLength(150).IsRequired();

        builder.Property(e => e.NomeFantasia).HasMaxLength(150);

        // Sempre normalizado (sem pontuação, maiúsculo). Único só entre as não excluídas.
        builder.Property(e => e.Cnpj).HasMaxLength(Cnpj.Length).IsFixedLength().IsRequired();
        builder.HasIndex(e => e.Cnpj).IsUnique().HasFilter("excluido = false");

        builder.Property(e => e.Ativo).IsRequired();
    }
}
