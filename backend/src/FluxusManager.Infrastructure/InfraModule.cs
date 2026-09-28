using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using FluxusManager.Infrastructure.Email;
using FluxusManager.Infrastructure.Repositories;
using FluxusManager.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FluxusManager.Infrastructure;

/// <summary>
/// Registro de todas as dependências da camada Infrastructure.
/// Toda classe/interface nova desta camada (repositórios, etc.) deve ser registrada aqui.
/// </summary>
public static class InfraModule
{
    public static IServiceCollection AddInfraModule(this IServiceCollection services, IConfiguration configuration)
    {
        AddDatabase(services, configuration);
        AddRepositories(services);
        AddSecurity(services);
        AddEmail(services, configuration);
        services.AddOptions<RefreshTokenOptions>().Bind(configuration.GetSection(RefreshTokenOptions.SectionName))
            .Validate(o => o.DuracaoDias is >= 1 and <= 90, "RefreshTokens:DuracaoDias deve estar entre 1 e 90.")
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<RefreshTokenCleanup>();

        return services;
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não configurada.");

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHostedService<AuditPartitionMaintenance>();
    }

    private static void AddRepositories(IServiceCollection services)
    {
        // Repositório genérico para entidades sem consultas próprias; os específicos são registrados abaixo.
        services.AddScoped(typeof(IRepository<>), typeof(RepositoryBase<>));

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IEmpresaRepository, EmpresaRepository>();
        services.AddScoped<IUsuarioEmpresaRepository, UsuarioEmpresaRepository>();
        services.AddScoped<IFilialRepository, FilialRepository>();
        services.AddScoped<IPerfilRepository, PerfilRepository>();
        services.AddScoped<IConviteRepository, ConviteRepository>();
    }

    private static void AddSecurity(IServiceCollection services)
    {
        // Sem estado: uma única instância serve a aplicação inteira.
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();
    }

    private static void AddEmail(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>().Bind(configuration.GetSection(EmailOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Host), "Email:Host é obrigatório.")
            .Validate(o => o.Port is >= 1 and <= 65535, "Email:Port deve estar entre 1 e 65535.")
            .Validate(o => Enum.IsDefined(o.Seguranca), "Email:Seguranca deve ser Nenhuma, StartTls ou SslTls.")
            .Validate(o => System.Net.Mail.MailAddress.TryCreate(o.RemetenteEmail, out var endereco) && endereco.Address == o.RemetenteEmail,
                "Email:RemetenteEmail deve ser um e-mail válido.")
            .Validate(o => o.TimeoutSegundos is >= 1 and <= 300, "Email:TimeoutSegundos deve estar entre 1 e 300.")
            .ValidateOnStart();

        // Sem estado: cada envio abre a própria conexão SMTP.
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
    }
}
