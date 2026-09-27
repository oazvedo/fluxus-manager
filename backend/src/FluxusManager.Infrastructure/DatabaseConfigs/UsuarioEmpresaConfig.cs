using FluxusManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxusManager.Infrastructure.DatabaseConfigs;

public class UsuarioEmpresaConfig : IEntityTypeConfiguration<UsuarioEmpresa>
{
    public void Configure(EntityTypeBuilder<UsuarioEmpresa> builder)
    {
        builder.ToTable("usuarios_empresas");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Perfil).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Ativo).IsRequired();
        builder.HasIndex(e => new { e.UsuarioId, e.EmpresaId }).IsUnique().HasFilter("excluido = false");
        builder.HasOne<Usuario>().WithMany().HasForeignKey(e => e.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Empresa>().WithMany().HasForeignKey(e => e.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}
