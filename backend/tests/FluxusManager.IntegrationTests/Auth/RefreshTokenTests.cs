using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluxusManager.Application.DTOs.AuthDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Security;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using FluxusManager.IntegrationTests.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluxusManager.IntegrationTests.Auth;

[Collection(PostgresCollection.Name)]
public sealed class RefreshTokenTests(PostgresFixture postgres) : IAsyncLifetime
{
    private AuthDatabaseFactory _factory = null!;
    private HttpClient _client = null!;
    private Guid _userId;
    private Guid _companyId;
    private Guid _secondCompanyId;
    private readonly TestClock _clock = new();

    public async Task InitializeAsync()
    {
        _factory = new AuthDatabaseFactory(await postgres.CreateDatabaseAsync(), _clock);
        _client = _factory.CreateClient();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var user = new Usuario("Ana", "ana@fluxus.com", hasher.Hash("segredo123"));
        var company = new Empresa("Fluxus", null, "11222333000181");
        var secondCompany = new Empresa("Outra", null, "11444777000161");
        var admin = Perfil.CriarPadrao(company.Id, "Administrador", "", PermissionCatalog.All);
        var consulta = Perfil.CriarPadrao(company.Id, "Consulta", "", PermissionCatalog.ReadOnly);
        var secondCompanyConsulta = Perfil.CriarPadrao(secondCompany.Id, "Consulta", "", PermissionCatalog.ReadOnly);
        db.AddRange(user, company, secondCompany, admin, consulta, secondCompanyConsulta,
            new UsuarioEmpresa(user.Id, company.Id, admin.Id),
            new UsuarioEmpresa(user.Id, secondCompany.Id, secondCompanyConsulta.Id));
        await db.SaveChangesAsync();
        _userId = user.Id;
        _companyId = company.Id;
        _secondCompanyId = secondCompany.Id;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<TokenResponse> LoginAsync()
    {
        using var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequest("ana@fluxus.com", "segredo123", _companyId));
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.CacheControl?.NoStore);
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    private Task<HttpResponseMessage> RefreshAsync(string token, Guid? company = null)
        => _client.PostAsJsonAsync("/auth/refresh", new RefreshRequest(token, company));

    private async Task<TokenResponse> RotateAsync(string token, Guid? company = null)
    {
        using var response = await RefreshAsync(token, company);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    [Fact]
    public async Task LoginERotacao_GravamApenasHash_EPreservamExpiracaoDaFamilia()
    {
        var login = await LoginAsync();
        Assert.Equal(64, login.RefreshToken!.Length);
        _clock.Advance(TimeSpan.FromDays(2));
        // Renovação precisa funcionar mesmo com Bearer inválido/expirado enviado pelo cliente.
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "expirado");
        var rotated = await RotateAsync(login.RefreshToken);
        Assert.NotEqual(login.RefreshToken, rotated.RefreshToken);
        Assert.Equal(login.RefreshTokenExpiresAt, rotated.RefreshTokenExpiresAt);
        Assert.Equal(_companyId.ToString(), new JwtSecurityTokenHandler().ReadJwtToken(rotated.AccessToken).Claims.Single(c => c.Type == "tenant_id").Value);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rotated.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/empresas")).StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokens = await db.Set<RefreshToken>().AsNoTracking().ToListAsync();
        Assert.Equal(2, tokens.Count);
        Assert.DoesNotContain(tokens, t => t.TokenHash == login.RefreshToken || t.TokenHash == rotated.RefreshToken);
        var previous = tokens.Single(t => t.TokenHash == SecretToken.Hash(login.RefreshToken));
        var current = tokens.Single(t => t.TokenHash == SecretToken.Hash(rotated.RefreshToken!));
        Assert.NotNull(previous.RevogadoEm);
        Assert.Equal(current.Id, previous.SubstituidoPorId);
        Assert.Equal(previous.FamiliaId, current.FamiliaId);
        Assert.Null(current.RevogadoEm);
        var audit = await db.Database.SqlQuery<string>($"""
            SELECT coalesce(old_values::text, '') || coalesce(new_values::text, '') AS "Value"
            FROM audit.change_log WHERE table_name = 'refresh_tokens'
            """).ToListAsync();
        Assert.NotEmpty(audit);
        foreach (var entry in audit)
        {
            Assert.DoesNotContain(login.RefreshToken, entry);
            Assert.DoesNotContain(previous.TokenHash, entry);
            Assert.DoesNotContain(rotated.RefreshToken!, entry);
            Assert.DoesNotContain(current.TokenHash, entry);
        }
    }

    [Fact]
    public async Task Reuso_RevogaDescendentes_MasPreservaOutroLogin()
    {
        var login = await LoginAsync();
        var independent = await LoginAsync();
        var rotated = await RotateAsync(login.RefreshToken!);
        var latest = await RotateAsync(rotated.RefreshToken!);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(login.RefreshToken!)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(latest.RefreshToken!)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(independent.RefreshToken!)).StatusCode);
    }

    [Fact]
    public async Task Logout_ComTokenAnterior_RevogaFamilia_EEhIdempotente()
    {
        var login = await LoginAsync();
        var rotated = await RotateAsync(login.RefreshToken!);
        for (var i = 0; i < 2; i++)
            Assert.Equal(HttpStatusCode.NoContent, (await _client.PostAsJsonAsync("/auth/logout", new LogoutRequest(login.RefreshToken!))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(rotated.RefreshToken!)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PostAsJsonAsync("/auth/logout", new LogoutRequest(new string('A', 64)))).StatusCode);
    }

    [Fact]
    public async Task Refresh_Simultaneo_SoUmRotaciona_EReusoRevogaONovoToken()
    {
        var login = await LoginAsync();
        var responses = await Task.WhenAll(RefreshAsync(login.RefreshToken!), RefreshAsync(login.RefreshToken!));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Unauthorized);
        var success = await responses.Single(r => r.StatusCode == HttpStatusCode.OK).Content.ReadFromJsonAsync<TokenResponse>();
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(success!.RefreshToken!)).StatusCode);
    }

    [Fact]
    public async Task LogoutSimultaneoComRefresh_NaoDeixaDescendenteAtivo()
    {
        var login = await LoginAsync();
        var refreshTask = RefreshAsync(login.RefreshToken!);
        var logoutTask = _client.PostAsJsonAsync("/auth/logout", new LogoutRequest(login.RefreshToken!));
        var responses = await Task.WhenAll(refreshTask, logoutTask);
        using var refresh = responses[0];
        using var logout = responses[1];
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        if (refresh.StatusCode == HttpStatusCode.OK)
        {
            var success = await refresh.Content.ReadFromJsonAsync<TokenResponse>();
            Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(success!.RefreshToken!)).StatusCode);
        }
        else
            Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Theory]
    [InlineData("usuario")]
    [InlineData("empresa")]
    [InlineData("vinculo")]
    [InlineData("excluido")]
    public async Task Refresh_RevalidaUsuarioEmpresaEVinculo(string disabled)
    {
        var login = await LoginAsync();
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (disabled == "usuario") (await db.Set<Usuario>().SingleAsync(u => u.Id == _userId)).Inativar();
            if (disabled == "empresa") (await db.Set<Empresa>().SingleAsync(e => e.Id == _companyId)).Inativar();
            var link = await db.Set<UsuarioEmpresa>().SingleAsync(v => v.UsuarioId == _userId && v.EmpresaId == _companyId);
            if (disabled == "vinculo") link.Inativar();
            if (disabled == "excluido") db.Remove(link);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(login.RefreshToken!)).StatusCode);
    }

    [Fact]
    public async Task Refresh_RecalculaPermissoes_EPermiteEmpresaVinculada()
    {
        var login = await LoginAsync();
        var rotated = await RotateAsync(login.RefreshToken!, _secondCompanyId);
        Assert.Equal(_secondCompanyId, rotated.TenantId);
        Assert.Equal("Consulta", rotated.Role);
        Assert.DoesNotContain("empresas.editar", rotated.Permissions);
        var again = await RotateAsync(rotated.RefreshToken!);
        Assert.Equal(_secondCompanyId, again.TenantId);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(again.RefreshToken!, Guid.NewGuid())).StatusCode);
    }

    [Fact]
    public async Task Refresh_PerfilAlterado_UsaPermissoesAtuais()
    {
        var login = await LoginAsync();
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenant(_companyId);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Set<UsuarioEmpresa>().SingleAsync(v => v.UsuarioId == _userId && v.EmpresaId == _companyId))
                .AtualizarPerfil(await db.Set<Perfil>().SingleAsync(p => p.TenantId == _companyId && p.Nome == "Consulta"));
            await db.SaveChangesAsync();
        }
        var rotated = await RotateAsync(login.RefreshToken!);
        Assert.DoesNotContain("empresas.editar", rotated.Permissions);
    }

    [Fact]
    public async Task Limpeza_PreservaTokensUsadosAteExpiracaoDaFamilia()
    {
        var login = await LoginAsync();
        var rotated = await RotateAsync(login.RefreshToken!);
        await using var scope = _factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
        Assert.Equal(0, await repository.LimparExpiradosAsync(_clock.GetUtcNow().UtcDateTime));
        _clock.Advance(TimeSpan.FromDays(8));
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(rotated.RefreshToken!)).StatusCode);
        Assert.Equal(2, await repository.LimparExpiradosAsync(_clock.GetUtcNow().UtcDateTime));
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(login.RefreshToken!)).StatusCode);
        Assert.Equal(0, await repository.LimparExpiradosAsync(_clock.GetUtcNow().UtcDateTime));
    }

    [Fact]
    public async Task Limpeza_NaoRemoveFamiliaBloqueadaPorRenovacaoOuLogout()
    {
        var login = await LoginAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
        var token = await repository.ObterPorHashAsync(SecretToken.Hash(login.RefreshToken!));
        await using var transaction = await db.Database.BeginTransactionAsync();
        await repository.BloquearFamiliaAsync(token!.FamiliaId);
        _clock.Advance(TimeSpan.FromDays(8));
        await using var cleanupScope = _factory.Services.CreateAsyncScope();
        var cleanupRepository = cleanupScope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
        Assert.Equal(0, await cleanupRepository.LimparExpiradosAsync(_clock.GetUtcNow().UtcDateTime));
        await transaction.CommitAsync();
        Assert.Equal(1, await cleanupRepository.LimparExpiradosAsync(_clock.GetUtcNow().UtcDateTime));
    }

    [Fact]
    public async Task Refresh_TokenInexistenteOuCorpoInvalido_RejeitaSemEmitirCredenciais()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(new string('A', 64))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await RefreshAsync("")).StatusCode);
    }

    private sealed class TestClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan elapsed) => _now += elapsed;
    }

    private sealed class AuthDatabaseFactory(string connectionString, TestClock clock)
        : ApiFactory("Development", useTestAuthentication: false, useRealAuthService: true)
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
            builder.UseSetting("Database:MigrateOnStartup", "true");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(clock);
            });
        }
    }
}
