namespace FluxusManager.Application.Options;

/// <summary>Prazos dos links da solicitação pública de cadastro e o envio automático dos e-mails.</summary>
public sealed class SolicitacaoCadastroOptions
{
    public const string SectionName = "SolicitacoesCadastro";

    /// <summary>Horas até o link de verificação do e-mail vencer, contadas do último envio.</summary>
    public int VerificacaoValidadeHoras { get; set; } = 24;

    /// <summary>Dias até o link de acompanhamento vencer, contados do último e-mail que o enviou.</summary>
    public int AcompanhamentoValidadeDias { get; set; } = 30;

    /// <summary>Rotina em segundo plano que envia e reenvia os e-mails pendentes. Os testes a desligam.</summary>
    public bool EnvioAutomatico { get; set; } = true;
}
