using FluxusManager.API.Logging;
using FluxusManager.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace FluxusManager.API.Filters;

/// <summary>
/// Converte exceções dos controllers em respostas <see cref="ProblemDetails"/> (RFC 9457).
/// Erros de negócio devolvem a mensagem; erros inesperados viram 500 sem detalhes internos fora de Development.
/// </summary>
public class ExceptionFilter(
    ProblemDetailsFactory problemDetailsFactory,
    IHostEnvironment environment,
    ILogger<ExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var (status, title, detail) = context.Exception switch
        {
            NotFoundException e => (StatusCodes.Status404NotFound, "Recurso não encontrado", e.Message),
            ConflictException e => (StatusCodes.Status409Conflict, "Conflito", e.Message),
            BusinessRuleException e => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada", e.Message),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno", UnexpectedErrorDetail(context)),
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(context.Exception, "Erro não tratado em {Path}", context.HttpContext.Request.Path);

        var problem = problemDetailsFactory.CreateProblemDetails(
            context.HttpContext, status, title, detail: detail, instance: context.HttpContext.Request.Path);

        context.Result = new ObjectResult(problem) { StatusCode = status };
        context.ExceptionHandled = true;
    }

    private string UnexpectedErrorDetail(ExceptionContext context)
        => environment.IsDevelopment()
            ? context.Exception.Message
            : "Não foi possível concluir a operação. Tente novamente; se o erro continuar, " +
              $"informe ao suporte o código {CorrelationIdMiddleware.Get(context.HttpContext)}.";
}
