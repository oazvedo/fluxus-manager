using FluxusManager.Application.Interfaces;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
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

        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IEmpresaRepository, EmpresaRepository>();
    }

    private static void AddSecurity(IServiceCollection services)
    {
        // Sem estado: uma única instância serve a aplicação inteira.
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
    }
}
