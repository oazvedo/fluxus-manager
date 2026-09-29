using FluxusManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxusManager.Infrastructure.DatabaseConfigs;

public class AdministradorPlataformaConfig : IEntityTypeConfiguration<AdministradorPlataforma>
{
    public void Configure(EntityTypeBuilder<AdministradorPlataforma> builder)
    {
        builder.ToTable("administradores_plataforma");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.UsuarioId).IsUnique().HasFilter("excluido = false");
        builder.HasOne<Usuario>().WithMany().HasForeignKey(a => a.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
