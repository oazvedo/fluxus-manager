namespace FluxusManager.Domain.Entities;

/// <summary>Histórico de uma solicitação de cadastro: quem fez o quê (a data é o <see cref="BaseEntity.CriadoEm"/>).</summary>
public class SolicitacaoCadastroEvento : BaseEntity
{
    public Guid SolicitacaoId { get; private set; }
    public SolicitacaoCadastroEventoTipo Tipo { get; private set; }

    /// <summary>Administrador da plataforma; nulo nas ações do próprio solicitante.</summary>
    public Guid? UsuarioId { get; private set; }

    public SolicitacaoCadastroEvento(Guid solicitacaoId, SolicitacaoCadastroEventoTipo tipo, Guid? usuarioId)
    {
        SolicitacaoId = solicitacaoId;
        Tipo = tipo;
        UsuarioId = usuarioId;
    }

    // Construtor do EF.
    private SolicitacaoCadastroEvento()
    {
    }
}

public enum SolicitacaoCadastroEventoTipo
{
    Criada,
    VerificacaoReenviada,
    Verificada,
    Visualizada,
    Aprovada,
    Recusada,
    EmailsReenviados
}
