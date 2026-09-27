using FluxusManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxusManager.Infrastructure.DatabaseConfigs;

public class PerfilConfig : IEntityTypeConfiguration<Perfil>
{
    public void Configure(EntityTypeBuilder<Perfil> builder)
    {
        builder.ToTable("perfis");
        builder.HasKey(perfil => perfil.Id);
        // Nome fixo: sem ele, a convenção passa a derivar outro nome quando surgem novas FKs para esta chave.
        builder.HasAlternateKey(perfil => new { perfil.Id, perfil.TenantId }).HasName("ak_perfis_id_tenant_id");
        builder.Property(perfil => perfil.Nome).HasMaxLength(100).IsRequired();
        builder.Property(perfil => perfil.Descricao).HasMaxLength(250);
        builder.Property(perfil => perfil.Ativo).IsRequired();
        builder.Property(perfil => perfil.NomeNormalizado).HasMaxLength(100).IsRequired();
        builder.HasIndex(perfil => new { perfil.TenantId, perfil.NomeNormalizado })
            .IsUnique().HasFilter("excluido = false");
        builder.HasOne<Empresa>().WithMany(empresa => empresa.Perfis)
            .HasForeignKey(perfil => perfil.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(perfil => perfil.Permissoes).WithOne(permissao => permissao.Perfil)
            .HasForeignKey(permissao => new { permissao.PerfilId, permissao.TenantId })
            .HasPrincipalKey(perfil => new { perfil.Id, perfil.TenantId }).OnDelete(DeleteBehavior.Cascade);
    }
}
