using FluxusManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxusManager.Infrastructure.DatabaseConfigs;

public class UsuarioConfig : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nome).HasMaxLength(150).IsRequired();

        builder.Property(u => u.Email).HasMaxLength(254).IsRequired();
        // Único só entre os não excluídos: o e-mail de um usuário excluído pode ser reaproveitado.
        builder.HasIndex(u => u.Email).IsUnique().HasFilter("excluido = false");

        builder.Property(u => u.SenhaHash).HasMaxLength(500).IsRequired();

        builder.Property(u => u.Ativo).IsRequired();
    }
}
