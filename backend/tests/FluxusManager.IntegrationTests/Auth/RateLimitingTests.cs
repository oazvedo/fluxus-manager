using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluxusManager.Application.DTOs.AuthDtos;
using Microsoft.AspNetCore.Hosting;

namespace FluxusManager.IntegrationTests.Auth;

public sealed class RateLimitingTests
{
    private static HttpRequestMessage Login(string? ip = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/login")
        {
            Content = JsonContent.Create(new LoginRequest("nao@existe.com", "senha-errada1"))
        };
        if (ip is not null)
            request.Headers.Add("X-Forwarded-For", ip);
        return request;
    }

    [Fact]
    public async Task RotaPublica_AcimaDoLimite_Retorna429ComRetryAfter_SemAfetarOutrasRotas()
    {
        using var factory = new LimiteFactory(confiarNoProxy: false);
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(Login())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(Login())).StatusCode);
        using var bloqueada = await client.SendAsync(Login());

        Assert.Equal(HttpStatusCode.TooManyRequests, bloqueada.StatusCode);
        Assert.True(int.Parse(bloqueada.Headers.GetValues("Retry-After").Single()) > 0);
        var problema = JsonNode.Parse(await bloqueada.Content.ReadAsStringAsync())!;
        Assert.Equal(429, problema["status"]!.GetValue<int>());
        Assert.Contains("Aguarde", problema["detail"]!.GetValue<string>());
        // Variar maiúsculas ou barra final não abre uma contagem nova.
        using var variacao = new HttpRequestMessage(HttpMethod.Post, "/AUTH/Login/") { Content = Login().Content };
        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.SendAsync(variacao)).StatusCode);
        // Refresh e logout ([DisableRateLimiting]) e o health check nunca recebem 429.
        for (var i = 0; i < 3; i++)
        {
            using var refresh = await client.PostAsJsonAsync("/auth/refresh", new RefreshRequest(new string('A', 64)));
            Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
            using var logout = await client.PostAsJsonAsync("/auth/logout", new LogoutRequest(new string('A', 64)));
            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, (await client.GetAsync("/health")).StatusCode);
        }
    }

    [Fact]
    public async Task AtrasDoProxy_OLimiteEPorIpDoCliente()
    {
        using var factory = new LimiteFactory(confiarNoProxy: true);
        using var client = factory.CreateClient();

        await client.SendAsync(Login("203.0.113.10"));
        await client.SendAsync(Login("203.0.113.10"));

        Assert.Equal(HttpStatusCode.TooManyRequests, (await client.SendAsync(Login("203.0.113.10"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(Login("198.51.100.7"))).StatusCode);
    }

    private sealed class LimiteFactory(bool confiarNoProxy) : ApiFactory("Development", useTestAuthentication: false)
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("RateLimiting:RequisicoesPorMinuto", "2");
            builder.UseSetting("Proxy:Confiavel", confiarNoProxy.ToString());
        }
    }
}
