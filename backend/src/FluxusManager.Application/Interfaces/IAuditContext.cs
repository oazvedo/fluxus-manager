namespace FluxusManager.Application.Interfaces;

/// <summary>
/// Quem, em qual empresa e a partir de qual tela a requisição atual altera dados.
/// O UnitOfWork repassa esses valores ao PostgreSQL, e o trigger de auditoria grava em <c>audit.change_log</c>.
/// </summary>
public interface IAuditContext
{
    /// <summary>Header com a tela do frontend que originou a requisição.</summary>
    const string FrontendUrlHeader = "X-Frontend-Url";

    /// <summary>Tamanho máximo gravado de <see cref="FrontendUrl"/>; o excedente é descartado.</summary>
    const int FrontendUrlMaxLength = 2048;

    string? User { get; }

    Guid? TenantId { get; }

    string? FrontendUrl { get; }

    void SetUser(string user);

    void SetFrontendUrl(string frontendUrl);
}
