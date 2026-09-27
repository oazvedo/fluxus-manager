using System.Net;
using System.Net.Http.Json;
using FluxusManager.Application.DTOs.EmpresasDtos;
using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Exceptions;
using FluxusManager.Domain.Interfaces;
using FluxusManager.IntegrationTests.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FluxusManager.IntegrationTests.Empresas;

[Collection(PostgresCollection.Name)]
public sealed class EmpresasEndpointsTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ApiComBancoFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new ApiComBancoFactory(await postgres.CreateDatabaseAsync());
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();

    private async Task<EmpresaResponse> CriarAsync(string cnpj = "11222333000181")
    {
        var response = await _client.PostAsJsonAsync("/empresas", new CriarEmpresaRequest("Fluxus Ltda", "Fluxus", cnpj));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EmpresaResponse>())!;
    }

    [Fact]
    public async Task Post_Cria_Retorna201ComLocation_ECnpjNormalizado()
    {
        var response = await _client.PostAsJsonAsync("/empresas",
            new CriarEmpresaRequest("Fluxus Ltda", null, "11.222.333/0001-81"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var empresa = (await response.Content.ReadFromJsonAsync<EmpresaResponse>())!;
        Assert.Equal("11222333000181", empresa.Cnpj);
        Assert.EndsWith($"/empresas/{empresa.Id}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Post_ComCnpjDuplicado_Retorna409()
    {
        await CriarAsync("11222333000181");

        var response = await _client.PostAsJsonAsync("/empresas", new CriarEmpresaRequest("Outra", null, "11.222.333/0001-81"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Post_ComDadosInvalidos_Retorna400ComErrosPorCampo()
    {
        var response = await _client.PostAsJsonAsync("/empresas", new CriarEmpresaRequest("", null, "11222333000182"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
        Assert.Equal(["cnpj", "razaoSocial"], problem.Errors.Keys.Order());
        Assert.Equal("Informe um CNPJ válido, com 14 caracteres. Pode ser com ou sem pontuação.", problem.Errors["cnpj"][0]);
    }

    [Fact]
    public async Task Post_ComCnpjAlfanumerico_Cria()
    {
        var empresa = await CriarAsync("12.ABC.345/01DE-35");

        Assert.Equal("12ABC34501DE35", empresa.Cnpj);
    }

    [Fact]
    public async Task Get_Inexistente_Retorna404()
    {
        var response = await _client.GetAsync($"/empresas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_Atualiza()
    {
        var criada = await CriarAsync();

        var response = await _client.PutAsJsonAsync($"/empresas/{criada.Id}", new AtualizarEmpresaRequest("Fluxus S.A.", null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var empresa = (await response.Content.ReadFromJsonAsync<EmpresaResponse>())!;
        Assert.Equal("Fluxus S.A.", empresa.RazaoSocial);
        Assert.Null(empresa.NomeFantasia);
    }

    [Fact]
    public async Task Patch_Inativar_Retorna204_EAEmpresaFicaInativa()
    {
        var criada = await CriarAsync();

        var response = await _client.PatchAsync($"/empresas/{criada.Id}/inativar", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var empresa = await _client.GetFromJsonAsync<EmpresaResponse>($"/empresas/{criada.Id}");
        Assert.False(empresa!.Ativo);
    }

    [Fact]
    public async Task Get_Lista_Paginada()
    {
        await CriarAsync("11222333000181");
        await CriarAsync("11444777000161");
        await CriarAsync("12ABC34501DE35");

        var pagina = await _client.GetFromJsonAsync<PagedResult<EmpresaResponse>>("/empresas?page=2&pageSize=2");

        Assert.Equal(3, pagina!.TotalCount);
        Assert.Single(pagina.Items);
    }

    [Fact]
    public async Task PostsSimultaneos_ComOMesmoCnpj_UmCria_OsOutrosRecebem409()
    {
        var respostas = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            _client.PostAsJsonAsync("/empresas", new CriarEmpresaRequest("Fluxus Ltda", null, "11222333000181"))));

        Assert.Single(respostas, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(respostas.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
    }

    [Fact]
    public async Task CommitQueViolaIndiceUnico_ViraDuplicateKeyException()
    {
        await CriarAsync("11222333000181");

        await using var scope = _factory.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IEmpresaRepository>().Add(new Empresa("Outra", null, "11222333000181"));

        await Assert.ThrowsAsync<DuplicateKeyException>(() =>
            scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync());
    }

    [Fact]
    public async Task CnpjDeEmpresaExcluida_PodeSerCadastradoDeNovo()
    {
        var criada = await CriarAsync();
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var empresas = scope.ServiceProvider.GetRequiredService<IEmpresaRepository>();
            empresas.Remove((await empresas.GetByIdAsync(criada.Id))!);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync();
        }

        var response = await _client.PostAsJsonAsync("/empresas", new CriarEmpresaRequest("Nova", null, criada.Cnpj));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
