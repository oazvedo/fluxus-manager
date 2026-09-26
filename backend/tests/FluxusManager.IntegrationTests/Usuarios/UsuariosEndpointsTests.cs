using System.Net;
using System.Net.Http.Json;
using FluxusManager.Application.DTOs.UsuariosDtos;
using FluxusManager.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace FluxusManager.IntegrationTests.Usuarios;

public sealed class UsuariosEndpointsTests : IDisposable
{
    private readonly ApiComBancoFactory _factory = new();
    private readonly HttpClient _client;

    public UsuariosEndpointsTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    private async Task<UsuarioResponse> CriarAsync(string email = "ana@fluxus.com")
    {
        var response = await _client.PostAsJsonAsync("/usuarios", new CriarUsuarioRequest("Ana", email, "segredo123"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UsuarioResponse>())!;
    }

    [Fact]
    public async Task Post_Cria_Retorna201ComLocation_ESemSenha()
    {
        var response = await _client.PostAsJsonAsync("/usuarios",
            new CriarUsuarioRequest("Ana", "Ana@Fluxus.com", "segredo123"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("senha", body, StringComparison.OrdinalIgnoreCase);

        var usuario = (await response.Content.ReadFromJsonAsync<UsuarioResponse>())!;
        Assert.Equal("ana@fluxus.com", usuario.Email);
        Assert.EndsWith($"/usuarios/{usuario.Id}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Post_ComEmailDuplicado_Retorna409()
    {
        await CriarAsync("ana@fluxus.com");

        var response = await _client.PostAsJsonAsync("/usuarios",
            new CriarUsuarioRequest("Outra", "ANA@fluxus.com", "segredo123"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Post_ComDadosInvalidos_Retorna400ComErrosPorCampo()
    {
        var response = await _client.PostAsJsonAsync("/usuarios", new CriarUsuarioRequest("", "x", "curta"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
        Assert.Equal(["email", "nome", "senha"], problem.Errors.Keys.Order());
    }

    [Fact]
    public async Task Get_Inexistente_Retorna404()
    {
        var response = await _client.GetAsync($"/usuarios/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_Atualiza()
    {
        var criado = await CriarAsync();

        var response = await _client.PutAsJsonAsync($"/usuarios/{criado.Id}",
            new AtualizarUsuarioRequest("Ana S.", "ana@fluxus.com"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Ana S.", (await response.Content.ReadFromJsonAsync<UsuarioResponse>())!.Nome);
    }

    [Fact]
    public async Task Patch_Inativar_Retorna204_EOUsuarioFicaInativo()
    {
        var criado = await CriarAsync();

        var response = await _client.PatchAsync($"/usuarios/{criado.Id}/inativar", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var usuario = await _client.GetFromJsonAsync<UsuarioResponse>($"/usuarios/{criado.Id}");
        Assert.False(usuario!.Ativo);
    }

    [Fact]
    public async Task Get_Lista_Paginada()
    {
        await CriarAsync("a@fluxus.com");
        await CriarAsync("b@fluxus.com");
        await CriarAsync("c@fluxus.com");

        var pagina = await _client.GetFromJsonAsync<PagedResult<UsuarioResponse>>("/usuarios?page=2&pageSize=2");

        Assert.Equal(3, pagina!.TotalCount);
        Assert.Single(pagina.Items);
    }

    [Fact]
    public async Task Get_Lista_ComPageSizeInvalido_Retorna400EmPortugues()
    {
        var response = await _client.GetAsync("/usuarios?pageSize=500");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
        Assert.Equal("Dados inválidos", problem.Title);
        Assert.Equal("O tamanho da página deve estar entre 1 e 100.", problem.Errors["pageSize"][0]);
    }
}
