using FluxusManager.Application.DTOs.UsuariosDtos;
using FluxusManager.Application.Services;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Exceptions;
using FluxusManager.Infrastructure.Database;
using FluxusManager.Infrastructure.Repositories;
using FluxusManager.Infrastructure.Security;
using FluxusManager.UnitTests.MultiTenant;
using Microsoft.EntityFrameworkCore;

namespace FluxusManager.UnitTests.Usuarios;

public sealed class UsuarioServiceTests : IDisposable
{
    // Banco SQLite em memória; usuário é global, então não precisa de tenant.
    private readonly TenantTestDatabase _database = new();
    private readonly PasswordHasher _hasher = new();

    public void Dispose() => _database.Dispose();

    private (UsuarioService service, AppDbContext context) CriarService()
    {
        var context = _database.CreateContext(tenantId: null);
        var service = new UsuarioService(new UsuarioRepository(context), new UnitOfWork(context), _hasher);
        return (service, context);
    }

    private async Task<UsuarioResponse> CriarUsuarioAsync(string email = "ana@fluxus.com")
    {
        var (service, context) = CriarService();
        await using (context)
            return await service.CriarAsync(new CriarUsuarioRequest("Ana", email, "segredo123"));
    }

    [Fact]
    public async Task Criar_NormalizaNomeEEmail_ENaoRetornaASenha()
    {
        var (service, context) = CriarService();
        await using var _ = context;

        var usuario = await service.CriarAsync(new CriarUsuarioRequest("  Ana Silva ", "  Ana@Fluxus.COM ", "segredo123"));

        Assert.Equal("Ana Silva", usuario.Nome);
        Assert.Equal("ana@fluxus.com", usuario.Email);
        Assert.True(usuario.Ativo);
        Assert.DoesNotContain(typeof(UsuarioResponse).GetProperties(), p => p.Name.Contains("Senha"));
    }

    [Fact]
    public async Task Criar_SalvaOHashDaSenha_NaoASenha()
    {
        var criado = await CriarUsuarioAsync();

        await using var context = _database.CreateContext(tenantId: null);
        var salvo = await context.Set<Usuario>().SingleAsync(u => u.Id == criado.Id);

        Assert.NotEqual("segredo123", salvo.SenhaHash);
        Assert.True(_hasher.Verificar("segredo123", salvo.SenhaHash));
    }

    [Fact]
    public async Task Criar_ComEmailJaUsado_EscritoDiferente_LancaConflict()
    {
        await CriarUsuarioAsync("ana@fluxus.com");

        var (service, context) = CriarService();
        await using var _ = context;

        await Assert.ThrowsAsync<ConflictException>(
            () => service.CriarAsync(new CriarUsuarioRequest("Outra", " ANA@fluxus.com", "segredo123")));
    }

    [Fact]
    public async Task Obter_Inexistente_LancaNotFound()
    {
        var (service, context) = CriarService();
        await using var _ = context;

        await Assert.ThrowsAsync<NotFoundException>(() => service.ObterAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Atualizar_MantendoOProprioEmail_Funciona()
    {
        var criado = await CriarUsuarioAsync("ana@fluxus.com");

        var (service, context) = CriarService();
        await using var _ = context;
        var atualizado = await service.AtualizarAsync(criado.Id, new AtualizarUsuarioRequest("Ana S.", "ana@fluxus.com"));

        Assert.Equal("Ana S.", atualizado.Nome);
        Assert.NotNull(atualizado.AtualizadoEm);
    }

    [Fact]
    public async Task Atualizar_ParaEmailDeOutroUsuario_LancaConflict()
    {
        await CriarUsuarioAsync("ana@fluxus.com");
        var bruno = await CriarUsuarioAsync("bruno@fluxus.com");

        var (service, context) = CriarService();
        await using var _ = context;

        await Assert.ThrowsAsync<ConflictException>(
            () => service.AtualizarAsync(bruno.Id, new AtualizarUsuarioRequest("Bruno", "ana@fluxus.com")));
    }

    [Fact]
    public async Task InativarEAtivar_AlteramOStatus()
    {
        var criado = await CriarUsuarioAsync();

        var (service, context) = CriarService();
        await using (context)
            await service.InativarAsync(criado.Id);

        (service, context) = CriarService();
        await using (context)
            Assert.False((await service.ObterAsync(criado.Id)).Ativo);

        (service, context) = CriarService();
        await using (context)
        {
            await service.AtivarAsync(criado.Id);
            Assert.True((await service.ObterAsync(criado.Id)).Ativo);
        }
    }

    [Fact]
    public async Task Listar_RetornaPaginado()
    {
        await CriarUsuarioAsync("a@fluxus.com");
        await CriarUsuarioAsync("b@fluxus.com");
        await CriarUsuarioAsync("c@fluxus.com");

        var (service, context) = CriarService();
        await using var _ = context;
        var pagina = await service.ListarAsync(page: 1, pageSize: 2);

        Assert.Equal(3, pagina.TotalCount);
        Assert.Equal(2, pagina.Items.Count);
        Assert.True(pagina.HasNextPage);
    }
}
