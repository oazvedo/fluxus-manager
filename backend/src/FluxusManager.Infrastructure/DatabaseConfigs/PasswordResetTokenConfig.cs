using FluxusManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxusManager.Infrastructure.DatabaseConfigs;

public class PasswordResetTokenConfig : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(e => e.TokenHash).IsUnique();
        builder.HasIndex(e => new { e.UsuarioId, e.ExpiraEm });
        builder.HasOne<Usuario>().WithMany().HasForeignKey(e => e.UsuarioId).OnDelete(DeleteBehavior.Cascade);
    }
}
