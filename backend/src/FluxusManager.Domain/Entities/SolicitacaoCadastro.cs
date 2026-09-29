namespace FluxusManager.Domain.Entities;

/// <summary>
/// Pedido público de uma empresa para entrar no Fluxus. Não é de um tenant: a empresa só existe depois da aprovação.
/// Guarda apenas hashes dos tokens enviados por e-mail. Transições: AguardandoVerificacao → PendenteAnalise →
/// Aprovada ou Recusada; nada volta. "Expirado" não é status: é o token de verificação vencido.
/// </summary>
public class SolicitacaoCadastro : BaseEntity
{
    public static readonly TimeSpan IntervaloMinimoReenvio = TimeSpan.FromMinutes(2);
    public const int MaxReenviosVerificacao = 5;

    public string RazaoSocial { get; private set; }
    public string? NomeFantasia { get; private set; }
    public string Cnpj { get; private set; }
    public string ResponsavelNome { get; private set; }
    public string ResponsavelEmail { get; private set; }
    public string? ResponsavelTelefone { get; private set; }
    public SolicitacaoCadastroStatus Status { get; private set; } = SolicitacaoCadastroStatus.AguardandoVerificacao;

    /// <summary>Hash do link de verificação; mantido depois de vencer (para distinguir expirado de inválido) e limpo ao usar.</summary>
    public string? VerificacaoTokenHash { get; private set; }
    public DateTime? VerificacaoExpiraEm { get; private set; }

    /// <summary>Hash do link de acompanhamento (só leitura). Trocado a cada e-mail que o envia.</summary>
    public string? AcompanhamentoTokenHash { get; private set; }
    public DateTime? AcompanhamentoExpiraEm { get; private set; }

    public DateTime? VerificadaEm { get; private set; }
    public DateTime? DecididaEm { get; private set; }
    public Guid? DecididaPorId { get; private set; }

    /// <summary>Anotação do administrador da plataforma; nunca vai para o solicitante.</summary>
    public string? ObservacaoInterna { get; private set; }

    /// <summary>Justificativa da recusa, comunicada ao solicitante.</summary>
    public string? MotivoRecusa { get; private set; }

    /// <summary>Empresa criada na aprovação.</summary>
    public Guid? EmpresaId { get; private set; }

    public List<SolicitacaoCadastroEvento> Eventos { get; private set; } = [];
    public List<SolicitacaoCadastroEmail> Emails { get; private set; } = [];

    public SolicitacaoCadastro(string razaoSocial, string? nomeFantasia, string cnpj,
        string responsavelNome, string responsavelEmail, string? responsavelTelefone, DateTime agora)
    {
        RazaoSocial = razaoSocial;
        NomeFantasia = nomeFantasia;
        Cnpj = cnpj;
        ResponsavelNome = responsavelNome;
        ResponsavelEmail = responsavelEmail;
        ResponsavelTelefone = responsavelTelefone;
        Registrar(SolicitacaoCadastroEventoTipo.Criada, null);
        AgendarEmail(SolicitacaoCadastroEmailTipo.Verificacao, agora);
    }

    // Construtor do EF.
    private SolicitacaoCadastro()
    {
        RazaoSocial = null!;
        Cnpj = null!;
        ResponsavelNome = null!;
        ResponsavelEmail = null!;
    }

    /// <summary>Ainda disputa o CNPJ/e-mail com um novo pedido.</summary>
    public bool Ativa => Status is SolicitacaoCadastroStatus.AguardandoVerificacao or SolicitacaoCadastroStatus.PendenteAnalise;

    public bool VerificacaoExpirada(DateTime agora)
        => Status == SolicitacaoCadastroStatus.AguardandoVerificacao && VerificacaoExpiraEm is { } expira && expira <= agora;

    /// <summary>
    /// Sem confirmação dentro do prazo, contando do pedido quando nenhum link chegou a sair (e-mail que nunca é
    /// entregue): esse pedido não pode segurar o CNPJ e o e-mail para sempre.
    /// </summary>
    public bool VerificacaoVencida(DateTime agora, TimeSpan validade)
        => VerificacaoExpirada(agora)
            || Status == SolicitacaoCadastroStatus.AguardandoVerificacao && VerificacaoExpiraEm is null && CriadoEm <= agora - validade;

    /// <summary>Chamado no envio do e-mail de verificação: o link anterior deixa de valer.</summary>
    public void RenovarVerificacao(string tokenHash, DateTime expiraEm)
    {
        GarantirStatus(SolicitacaoCadastroStatus.AguardandoVerificacao);
        VerificacaoTokenHash = tokenHash;
        VerificacaoExpiraEm = expiraEm;
    }

    /// <summary>Chamado no envio de um e-mail com o link de acompanhamento: o link anterior deixa de valer.</summary>
    public void RenovarAcompanhamento(string tokenHash, DateTime expiraEm)
    {
        AcompanhamentoTokenHash = tokenHash;
        AcompanhamentoExpiraEm = expiraEm;
    }

    /// <summary>
    /// Pede um novo envio do link de verificação (o token é gerado no envio). Como qualquer um pode pedir pela rota
    /// pública, há um intervalo mínimo e um limite de reenvios: fora deles o pedido é ignorado (devolve false) e a
    /// resposta pública continua a mesma, para ninguém usar o Fluxus para inundar a caixa de outra pessoa.
    /// </summary>
    public bool ReenviarVerificacao(DateTime agora)
    {
        GarantirStatus(SolicitacaoCadastroStatus.AguardandoVerificacao);
        var email = Emails.FirstOrDefault(e => e.Tipo == SolicitacaoCadastroEmailTipo.Verificacao);
        if (email is { Status: SolicitacaoCadastroEmailStatus.Pendente or SolicitacaoCadastroEmailStatus.Enviando }
            || email?.UltimaTentativaEm > agora - IntervaloMinimoReenvio
            || Eventos.Count(e => e.Tipo == SolicitacaoCadastroEventoTipo.VerificacaoReenviada) >= MaxReenviosVerificacao)
            return false;

        Registrar(SolicitacaoCadastroEventoTipo.VerificacaoReenviada, null);
        AgendarEmail(SolicitacaoCadastroEmailTipo.Verificacao, agora);
        return true;
    }

    public void Verificar(DateTime agora)
    {
        if (VerificacaoTokenHash is null || VerificacaoExpirada(agora))
            throw new InvalidOperationException("Só uma verificação pendente e dentro do prazo pode ser confirmada.");
        GarantirStatus(SolicitacaoCadastroStatus.AguardandoVerificacao);
        Status = SolicitacaoCadastroStatus.PendenteAnalise;
        VerificadaEm = agora;
        VerificacaoTokenHash = null;
        VerificacaoExpiraEm = null;
        Registrar(SolicitacaoCadastroEventoTipo.Verificada, null);
        AgendarEmail(SolicitacaoCadastroEmailTipo.Acompanhamento, agora);
    }

    public void Aprovar(Guid empresaId, Guid conviteId, Guid administradorId, string? observacaoInterna, DateTime agora)
    {
        GarantirStatus(SolicitacaoCadastroStatus.PendenteAnalise);
        Status = SolicitacaoCadastroStatus.Aprovada;
        EmpresaId = empresaId;
        Decidir(administradorId, observacaoInterna, agora);
        Registrar(SolicitacaoCadastroEventoTipo.Aprovada, administradorId);
        AgendarEmail(SolicitacaoCadastroEmailTipo.Aprovacao, agora).ConviteId = conviteId;
    }

    public void Recusar(string motivo, Guid administradorId, string? observacaoInterna, DateTime agora)
    {
        GarantirStatus(SolicitacaoCadastroStatus.PendenteAnalise);
        Status = SolicitacaoCadastroStatus.Recusada;
        MotivoRecusa = motivo;
        Decidir(administradorId, observacaoInterna, agora);
        Registrar(SolicitacaoCadastroEventoTipo.Recusada, administradorId);
        AgendarEmail(SolicitacaoCadastroEmailTipo.Recusa, agora);
    }

    /// <summary>Leitura do detalhe pelo administrador: fica no histórico (e na auditoria) como qualquer decisão.</summary>
    public void RegistrarVisualizacao(Guid administradorId) => Registrar(SolicitacaoCadastroEventoTipo.Visualizada, administradorId);

    /// <summary>Reagenda para já os e-mails ainda não entregues. Devolve quantos foram reagendados.</summary>
    public int ReenviarEmails(Guid administradorId, DateTime agora)
    {
        var naoEntregues = Emails.Where(e => e.Status != SolicitacaoCadastroEmailStatus.Enviado).ToList();
        foreach (var email in naoEntregues)
            email.Reagendar(agora);
        if (naoEntregues.Count > 0)
            Registrar(SolicitacaoCadastroEventoTipo.EmailsReenviados, administradorId);
        return naoEntregues.Count;
    }

    private void Decidir(Guid administradorId, string? observacaoInterna, DateTime agora)
    {
        DecididaEm = agora;
        DecididaPorId = administradorId;
        ObservacaoInterna = observacaoInterna;
    }

    /// <summary>Um registro por tipo de e-mail: pedir de novo reagenda o mesmo, sem duplicar entregas.</summary>
    private SolicitacaoCadastroEmail AgendarEmail(SolicitacaoCadastroEmailTipo tipo, DateTime agora)
    {
        var email = Emails.FirstOrDefault(e => e.Tipo == tipo);
        if (email is null)
        {
            email = new SolicitacaoCadastroEmail(Id, tipo, agora);
            Emails.Add(email);
        }
        else
            email.Reagendar(agora);
        return email;
    }

    private void Registrar(SolicitacaoCadastroEventoTipo tipo, Guid? usuarioId)
        => Eventos.Add(new SolicitacaoCadastroEvento(Id, tipo, usuarioId));

    private void GarantirStatus(SolicitacaoCadastroStatus esperado)
    {
        if (Status != esperado)
            throw new InvalidOperationException($"Operação exige solicitação {esperado}, mas ela está {Status}.");
    }
}

public enum SolicitacaoCadastroStatus
{
    AguardandoVerificacao,
    PendenteAnalise,
    Aprovada,
    Recusada
}
