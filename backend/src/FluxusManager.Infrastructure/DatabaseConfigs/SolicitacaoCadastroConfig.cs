using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxusManager.Infrastructure.DatabaseConfigs;

public class SolicitacaoCadastroConfig : IEntityTypeConfiguration<SolicitacaoCadastro>
{
    /// <summary>Status que ainda disputam CNPJ/e-mail com um novo pedido.</summary>
    private const string Ativa = "status IN ('AguardandoVerificacao', 'PendenteAnalise') AND excluido = false";

    public void Configure(EntityTypeBuilder<SolicitacaoCadastro> builder)
    {
        builder.ToTable("solicitacoes_cadastro");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.RazaoSocial).HasMaxLength(150).IsRequired();
        builder.Property(s => s.NomeFantasia).HasMaxLength(150);
        builder.Property(s => s.Cnpj).HasMaxLength(Cnpj.Length).IsFixedLength().IsRequired();
        builder.Property(s => s.ResponsavelNome).HasMaxLength(150).IsRequired();
        builder.Property(s => s.ResponsavelEmail).HasMaxLength(254).IsRequired();
        builder.Property(s => s.ResponsavelTelefone).HasMaxLength(20);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        // SHA-256 em hexadecimal; os tokens só existem nos e-mails enviados.
        builder.Property(s => s.VerificacaoTokenHash).HasMaxLength(64);
        builder.HasIndex(s => s.VerificacaoTokenHash).IsUnique();
        builder.Property(s => s.AcompanhamentoTokenHash).HasMaxLength(64);
        builder.HasIndex(s => s.AcompanhamentoTokenHash).IsUnique();
        builder.Property(s => s.ObservacaoInterna).HasMaxLength(1000);
        builder.Property(s => s.MotivoRecusa).HasMaxLength(500);
        // Um pedido ativo por CNPJ e por e-mail: a corrida entre dois envios termina no índice.
        builder.HasIndex(s => s.Cnpj).IsUnique().HasFilter(Ativa);
        builder.HasIndex(s => s.ResponsavelEmail).IsUnique().HasFilter(Ativa);
        builder.HasIndex(s => new { s.Status, s.CriadoEm });
        builder.HasOne<Empresa>().WithMany().HasForeignKey(s => s.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(s => s.DecididaPorId).OnDelete(DeleteBehavior.Restrict);
        // Cascade: descartar um pedido vencido (exclusão lógica) leva junto o histórico e os e-mails pendentes dele.
        builder.HasMany(s => s.Eventos).WithOne().HasForeignKey(e => e.SolicitacaoId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(s => s.Emails).WithOne().HasForeignKey(e => e.SolicitacaoId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SolicitacaoCadastroEventoConfig : IEntityTypeConfiguration<SolicitacaoCadastroEvento>
{
    public void Configure(EntityTypeBuilder<SolicitacaoCadastroEvento> builder)
    {
        builder.ToTable("solicitacoes_cadastro_eventos");
        builder.HasKey(e => e.Id);
        // Criado pela solicitação já rastreada: sem isso o EF trataria o id preenchido como registro existente.
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(e => new { e.SolicitacaoId, e.CriadoEm });
        builder.HasOne<Usuario>().WithMany().HasForeignKey(e => e.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SolicitacaoCadastroEmailConfig : IEntityTypeConfiguration<SolicitacaoCadastroEmail>
{
    public void Configure(EntityTypeBuilder<SolicitacaoCadastroEmail> builder)
    {
        builder.ToTable("solicitacoes_cadastro_emails");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(e => new { e.SolicitacaoId, e.Tipo }).IsUnique().HasFilter("excluido = false");
        builder.HasIndex(e => new { e.Status, e.ProximaTentativaEm });
        // Dois processos que tentam o mesmo envio: o segundo a marcar "Enviando" falha no xmin e desiste.
        builder.Property<uint>("Versao").HasColumnName("xmin").HasColumnType("xid").IsRowVersion();
        builder.HasOne<Convite>().WithMany().HasForeignKey(e => e.ConviteId).OnDelete(DeleteBehavior.Restrict);
    }
}
