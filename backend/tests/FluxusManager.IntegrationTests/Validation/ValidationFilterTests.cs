using System.Net;
using System.Net.Http.Json;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FluxusManager.IntegrationTests.Validation;

public class ValidationFilterTests
{
    private static HttpClient CreateClient()
    {
        var factory = new ApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddValidatorsFromAssemblyContaining<CadastroTestDtoValidator>()));
        return factory.CreateClient();
    }

    [Fact]
    public async Task DadosValidos_ExecutaAAction()
    {
        var response = await CreateClient().PostAsJsonAsync("/test/validacao",
            new CadastroTestDto("Fluxus", "contato@fluxus.com", new EnderecoTestDto("01001000")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DadosInvalidos_Retorna400ComOsErrosPorCampo_ESemExecutarAAction()
    {
        var chamadasAntes = ValidacaoTestController.Chamadas;

        var response = await CreateClient().PostAsJsonAsync("/test/validacao",
            new CadastroTestDto("", "invalido", new EnderecoTestDto("123")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Dados inválidos", problem.Title);
        Assert.Equal(["email", "endereco.cep", "nome"], problem.Errors.Keys.Order());
        Assert.Equal("'Nome' deve ser informado.", problem.Errors["nome"][0]); // mensagens em pt-BR
        Assert.Equal(chamadasAntes, ValidacaoTestController.Chamadas);
    }
}
