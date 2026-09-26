using System.Net;
using Microsoft.AspNetCore.Hosting;

namespace FluxusManager.IntegrationTests.Hosting;

public class PathBaseTests
{
    [Fact]
    public async Task ComPathBase_ApiRespondeSobOPrefixo()
    {
        using var factory = new ApiFactory();
        var client = factory.WithWebHostBuilder(b => b.UseSetting("PathBase", "/api")).CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/openapi/v1.json")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/swagger/index.html")).StatusCode);
    }

    [Fact]
    public async Task SemPathBase_ApiRespondeNaRaiz()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/openapi/v1.json")).StatusCode);
    }
}
