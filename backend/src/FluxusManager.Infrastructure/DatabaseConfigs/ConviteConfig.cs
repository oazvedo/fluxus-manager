using FluxusManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxusManager.Infrastructure.DatabaseConfigs;

public class ConviteConfig : IEntityTypeConfiguration<Convite>
{
    public void Configure(EntityTypeBuilder<Convite> builder)
    {
        builder.ToTable("convites");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Email).HasMaxLength(254).IsRequired();
        // SHA-256 em hexadecimal; o token em si só existe no e-mail enviado.
        builder.Property(c => c.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(c => c.TokenHash).IsUnique();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        // Aceite, cancelamento e reenvio leem e gravam o status: o xmin do PostgreSQL faz o segundo a gravar falhar (409).
        builder.Property<uint>("Versao").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
        // No máximo um convite pendente por e-mail em cada empresa.
        builder.HasIndex(c => new { c.TenantId, c.Email }).IsUnique().HasFilter("status = 'Pendente' AND excluido = false");
        // Listagem da empresa, ordenada por criação.
        builder.HasIndex(c => new { c.TenantId, c.CriadoEm });
        builder.HasOne<Empresa>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(c => c.Perfil).WithMany()
            .HasForeignKey(c => new { c.PerfilId, c.TenantId })
            .HasPrincipalKey(perfil => new { perfil.Id, perfil.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(c => c.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
