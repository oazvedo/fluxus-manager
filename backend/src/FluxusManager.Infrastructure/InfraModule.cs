using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using FluxusManager.Infrastructure.Repositories;
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
    }

    private static void AddRepositories(IServiceCollection services)
    {
        // Repositório genérico para entidades sem consultas próprias; os específicos são registrados abaixo.
        services.AddScoped(typeof(IRepository<>), typeof(RepositoryBase<>));

        // services.AddScoped<IEmpresaRepository, EmpresaRepository>();
    }
}
