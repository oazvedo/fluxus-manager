namespace FluxusManager.Domain.Entities;

/// <summary>
/// Entrega pendente (outbox) de um e-mail da solicitação. Guarda só o estado da entrega: o conteúdo e o token do link
/// são gerados no momento do envio, então nenhum token fica gravado em texto. Um registro por tipo.
/// </summary>
public class SolicitacaoCadastroEmail : BaseEntity
{
    /// <summary>Envios automáticos antes de desistir; depois disso só o reenvio manual do administrador.</summary>
    public const int MaxTentativasAutomaticas = 8;

    /// <summary>Um envio "Enviando" há mais tempo que isso é considerado interrompido e volta a ser tentado.</summary>
    public static readonly TimeSpan EnvioInterrompidoApos = TimeSpan.FromMinutes(5);

    public Guid SolicitacaoId { get; private set; }
    public SolicitacaoCadastroEmailTipo Tipo { get; private set; }
    public SolicitacaoCadastroEmailStatus Status { get; private set; } = SolicitacaoCadastroEmailStatus.Pendente;
    public int Tentativas { get; private set; }
    public DateTime? UltimaTentativaEm { get; private set; }
    public DateTime ProximaTentativaEm { get; private set; }

    /// <summary>Convite do responsável (só no e-mail de aprovação).</summary>
    public Guid? ConviteId { get; internal set; }

    public SolicitacaoCadastroEmail(Guid solicitacaoId, SolicitacaoCadastroEmailTipo tipo, DateTime agora)
    {
        SolicitacaoId = solicitacaoId;
        Tipo = tipo;
        ProximaTentativaEm = agora;
    }

    // Construtor do EF.
    private SolicitacaoCadastroEmail()
    {
    }

    /// <summary>Marca o envio em andamento, para outro processo não enviar o mesmo e-mail em paralelo.</summary>
    public bool IniciarEnvio(DateTime agora)
    {
        if (Status == SolicitacaoCadastroEmailStatus.Enviado
            || Status == SolicitacaoCadastroEmailStatus.Enviando && UltimaTentativaEm > agora - EnvioInterrompidoApos)
            return false;

        Status = SolicitacaoCadastroEmailStatus.Enviando;
        Tentativas++;
        UltimaTentativaEm = agora;
        return true;
    }

    public void MarcarEnviado() => Status = SolicitacaoCadastroEmailStatus.Enviado;

    /// <summary>Espera dobra a cada falha (1, 2, 4... minutos, até 1 hora).</summary>
    public void MarcarFalha(DateTime agora)
    {
        Status = SolicitacaoCadastroEmailStatus.Falhou;
        ProximaTentativaEm = agora.AddMinutes(Math.Min(60, Math.Pow(2, Math.Max(0, Tentativas - 1))));
    }

    /// <summary>Volta para a fila já, inclusive depois de esgotar as tentativas automáticas.</summary>
    public void Reagendar(DateTime agora)
    {
        Status = SolicitacaoCadastroEmailStatus.Pendente;
        Tentativas = 0;
        ProximaTentativaEm = agora;
    }
}

public enum SolicitacaoCadastroEmailTipo
{
    Verificacao,
    Acompanhamento,
    Aprovacao,
    Recusa
}

public enum SolicitacaoCadastroEmailStatus
{
    Pendente,
    Enviando,
    Enviado,
    Falhou
}
