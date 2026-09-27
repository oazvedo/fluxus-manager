using System.Security.Claims;
using System.Text.Encodings.Web;
using FluxusManager.Application.DTOs.AuthDtos;
using FluxusManager.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluxusManager.IntegrationTests;

/// <summary>
/// Sobe a API em memória, incluindo os controllers de teste deste assembly.
/// Não acessa o banco: a connection string só precisa existir para o InfraModule.
/// </summary>
public class ApiFactory(string environment, bool useTestAuthentication = true, bool useRealAuthService = false) : WebApplicationFactory<Program>
{
    public ApiFactory() : this("Development")
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=fluxus_tests");
        builder.UseSetting("Jwt:SigningKey", "integration-tests-only-signing-key-32-bytes-minimum");
        // Sem banco por padrão; testes que precisam dele usam o ApiComBancoFactory.
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("DevelopmentSeed:Enabled", "false");
        builder.UseSetting("RefreshTokens:LimpezaHabilitada", "false");
        builder.UseSetting("Filiais:ValidarRaizCnpjDaEmpresa", "false");
        // Só para a validação na subida também em Production; nenhum teste daqui envia e-mail.
        builder.UseSetting("Email:Host", "localhost");
        builder.UseSetting("Email:RemetenteEmail", "testes@fluxus.local");
        builder.UseSetting("Frontend:Url", "http://localhost:5173");
        // A migration já cria as partições da auditoria; a rotina diária não é necessária nos testes.
        builder.UseSetting("Auditoria:ManutencaoParticoes", "false");
        builder.ConfigureServices(services =>
        {
            if (useTestAuthentication)
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "IntegrationTests";
                    options.DefaultChallengeScheme = "IntegrationTests";
                    options.DefaultScheme = "IntegrationTests";
                }).AddScheme<AuthenticationSchemeOptions, IntegrationTestAuthHandler>("IntegrationTests", _ => { });
            }
            if (!useRealAuthService)
            {
                services.RemoveAll<IAuthService>();
                services.AddSingleton<IAuthService, IntegrationTestAuthService>();
            }
            services.AddControllers().AddApplicationPart(typeof(ApiFactory).Assembly);
        });
    }
}

internal sealed class IntegrationTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new("sub", Request.Headers.TryGetValue("X-Test-User", out var user) ? user.ToString() : Guid.NewGuid().ToString()),
            new("tenant_id", Request.Headers.TryGetValue("X-Test-Tenant", out var tenant) ? tenant.ToString() : Guid.NewGuid().ToString()),
            new("role", "Administrador")
        };
        claims.AddRange(new[]
        {
            "empresas.visualizar", "empresas.editar", "filiais.visualizar", "filiais.editar",
            "usuarios.visualizar", "usuarios.editar", "usuarios-empresas.visualizar", "usuarios-empresas.editar",
            "perfis.visualizar", "perfis.editar"
        }.Select(permission => new Claim("permissions", permission)));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}

internal sealed class IntegrationTestAuthService : IAuthService
{
    public Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) => Task.FromResult<TokenResponse?>(null);

    public Task<TokenResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default) => Task.FromResult<TokenResponse?>(null);

    public Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<TokenResponse> SwitchTenantAsync(Guid userId, Guid empresaId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
