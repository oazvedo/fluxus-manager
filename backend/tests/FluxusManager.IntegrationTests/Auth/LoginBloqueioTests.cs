using System.Net;
using System.Net.Http.Json;
using FluxusManager.Application.DTOs.AuthDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Security;
using FluxusManager.Domain.Entities;
using FluxusManager.Infrastructure.Database;
using FluxusManager.IntegrationTests.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluxusManager.IntegrationTests.Auth;

/// <summary>Bloqueio por senhas erradas com o SQL real (incremento atômico e janela de tempo). Limite: 3 em 15 minutos.</summary>
[Collection(PostgresCollection.Name)]
public sealed class LoginBloqueioTests(PostgresFixture postgres) : IAsyncLifetime
{
    private readonly Relogio _clock = new();
    private BloqueioFactory _factory = null!;
    private HttpClient _client = null!;
    private Guid _usuarioId;

    public async Task InitializeAsync()
    {
        _factory = new BloqueioFactory(await postgres.CreateDatabaseAsync(), _clock);
        _client = _factory.CreateClient();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var user = new Usuario("Ana", "ana@fluxus.com", hasher.Hash("segredo123"));
        var company = new Empresa("Fluxus", null, "11222333000181");
        var perfil = Perfil.CriarPadrao(company.Id, "Consulta", "", PermissionCatalog.ReadOnly);
        db.AddRange(user, company, perfil, new UsuarioEmpresa(user.Id, company.Id, perfil.Id));
        await db.SaveChangesAsync();
        _usuarioId = user.Id;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private Task<HttpResponseMessage> LoginAsync(string senha, string email = "ana@fluxus.com")
        => _client.PostAsJsonAsync("/auth/login", new LoginRequest(email, senha));

    private async Task<Usuario> UsuarioAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<Usuario>().AsNoTracking().SingleAsync();
    }

    [Fact]
    public async Task SenhasErradasSeguidas_BloqueiamOLogin_ComAMesmaRespostaGenerica_AtePrazoOuDesbloqueio()
    {
        for (var i = 0; i < 3; i++)
            (await LoginAsync("errada123")).Dispose();

        using var bloqueado = await LoginAsync("segredo123");
        using var inexistente = await LoginAsync("segredo123", "nao@existe.com");
        Assert.Equal(HttpStatusCode.Unauthorized, bloqueado.StatusCode);
        Assert.Equal(await inexistente.Content.ReadAsStringAsync(), await bloqueado.Content.ReadAsStringAsync());

        await using (var scope = _factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<IUsuarioService>().DesbloquearAsync(_usuarioId);
        using var desbloqueado = await LoginAsync("segredo123");
        Assert.Equal(HttpStatusCode.OK, desbloqueado.StatusCode);
    }

    [Fact]
    public async Task FalhasForaDaJanela_NaoSomam_EOBloqueioTerminaSozinho()
    {
        (await LoginAsync("errada123")).Dispose();
        (await LoginAsync("errada123")).Dispose();
        _clock.Avancar(TimeSpan.FromMinutes(16));
        (await LoginAsync("errada123")).Dispose();
        Assert.Equal((1, null), ((await UsuarioAsync()).TentativasLoginFalhas, (await UsuarioAsync()).BloqueadoAte));

        (await LoginAsync("errada123")).Dispose();
        (await LoginAsync("errada123")).Dispose();
        Assert.NotNull((await UsuarioAsync()).BloqueadoAte);

        _clock.Avancar(TimeSpan.FromMinutes(15));
        using var liberado = await LoginAsync("segredo123");
        Assert.Equal(HttpStatusCode.OK, liberado.StatusCode);
        Assert.Equal((0, null), ((await UsuarioAsync()).TentativasLoginFalhas, (await UsuarioAsync()).BloqueadoAte));
    }

    [Fact]
    public async Task FalhasSimultaneas_SaoTodasContadas()
    {
        var respostas = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => LoginAsync("errada123")));
        foreach (var resposta in respostas)
            resposta.Dispose();

        // 5 falhas com limite 3: bloqueia na 3ª e recomeça a contagem (2), sem perder incrementos.
        var usuario = await UsuarioAsync();
        Assert.NotNull(usuario.BloqueadoAte);
        Assert.Equal(2, usuario.TentativasLoginFalhas);
    }

    private sealed class Relogio : TimeProvider
    {
        private DateTimeOffset _agora = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _agora;
        public void Avancar(TimeSpan tempo) => _agora += tempo;
    }

    private sealed class BloqueioFactory(string connectionString, TimeProvider clock)
        : ApiComBancoFactory(connectionString, useTestAuthentication: false, useRealAuthService: true)
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Login:MaxTentativas", "3");
            builder.UseSetting("Login:BloqueioMinutos", "15");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(clock);
            });
        }
    }
}
