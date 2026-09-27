using System.Globalization;
using FluentValidation;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluxusManager.Application;

/// <summary>
/// Registro de todas as dependências da camada Application.
/// Toda classe/interface nova desta camada (services, etc.) deve ser registrada aqui.
/// </summary>
public static class ApplicationModule
{
    public static IServiceCollection AddApplicationModule(this IServiceCollection services)
    {
        AddServices(services);
        AddValidators(services);

        return services;
    }

    private static void AddServices(IServiceCollection services)
    {
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IAuditContext, AuditContext>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IEmpresaService, EmpresaService>();
        services.AddScoped<IUsuarioEmpresaService, UsuarioEmpresaService>();
    }

    private static void AddValidators(IServiceCollection services)
    {
        // Todo AbstractValidator<T> desta camada é registrado automaticamente e executado pelo ValidationFilter da API.
        services.AddValidatorsFromAssembly(typeof(ApplicationModule).Assembly);

        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("pt-BR");
    }
}
