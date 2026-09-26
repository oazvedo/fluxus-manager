using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace FluxusManager.API.Filters;

/// <summary>
/// Resposta 400 da validação automática do ASP.NET (ex.: [Range] em query string, JSON mal formado),
/// no mesmo formato do <see cref="ValidationFilter"/>: título "Dados inválidos" e erros por campo.
/// </summary>
public static class InvalidModelStateResponse
{
    public static IActionResult Create(ActionContext context)
    {
        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

        var problem = factory.CreateValidationProblemDetails(
            context.HttpContext, context.ModelState, StatusCodes.Status400BadRequest,
            title: "Dados inválidos", instance: context.HttpContext.Request.Path);

        return new BadRequestObjectResult(problem);
    }
}
