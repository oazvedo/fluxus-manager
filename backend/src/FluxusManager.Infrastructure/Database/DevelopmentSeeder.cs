using FluxusManager.Application.Interfaces;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FluxusManager.Infrastructure.Database;

public static class DevelopmentSeeder
{
    public static readonly Guid AdminId = Guid.Parse("0195cfe0-0000-7000-8000-000000000001");
    public const string AdminEmail = "admin@fluxus.local";
    public const string EmpresaCnpj = "47986213000106";

    /// <summary>Bootstrap local atômico; nunca restaura acessos alterados ou excluídos pelo administrador.</summary>
    public static async Task SeedDevelopmentAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var environment = services.GetRequiredService<IHostEnvironment>();
        var configuration = services.GetRequiredService<IConfiguration>();
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("DevelopmentSeed:Enabled"))
            return;

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DevelopmentSeeder));
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // Serializa o bootstrap caso dois processos locais iniciem juntos.
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(5959001)", ct);
            if (await db.Set<Usuario>().IgnoreQueryFilters().AnyAsync(u => u.Id == AdminId, ct))
                return;

            var password = configuration["DevelopmentSeed:AdminPassword"];
            if (string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("Seed local não executado. Configure DevelopmentSeed:AdminPassword via user-secrets para criar o administrador.");
                return;
            }
            if (password.Length < 12)
                throw new InvalidOperationException("DevelopmentSeed:AdminPassword deve conter ao menos 12 caracteres.");

            if (await db.Set<Usuario>().IgnoreQueryFilters().AnyAsync(u => u.Email == AdminEmail, ct)
                || await db.Set<Empresa>().IgnoreQueryFilters().AnyAsync(e => e.Cnpj == EmpresaCnpj, ct))
                throw new InvalidOperationException("O e-mail ou CNPJ do seed local já existe. Desabilite DevelopmentSeed:Enabled ou resolva o conflito antes de criar o administrador.");

            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var admin = new Usuario("Administrador local", AdminEmail, hasher.Hash(password));
            db.Add(admin).Property(u => u.Id).CurrentValue = AdminId;
            var empresa = new Empresa("Fluxus Desenvolvimento Ltda", "Fluxus Desenvolvimento", EmpresaCnpj);
            db.AddRange(empresa, new UsuarioEmpresa(admin.Id, empresa.Id, "Administrador"));
        }, cancellationToken);
    }
}
