using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FluxusManager.Application.DTOs.AuthDtos;
using FluxusManager.Application.DTOs.ConvitesDtos;
using FluxusManager.Application.Email;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Security;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Exceptions;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using FluxusManager.IntegrationTests.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FluxusManager.IntegrationTests.Convites;

[Collection(PostgresCollection.Name)]
public sealed partial class ConvitesEndpointsTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ConvitesFactory _factory = null!;
    private HttpClient _client = null!;
    private Guid _empresaId;
    private Guid _perfilId;

    public async Task InitializeAsync()
    {
        _factory = new ConvitesFactory(await postgres.CreateDatabaseAsync());
        _client = _factory.CreateClient();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var empresa = new Empresa("Acme Ltda", "Acme", "11222333000181");
        var perfil = Perfil.CriarPadrao(empresa.Id, "Consulta", "", PermissionCatalog.ReadOnly);
        db.AddRange(empresa, perfil);
        await db.SaveChangesAsync();
        (_empresaId, _perfilId) = (empresa.Id, perfil.Id);
        _client.DefaultRequestHeaders.Add("X-Test-Tenant", _empresaId.ToString());
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private string UltimoToken() => TokenNoLink().Match(_factory.Emails.Mensagens.Last().Texto).Groups[1].Value;

    [Fact]
    public async Task ConviteAceitoPorNovoUsuario_PermiteLoginNaEmpresa()
    {
        using var criado = await _client.PostAsJsonAsync("/convites", new CriarConviteRequest("Ana@Acme.com", _perfilId));
        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        var convite = (await criado.Content.ReadFromJsonAsync<ConviteResponse>())!;
        Assert.Equal($"/convites/{convite.Id}", criado.Headers.Location?.AbsolutePath);
        Assert.DoesNotContain("token", await criado.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        var token = UltimoToken();

        // O cliente de teste manda um tenant aleatório na claim: as rotas públicas precisam ignorá-lo.
        using var anonimo = _factory.CreateClient();
        var detalhes = await anonimo.PostAsJsonAsync("/convites/consultar", new ConsultarConviteRequest(token));
        Assert.Equal(HttpStatusCode.OK, detalhes.StatusCode);
        Assert.True(detalhes.Headers.CacheControl?.NoStore);
        var dados = (await detalhes.Content.ReadFromJsonAsync<ConviteDetalhesResponse>())!;
        Assert.Equal(("ana@acme.com", "Acme", "Consulta", false), (dados.Email, dados.Empresa, dados.Perfil, dados.UsuarioExistente));

        using var aceite = await anonimo.PostAsJsonAsync("/convites/aceitar", new AceitarConviteRequest(token, "Ana", "segredo123"));
        Assert.Equal(HttpStatusCode.NoContent, aceite.StatusCode);

        using var login = await anonimo.PostAsJsonAsync("/auth/login", new LoginRequest("ana@acme.com", "segredo123", _empresaId));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(_empresaId, (await login.Content.ReadFromJsonAsync<TokenResponse>())!.TenantId);

        var lista = JsonNode.Parse(await _client.GetStringAsync("/convites"))!;
        Assert.Equal("Aceito", lista["items"]![0]!["status"]!.GetValue<string>());
        using var repetido = await anonimo.PostAsJsonAsync("/convites/aceitar", new AceitarConviteRequest(token, "Ana", "segredo123"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, repetido.StatusCode);
    }

    [Fact]
    public async Task Post_ConviteDuplicado_Retorna409_EReenviarECancelarFuncionam()
    {
        using var primeiro = await _client.PostAsJsonAsync("/convites", new CriarConviteRequest("ana@acme.com", _perfilId));
        var convite = (await primeiro.Content.ReadFromJsonAsync<ConviteResponse>())!;

        using var duplicado = await _client.PostAsJsonAsync("/convites", new CriarConviteRequest("ANA@acme.com", _perfilId));
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode);

        using var reenvio = await _client.PostAsync($"/convites/{convite.Id}/reenviar", null);
        Assert.Equal(HttpStatusCode.OK, reenvio.StatusCode);
        Assert.Equal(2, _factory.Emails.Mensagens.Count);

        using var cancelamento = await _client.PatchAsync($"/convites/{convite.Id}/cancelar", null);
        Assert.Equal(HttpStatusCode.NoContent, cancelamento.StatusCode);
        var cancelado = await _client.GetFromJsonAsync<ConviteResponse>($"/convites/{convite.Id}");
        Assert.Equal("Cancelado", cancelado!.Status);
    }

    [Fact]
    public async Task ConvitesDeOutraEmpresa_NaoSaoVisiveis()
    {
        using var criado = await _client.PostAsJsonAsync("/convites", new CriarConviteRequest("ana@acme.com", _perfilId));
        var convite = (await criado.Content.ReadFromJsonAsync<ConviteResponse>())!;

        using var outraEmpresa = _factory.CreateClient();
        outraEmpresa.DefaultRequestHeaders.Add("X-Test-Tenant", Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.NotFound, (await outraEmpresa.GetAsync($"/convites/{convite.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outraEmpresa.PatchAsync($"/convites/{convite.Id}/cancelar", null)).StatusCode);
    }

    [Fact]
    public async Task Requisicoes_Invalidas_Retornam400ComOsCampos_E404ParaTokenDesconhecido()
    {
        using var criar = await _client.PostAsJsonAsync("/convites", new { email = "nao-e-email", perfilId = Guid.Empty });
        using var aceitar = await _client.PostAsJsonAsync("/convites/aceitar", new { token = "abc", senha = "curta" });
        using var desconhecido = await _client.PostAsJsonAsync("/convites/consultar", new ConsultarConviteRequest(SecretToken.Create()));

        Assert.Equal(HttpStatusCode.BadRequest, criar.StatusCode);
        var erros = JsonNode.Parse(await criar.Content.ReadAsStringAsync())!["errors"]!.AsObject();
        Assert.Contains("email", erros.Select(e => e.Key));
        Assert.Contains("perfilId", erros.Select(e => e.Key));
        Assert.Equal(HttpStatusCode.BadRequest, aceitar.StatusCode);
        var errosAceite = JsonNode.Parse(await aceitar.Content.ReadAsStringAsync())!["errors"]!.AsObject();
        Assert.Contains("token", errosAceite.Select(e => e.Key));
        Assert.Contains("senha", errosAceite.Select(e => e.Key));
        Assert.Equal(HttpStatusCode.NotFound, desconhecido.StatusCode);
    }

    [Fact]
    public async Task PerfilComConvitePendente_NaoPodeSerExcluido()
    {
        using var _ = await _client.PostAsJsonAsync("/convites", new CriarConviteRequest("ana@acme.com", _perfilId));

        using var exclusao = await _client.DeleteAsync($"/perfis/{_perfilId}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, exclusao.StatusCode);
    }

    [Fact]
    public async Task AlteracoesConcorrentesNoMesmoConvite_ASegundaFalhaComConflito()
    {
        using var criado = await _client.PostAsJsonAsync("/convites", new CriarConviteRequest("ana@acme.com", _perfilId));
        var id = (await criado.Content.ReadFromJsonAsync<ConviteResponse>())!.Id;
        await using var primeira = _factory.Services.CreateAsyncScope();
        await using var segunda = _factory.Services.CreateAsyncScope();
        var (conviteA, uowA) = await CarregarAsync(primeira);
        var (conviteB, uowB) = await CarregarAsync(segunda);

        conviteA.Cancelar(DateTime.UtcNow);
        await uowA.CommitAsync();
        conviteB.Aceitar(Guid.NewGuid(), DateTime.UtcNow);

        await Assert.ThrowsAsync<ConflictException>(() => uowB.CommitAsync());

        async Task<(Convite, IUnitOfWork)> CarregarAsync(AsyncServiceScope scope)
        {
            scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenant(_empresaId);
            var convite = await scope.ServiceProvider.GetRequiredService<IConviteRepository>().GetByIdAsync(id);
            return (convite!, scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        }
    }

    [Fact]
    public async Task TabelaDeConvites_NaoAuditaOHashDoToken()
    {
        using var _ = await _client.PostAsJsonAsync("/convites", new CriarConviteRequest("ana@acme.com", _perfilId));

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var valores = await db.Database.SqlQuery<string>(
            $"SELECT new_values::text AS \"Value\" FROM audit.change_log WHERE table_name = 'convites'").ToListAsync();

        Assert.Contains(valores, v => v.Contains("ana@acme.com"));
        Assert.All(valores, v => Assert.DoesNotContain(SecretToken.Hash(UltimoToken()), v));
    }

    [GeneratedRegex("token=([0-9A-F]{64})")]
    private static partial Regex TokenNoLink();

    private sealed class EmailsEnviados : IEmailSender
    {
        public List<MensagemEmail> Mensagens { get; } = [];

        public Task EnviarAsync(MensagemEmail mensagem, CancellationToken cancellationToken = default)
        {
            lock (Mensagens)
                Mensagens.Add(mensagem);
            return Task.CompletedTask;
        }
    }

    private sealed class ConvitesFactory(string connectionString)
        : ApiFactory("Development", useTestAuthentication: true, useRealAuthService: true)
    {
        public EmailsEnviados Emails { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
            builder.UseSetting("Database:MigrateOnStartup", "true");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(Emails);
            });
        }
    }
}
