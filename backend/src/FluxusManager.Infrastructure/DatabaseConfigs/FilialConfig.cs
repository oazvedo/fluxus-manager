using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxusManager.Infrastructure.DatabaseConfigs;

public class FilialConfig : IEntityTypeConfiguration<Filial>
{
    public void Configure(EntityTypeBuilder<Filial> builder)
    {
        builder.ToTable("filiais");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Nome).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Cnpj).HasMaxLength(Cnpj.Length).IsFixedLength().IsRequired();
        builder.HasIndex(e => e.Cnpj).IsUnique().HasFilter("excluido = false");
        builder.Property(e => e.Endereco).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Ativo).IsRequired();
        builder.Property(e => e.TenantId).IsRequired();
        builder.HasOne<Empresa>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
