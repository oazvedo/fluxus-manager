using System.Globalization;
using FluentValidation;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Options;
using FluxusManager.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FluxusManager.Application;

/// <summary>
/// Registro de todas as dependências da camada Application.
/// Toda classe/interface nova desta camada (services, etc.) deve ser registrada aqui.
/// </summary>
public static class ApplicationModule
{
    public static IServiceCollection AddApplicationModule(this IServiceCollection services, IConfiguration configuration)
    {
        AddServices(services);
        AddValidators(services);
        AddOptions(services, configuration);

        return services;
    }

    private static void AddOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ConviteOptions>().Bind(configuration.GetSection(ConviteOptions.SectionName))
            .Validate(o => o.ValidadeHoras is >= 1 and <= 720, "Convites:ValidadeHoras deve estar entre 1 e 720.")
            .ValidateOnStart();
        services.AddOptions<LoginOptions>().Bind(configuration.GetSection(LoginOptions.SectionName))
            .Validate(o => o.MaxTentativas is >= 3 and <= 20, "Login:MaxTentativas deve estar entre 3 e 20.")
            .Validate(o => o.BloqueioMinutos is >= 1 and <= 1440, "Login:BloqueioMinutos deve estar entre 1 e 1440.")
            .ValidateOnStart();
        services.AddOptions<PasswordResetOptions>().Bind(configuration.GetSection(PasswordResetOptions.SectionName))
            .Validate(o => o.ValidadeMinutos is >= 5 and <= 1440, "RecuperacaoSenha:ValidadeMinutos deve estar entre 5 e 1440.")
            .ValidateOnStart();
        services.AddOptions<FrontendOptions>().Bind(configuration.GetSection(FrontendOptions.SectionName))
            .Validate(o => Uri.TryCreate(o.Url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https",
                "Frontend:Url deve ser a URL absoluta HTTP(S) do frontend.")
            .ValidateOnStart();
    }

    private static void AddServices(IServiceCollection services)
    {
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IAuditContext, AuditContext>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IEmpresaService, EmpresaService>();
        services.AddScoped<IUsuarioEmpresaService, UsuarioEmpresaService>();
        services.AddScoped<IFilialService, FilialService>();
        services.AddScoped<IPerfilService, PerfilService>();
        services.AddScoped<IConviteService, ConviteService>();
    }

    private static void AddValidators(IServiceCollection services)
    {
        // Todo AbstractValidator<T> desta camada é registrado automaticamente e executado pelo ValidationFilter da API.
        services.AddValidatorsFromAssembly(typeof(ApplicationModule).Assembly);

        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("pt-BR");
    }
}
