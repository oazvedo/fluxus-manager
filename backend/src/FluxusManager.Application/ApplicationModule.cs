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

        return services;
    }

    private static void AddServices(IServiceCollection services)
    {
        // services.AddScoped<IEmpresaService, EmpresaService>();
    }
}
