using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluxusManager.Application.Options;
using FluxusManager.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace FluxusManager.IntegrationTests.Auth;

public sealed class AuthSecurityTests
{
    [Fact]
    public async Task EndpointProtegido_SemToken_Retorna401()
    {
        using var factory = new ApiFactory("Development", useTestAuthentication: false);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/empresas");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EndpointProtegido_ComTokenMalformado_Retorna401()
    {
        using var factory = new ApiFactory("Development", useTestAuthentication: false);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token-invalido");

        var response = await client.GetAsync("/empresas");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EndpointProtegido_SemPermissao_Retorna403()
    {
        using var factory = new ApiFactory("Development", useTestAuthentication: false);
        using var client = factory.CreateClient();
        var tokenIssuer = new JwtTokenIssuer(Options.Create(new JwtOptions
        {
            Issuer = "FluxusManager",
            Audience = "FluxusManager",
            SigningKey = "integration-tests-only-signing-key-32-bytes-minimum"
        }));
        var (token, _) = tokenIssuer.Create(Guid.NewGuid(), "ana@fluxus.com", Guid.NewGuid(), "Consulta", []);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/empresas");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_EOpenApi_SaoAcessiveisSemToken()
    {
        using var factory = new ApiFactory("Development", useTestAuthentication: false);
        using var client = factory.CreateClient();

        var login = await client.PostAsync("/auth/login", new StringContent("{\"email\":\"nao@existe.com\",\"senha\":\"senha-invalida\"}", System.Text.Encoding.UTF8, "application/json"));
        var openApi = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, openApi.StatusCode);
        var document = JsonNode.Parse(await openApi.Content.ReadAsStringAsync())!;
        Assert.NotNull(document["components"]?["securitySchemes"]?["Bearer"]);
        foreach (var path in new[] { "/auth/login", "/auth/refresh", "/auth/logout" })
        {
            var security = document["paths"]?[path]?["post"]?["security"];
            Assert.NotNull(security);
            Assert.Empty(security.AsArray());
        }
    }
}
