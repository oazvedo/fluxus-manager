using System.Net;
using System.Net.Http.Json;
using FluxusManager.API.Logging;
using Microsoft.AspNetCore.Mvc;
using Serilog.Events;

namespace FluxusManager.IntegrationTests.Logging;

public sealed class LoggingTests : IDisposable
{
    private const string RequestTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} em {Elapsed:0.0} ms";

    private readonly LogsFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static string? Property(LogEvent logEvent, string name)
        => logEvent.Properties.TryGetValue(name, out var value) && value is ScalarValue { Value: var scalar }
            ? scalar?.ToString()
            : null;

    private static string CorrelationId(HttpResponseMessage response)
        => Assert.Single(response.Headers.GetValues(CorrelationIdMiddleware.HeaderName));

    [Fact]
    public async Task SemHeader_GeraCorrelationId_EDevolveNaResposta()
    {
        var response = await _factory.CreateClient().GetAsync("/test/logs");

        Assert.Matches("^[0-9a-f]{32}$", CorrelationId(response));
    }

    [Fact]
    public async Task ComHeaderValido_ReaproveitaOCorrelationId()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, "front-123");

        var response = await client.GetAsync("/test/logs");

        Assert.Equal("front-123", CorrelationId(response));
    }

    [Theory]
    [InlineData("com espaço")]
    [InlineData("muito-longo-muito-longo-muito-longo-muito-longo-muito-longo-muito-longo")]
    public async Task ComHeaderInvalido_GeraOutroCorrelationId(string recebido)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, recebido);

        var response = await client.GetAsync("/test/logs");

        Assert.Matches("^[0-9a-f]{32}$", CorrelationId(response));
    }

    [Fact]
    public async Task LogsDaRequisicao_TemCorrelationIdTenantEUsuario()
    {
        var tenant = Guid.CreateVersion7().ToString();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, "abc-1");
        client.DefaultRequestHeaders.Add(LogsFactory.TenantHeader, tenant);
        client.DefaultRequestHeaders.Add(LogsFactory.UserHeader, "usuario-7");

        await client.GetAsync("/test/logs");

        foreach (var logEvent in new[] { _factory.Logs.Single(LogsTestController.Message), _factory.Logs.Single(RequestTemplate) })
        {
            Assert.Equal("abc-1", Property(logEvent, CorrelationIdMiddleware.LogProperty));
            Assert.Equal(tenant, Property(logEvent, UserLogContextMiddleware.TenantProperty));
            Assert.Equal("usuario-7", Property(logEvent, UserLogContextMiddleware.UserProperty));
        }
    }

    [Fact]
    public async Task LogDaRequisicao_TemMetodoCaminhoEStatus_SemQueryString()
    {
        await _factory.CreateClient().GetAsync("/test/logs?token=segredo");

        var logEvent = _factory.Logs.Single(RequestTemplate);
        Assert.Equal("GET", Property(logEvent, "RequestMethod"));
        Assert.Equal("/test/logs", Property(logEvent, "RequestPath"));
        Assert.Equal("204", Property(logEvent, "StatusCode"));
        Assert.DoesNotContain("segredo", logEvent.RenderMessage());
    }

    [Fact]
    public async Task ProblemDetails_TrazOCorrelationId()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, "erro-42");

        var response = await client.GetAsync("/test/erros/not-found");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("erro-42", problem?.Extensions["correlationId"]?.ToString());
    }

    [Fact]
    public async Task ErroInesperado_EmProducao_InformaOCodigoParaOSuporte()
    {
        using var factory = new LogsFactory("Production");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, "erro-500");

        var response = await client.GetAsync("/test/erros/unexpected");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(
            "Não foi possível concluir a operação. Tente novamente; se o erro continuar, informe ao suporte o código erro-500.",
            problem?.Detail);
        Assert.Equal(LogEventLevel.Error, factory.Logs.Single(RequestTemplate).Level);
    }
}
