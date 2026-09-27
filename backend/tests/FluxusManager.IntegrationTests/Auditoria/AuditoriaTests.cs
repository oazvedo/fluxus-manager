using System.Net.Http.Json;
using FluxusManager.Application.DTOs.UsuariosDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Interfaces;
using FluxusManager.Infrastructure.Database;
using FluxusManager.IntegrationTests.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace FluxusManager.IntegrationTests.Auditoria;

[Collection(PostgresCollection.Name)]
public sealed class AuditoriaTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ApiComBancoFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiComBancoFactory(await postgres.CreateDatabaseAsync());
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private sealed record Linha(
        string Operation, string TableName, string? RowId, string? OldValues, string? NewValues,
        string[]? ChangedFields, string? ApplicationUser, Guid? TenantId, string? FrontendUrl,
        string SessionUser, long TransactionId);

    private async Task<List<Linha>> LinhasAsync()
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT operation, table_name, row_id, old_values::text, new_values::text, changed_fields,
                   application_user, tenant_id, frontend_url, session_user_name, transaction_id
            FROM audit.change_log ORDER BY id
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var linhas = new List<Linha>();
        while (await reader.ReadAsync())
        {
            linhas.Add(new Linha(
                reader.GetString(0), reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetFieldValue<string[]>(5),
                reader.IsDBNull(6) ? null : reader.GetString(6), reader.IsDBNull(7) ? null : reader.GetGuid(7),
                reader.IsDBNull(8) ? null : reader.GetString(8), reader.GetString(9), reader.GetInt64(10)));
        }

        return linhas;
    }

    private async Task<T> ScalarAsync<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(_factory.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private async Task<UsuarioResponse> CriarUsuarioAsync(string? tela = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/usuarios")
        {
            Content = JsonContent.Create(new CriarUsuarioRequest("Ana", "ana@fluxus.com", "segredo123")),
        };
        if (tela is not null)
            request.Headers.Add(IAuditContext.FrontendUrlHeader, tela);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UsuarioResponse>())!;
    }

    [Fact]
    public async Task Inclusao_GravaInsertComATelaDoFront_ESemASenha()
    {
        var usuario = await CriarUsuarioAsync("/usuarios/novo");

        var linha = Assert.Single(await LinhasAsync());
        Assert.Equal("INSERT", linha.Operation);
        Assert.Equal("usuarios", linha.TableName);
        Assert.Equal(usuario.Id.ToString(), linha.RowId);
        Assert.Equal("/usuarios/novo", linha.FrontendUrl);
        Assert.Contains("ana@fluxus.com", linha.NewValues);
        Assert.DoesNotContain("senha_hash", linha.NewValues);
        Assert.Null(linha.OldValues);
        Assert.NotEmpty(linha.SessionUser);
        Assert.True(linha.TransactionId > 0);
    }

    [Fact]
    public async Task Alteracao_GravaUpdateComOsCamposAlterados()
    {
        var usuario = await CriarUsuarioAsync();

        var response = await _client.PutAsJsonAsync($"/usuarios/{usuario.Id}", new AtualizarUsuarioRequest("Ana S.", "ana@fluxus.com"));
        response.EnsureSuccessStatusCode();

        var linha = (await LinhasAsync())[^1];
        Assert.Equal("UPDATE", linha.Operation);
        Assert.Equal(["atualizado_em", "nome"], linha.ChangedFields!);
        Assert.Contains("\"nome\": \"Ana\"", linha.OldValues);
        Assert.Contains("\"nome\": \"Ana S.\"", linha.NewValues);
    }

    [Fact]
    public async Task UpdateSemMudanca_NaoEhAuditado()
    {
        await CriarUsuarioAsync();

        await ScalarAsync<int>("WITH u AS (UPDATE usuarios SET nome = nome RETURNING 1) SELECT count(*)::int FROM u");

        Assert.Single(await LinhasAsync());
    }

    [Fact]
    public async Task TrocaDeSenha_AparecemSoONomeDoCampo()
    {
        await CriarUsuarioAsync();

        await ScalarAsync<int>("WITH u AS (UPDATE usuarios SET senha_hash = 'outro-hash' RETURNING 1) SELECT count(*)::int FROM u");

        var linha = (await LinhasAsync())[^1];
        Assert.Equal(["senha_hash"], linha.ChangedFields!);
        Assert.DoesNotContain("hash", linha.OldValues);
        Assert.DoesNotContain("hash", linha.NewValues);
    }

    [Fact]
    public async Task ContextoDaAplicacao_GravaUsuarioETenant()
    {
        var tenantId = Guid.CreateVersion7();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenant(tenantId);
            scope.ServiceProvider.GetRequiredService<IAuditContext>().SetUser("usuario-7");
            scope.ServiceProvider.GetRequiredService<IUsuarioRepository>().Add(new Usuario("Bia", "bia@fluxus.com", "hash"));
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync();
        }

        var linha = Assert.Single(await LinhasAsync());
        Assert.Equal("usuario-7", linha.ApplicationUser);
        Assert.Equal(tenantId, linha.TenantId);
    }

    [Fact]
    public async Task ExclusaoLogica_EhAuditadaComoUpdate()
    {
        var usuario = await CriarUsuarioAsync();

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var usuarios = scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();
            usuarios.Remove((await usuarios.GetByIdAsync(usuario.Id))!);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync();
        }

        var linha = (await LinhasAsync())[^1];
        Assert.Equal("UPDATE", linha.Operation);
        Assert.Equal(["atualizado_em", "excluido"], linha.ChangedFields!);
        Assert.Equal(1L, await ScalarAsync<long>("SELECT count(*) FROM usuarios WHERE excluido"));
    }

    [Fact]
    public async Task ExclusaoFisica_GravaDeleteComOsValoresAntigos()
    {
        await CriarUsuarioAsync();

        await ScalarAsync<int>("WITH u AS (DELETE FROM usuarios RETURNING 1) SELECT count(*)::int FROM u");

        var linha = (await LinhasAsync())[^1];
        Assert.Equal("DELETE", linha.Operation);
        Assert.Contains("ana@fluxus.com", linha.OldValues);
        Assert.DoesNotContain("senha_hash", linha.OldValues);
        Assert.Null(linha.NewValues);
    }

    [Fact]
    public async Task TodaTabelaDoSistema_TemOTriggerDeAuditoria()
    {
        _ = _factory.Server; // sobe a API, que aplica as migrations

        var semTrigger = await ScalarAsync<string[]>("""
            SELECT coalesce(array_agg(t.table_name ORDER BY t.table_name), '{}')
            FROM information_schema.tables t
            WHERE t.table_schema = 'public' AND t.table_type = 'BASE TABLE'
              AND t.table_name <> '__EFMigrationsHistory'
              AND NOT EXISTS (
                  SELECT 1 FROM pg_trigger g
                  WHERE g.tgrelid = format('public.%I', t.table_name)::regclass AND g.tgname = 'audit_changes')
            """);

        Assert.True(semTrigger.Length == 0,
            $"Tabelas sem auditoria: {string.Join(", ", semTrigger)}. Chame migrationBuilder.EnableAuditTracking(...) na migration que cria a tabela.");
    }

    [Fact]
    public async Task Linhas_CaemNaParticaoDoMes_NaoNaDefault()
    {
        await CriarUsuarioAsync();

        var particao = await ScalarAsync<string>("SELECT tableoid::regclass::text FROM audit.change_log LIMIT 1");

        Assert.Equal($"audit.change_log_{DateTime.UtcNow:yyyy_MM}", particao);
    }

    [Fact]
    public async Task RotinaDeManutencao_CriaOsProximosMeses_EAplicaARetencao()
    {
        _ = _factory.Server;
        await ScalarAsync<string>("SELECT audit.create_partition('2020-01-15')::text");
        await ScalarAsync<string>("DROP TABLE audit.change_log_" + DateTime.UtcNow.AddMonths(3).ToString("yyyy_MM") + "; SELECT ''");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Auditoria:RetencaoMeses"] = "12" })
            .Build();
        var rotina = new AuditPartitionMaintenance(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(), configuration,
            NullLogger<AuditPartitionMaintenance>.Instance);

        await rotina.RunOnceAsync(CancellationToken.None);

        Assert.True(await ScalarAsync<bool>(
            $"SELECT to_regclass('audit.change_log_{DateTime.UtcNow.AddMonths(3):yyyy_MM}') IS NOT NULL"));
        Assert.False(await ScalarAsync<bool>("SELECT to_regclass('audit.change_log_2020_01') IS NOT NULL"));
    }

    [Fact]
    public async Task Retencao_ApagaSoAsParticoesAntigas()
    {
        _ = _factory.Server;
        await ScalarAsync<string>("SELECT audit.create_partition('2020-01-15')::text");

        var apagadas = await ScalarAsync<int>("SELECT audit.drop_partitions_older_than(interval '12 months')");

        Assert.Equal(1, apagadas);
        Assert.False(await ScalarAsync<bool>("SELECT to_regclass('audit.change_log_2020_01') IS NOT NULL"));
        Assert.Equal(4L, await ScalarAsync<long>("""
            SELECT count(*) FROM pg_inherits WHERE inhparent = 'audit.change_log'::regclass
              AND inhrelid::regclass::text ~ '^audit\.change_log_\d{4}_\d{2}$'
            """));
    }
}
