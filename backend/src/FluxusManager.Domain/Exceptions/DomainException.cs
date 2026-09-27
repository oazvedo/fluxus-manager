namespace FluxusManager.Domain.Exceptions;

/// <summary>
/// Erro esperado de negócio. A API converte cada tipo num status HTTP (ver ExceptionFilter);
/// a mensagem é devolvida ao cliente, então deve ser clara e sem dados sensíveis.
/// </summary>
public abstract class DomainException(string message, Exception? innerException = null) : Exception(message, innerException);

/// <summary>Recurso inexistente (ou de outro tenant) → 404.</summary>
public class NotFoundException(string message) : DomainException(message)
{
    public NotFoundException(string recurso, object chave)
        : this($"{recurso} '{chave}' não encontrado(a).")
    {
    }
}

/// <summary>Conflito com o estado atual, ex.: registro duplicado → 409.</summary>
public class ConflictException(string message, Exception? innerException = null) : DomainException(message, innerException);

/// <summary>
/// O banco recusou a gravação por violar um índice único. Acontece quando dois cadastros simultâneos
/// passam juntos pela checagem prévia do service (ex.: mesmo CNPJ); o normal é a checagem responder antes.
/// </summary>
public class DuplicateKeyException(Exception innerException)
    : ConflictException("Este registro já existe. Atualize a página e confira os dados.", innerException);

/// <summary>Operação válida no formato, mas que viola uma regra de negócio → 422.</summary>
public class BusinessRuleException(string message) : DomainException(message);
