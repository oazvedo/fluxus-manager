using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FluxusManager.API.Filters;

/// <summary>
/// Executa os validators do FluentValidation de cada argumento da action antes do controller.
/// Havendo erros, responde 400 com <see cref="ValidationProblemDetails"/> e a action não é executada.
/// </summary>
public class ValidationFilter(IServiceProvider services, ProblemDetailsFactory problemDetailsFactory) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new ModelStateDictionary();

        foreach (var argument in context.ActionArguments.Values.OfType<object>())
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator)
                continue;

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument), context.HttpContext.RequestAborted);

            foreach (var failure in result.Errors)
                errors.AddModelError(ToJsonPath(failure.PropertyName), failure.ErrorMessage);
        }

        if (errors.IsValid)
        {
            await next();
            return;
        }

        var problem = problemDetailsFactory.CreateValidationProblemDetails(
            context.HttpContext, errors, StatusCodes.Status400BadRequest,
            title: "Dados inválidos", instance: context.HttpContext.Request.Path);

        context.Result = new BadRequestObjectResult(problem);
    }

    // "Endereco.Cep" → "endereco.cep", no mesmo formato do JSON da API.
    private static string ToJsonPath(string propertyName)
        => string.Join('.', propertyName.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
}
