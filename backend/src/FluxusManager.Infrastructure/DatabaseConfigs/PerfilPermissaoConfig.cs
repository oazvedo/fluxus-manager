using FluxusManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxusManager.Infrastructure.DatabaseConfigs;

public class PerfilPermissaoConfig : IEntityTypeConfiguration<PerfilPermissao>
{
    public void Configure(EntityTypeBuilder<PerfilPermissao> builder)
    {
        builder.ToTable("perfil_permissoes");
        builder.HasKey(permissao => permissao.Id);
        builder.Property(permissao => permissao.Codigo).HasMaxLength(80).IsRequired();
        builder.HasIndex(permissao => new { permissao.TenantId, permissao.PerfilId, permissao.Codigo })
            .IsUnique().HasFilter("excluido = false");
    }
}
