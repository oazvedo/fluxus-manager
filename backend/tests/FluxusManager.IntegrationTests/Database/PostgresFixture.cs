using Npgsql;
using Testcontainers.PostgreSql;

namespace FluxusManager.IntegrationTests.Database;

/// <summary>
/// Servidor PostgreSQL dos testes de integração. Por padrão sobe um container (Testcontainers);
/// com a variável <c>FLUXUS_TEST_POSTGRES</c> usa um servidor já existente (ex.: o do docker compose).
/// Cada teste pede o seu banco em <see cref="CreateDatabaseAsync"/>, então os testes não se enxergam.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public const string ServerVariable = "FLUXUS_TEST_POSTGRES";

    private readonly List<string> _databases = [];
    private PostgreSqlContainer? _container;
    private string _serverConnectionString = string.Empty;

    public async Task InitializeAsync()
    {
        var server = Environment.GetEnvironmentVariable(ServerVariable);
        if (!string.IsNullOrWhiteSpace(server))
        {
            _serverConnectionString = server;
            return;
        }

        _container = new PostgreSqlBuilder("postgres:18-alpine").Build();
        await _container.StartAsync();
        _serverConnectionString = _container.GetConnectionString();
    }

    /// <summary>Cria um banco vazio e devolve a connection string dele.</summary>
    public async Task<string> CreateDatabaseAsync()
    {
        var name = $"fluxus_test_{Guid.NewGuid():N}";

        await ExecuteOnServerAsync($"CREATE DATABASE \"{name}\"");
        lock (_databases)
            _databases.Add(name);

        return new NpgsqlConnectionStringBuilder(_serverConnectionString) { Database = name }.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
            return;
        }

        // Servidor externo: remove os bancos criados para não acumular lixo entre execuções.
        NpgsqlConnection.ClearAllPools();
        foreach (var name in _databases)
            await ExecuteOnServerAsync($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)");
    }

    private async Task ExecuteOnServerAsync(string sql)
    {
        var builder = new NpgsqlConnectionStringBuilder(_serverConnectionString) { Database = "postgres", Pooling = false };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

/// <summary>Testes que usam o <see cref="PostgresFixture"/> compartilham um único servidor.</summary>
[CollectionDefinition(Name)]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "PostgreSQL";
}
