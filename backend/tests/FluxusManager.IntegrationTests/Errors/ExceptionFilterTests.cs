using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace FluxusManager.IntegrationTests.Errors;

public class ExceptionFilterTests
{
    [Theory]
    [InlineData("not-found", HttpStatusCode.NotFound, "Empresa '42' não encontrado(a).")]
    [InlineData("conflict", HttpStatusCode.Conflict, "CNPJ já cadastrado.")]
    [InlineData("business-rule", HttpStatusCode.UnprocessableEntity, "Empresa inativa não pode ter filiais.")]
    public async Task ErrosDeNegocio_ViramProblemDetailsComAMensagem(string rota, HttpStatusCode status, string detail)
    {
        using var factory = new ApiFactory();
        var response = await factory.CreateClient().GetAsync($"/test/erros/{rota}");

        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)status, problem.Status);
        Assert.Equal(detail, problem.Detail);
        Assert.Equal($"/test/erros/{rota}", problem.Instance);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
    }

    [Fact]
    public async Task ErroInesperado_EmProducao_NaoExpoeDetalhes()
    {
        using var factory = new ApiFactory("Production");
        var response = await factory.CreateClient().GetAsync("/test/erros/unexpected");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("detalhe interno sensível", body);
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.Contains("traceId", body);
    }

    [Fact]
    public async Task ErroInesperado_EmDevelopment_MostraAMensagem()
    {
        using var factory = new ApiFactory("Development");
        var response = await factory.CreateClient().GetAsync("/test/erros/unexpected");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("detalhe interno sensível", problem?.Detail);
    }

    [Fact]
    public async Task RotaInexistente_RetornaProblemDetails()
    {
        using var factory = new ApiFactory();
        var response = await factory.CreateClient().GetAsync("/nao-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
