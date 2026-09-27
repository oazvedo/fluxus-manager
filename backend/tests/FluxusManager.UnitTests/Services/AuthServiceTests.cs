using FluxusManager.Application.DTOs.AuthDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Services;
using FluxusManager.Application.Options;
using Microsoft.Extensions.Options;
using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Exceptions;
using FluxusManager.Domain.Interfaces;

namespace FluxusManager.UnitTests.Services;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Login_ComAdministrador_EmiteTenantRoleEPermissoes()
    {
        var user = new Usuario("Ana", "ana@fluxus.com", "hash");
        var company = new Empresa("Fluxus", null, "11222333000181");
        var link = new UsuarioEmpresa(user.Id, company.Id, "Administrador");
        var tokenIssuer = new FakeTokenIssuer();
        var service = CreateService(user, company, link, tokenIssuer);

        var response = await service.LoginAsync(new LoginRequest(" ANA@FLUXUS.COM ", "senha"));

        Assert.NotNull(response);
        Assert.Equal(company.Id, response.TenantId);
        Assert.Equal("Administrador", response.Role);
        Assert.Contains("empresas.editar", response.Permissions);
        Assert.Equal(user.Id, tokenIssuer.UserId);
        Assert.Equal(company.Id, tokenIssuer.TenantId);
    }

    [Fact]
    public async Task Login_ComPerfilDesconhecido_NaoConcedePermissoes()
    {
        var user = new Usuario("Ana", "ana@fluxus.com", "hash");
        var company = new Empresa("Fluxus", null, "11222333000181");
        var link = new UsuarioEmpresa(user.Id, company.Id, "Perfil livre ainda sem mapeamento");
        var tokenIssuer = new FakeTokenIssuer();
        var service = CreateService(user, company, link, tokenIssuer);

        var response = await service.LoginAsync(new LoginRequest(user.Email, "senha"));

        Assert.NotNull(response);
        Assert.Empty(response.Permissions);
        Assert.Empty(tokenIssuer.Permissions);
    }

    [Fact]
    public async Task Login_ComSenhaIncorreta_RetornaNulo()
    {
        var user = new Usuario("Ana", "ana@fluxus.com", "hash");
        var company = new Empresa("Fluxus", null, "11222333000181");
        var link = new UsuarioEmpresa(user.Id, company.Id, "Administrador");
        var service = CreateService(user, company, link, new FakeTokenIssuer());

        var response = await service.LoginAsync(new LoginRequest(user.Email, "incorreta"));

        Assert.Null(response);
    }

    [Fact]
    public async Task SwitchTenant_SemVinculoAtivo_RejeitaTroca()
    {
        var user = new Usuario("Ana", "ana@fluxus.com", "hash");
        var company = new Empresa("Fluxus", null, "11222333000181");
        var link = new UsuarioEmpresa(user.Id, company.Id, "Administrador");
        link.Inativar();
        var service = CreateService(user, company, link, new FakeTokenIssuer());

        await Assert.ThrowsAsync<NotFoundException>(() => service.SwitchTenantAsync(user.Id, company.Id));
    }

    [Fact]
    public async Task SwitchTenant_ComVinculoAtivo_EmiteTokenParaOTenantSelecionado()
    {
        var user = new Usuario("Ana", "ana@fluxus.com", "hash");
        var company = new Empresa("Fluxus", null, "11222333000181");
        var link = new UsuarioEmpresa(user.Id, company.Id, "Consulta");
        var tokenIssuer = new FakeTokenIssuer();
        var service = CreateService(user, company, link, tokenIssuer);

        var response = await service.SwitchTenantAsync(user.Id, company.Id);

        Assert.Equal(company.Id, response.TenantId);
        Assert.Equal("Consulta", response.Role);
        Assert.Equal(company.Id, tokenIssuer.TenantId);
        Assert.Contains("empresas.visualizar", response.Permissions);
        Assert.DoesNotContain("empresas.editar", response.Permissions);
    }

    private static AuthService CreateService(Usuario user, Empresa company, UsuarioEmpresa link, FakeTokenIssuer issuer)
        => new(new FakeUsuarioRepository(user), new FakeUsuarioEmpresaRepository(user, link), new FakeEmpresaRepository(company), new FakePasswordHasher(), issuer, new FakeRefreshRepository(), new FakeUnitOfWork(), TimeProvider.System, Options.Create(new RefreshTokenOptions()));

    private sealed class FakeRefreshRepository : IRefreshTokenRepository
    {
        public void Add(RefreshToken token) { }
        public Task<RefreshToken?> ObterPorHashAsync(string hash, CancellationToken cancellationToken = default) => Task.FromResult<RefreshToken?>(null);
        public Task BloquearFamiliaAsync(Guid familiaId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<RefreshToken>> ListarFamiliaAsync(Guid familiaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RefreshToken>>([]);
        public Task<int> LimparExpiradosAsync(DateTime agora, CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> CommitAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) => action(cancellationToken);
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default) => action(cancellationToken);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string senha) => "hash";
        public bool Verificar(string senha, string senhaHash) => senha == "senha" && senhaHash == "hash";
    }

    private sealed class FakeTokenIssuer : IJwtTokenIssuer
    {
        public Guid UserId { get; private set; }
        public Guid TenantId { get; private set; }
        public IReadOnlyCollection<string> Permissions { get; private set; } = [];

        public (string Token, int ExpiresIn) Create(Guid userId, string email, Guid tenantId, string role, IReadOnlyCollection<string> permissions)
        {
            UserId = userId;
            TenantId = tenantId;
            Permissions = permissions;
            return ("token", 900);
        }
    }

    private sealed class FakeUsuarioRepository(Usuario user) : IUsuarioRepository
    {
        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult<Usuario?>(email == user.Email ? user : null);
        public Task<Usuario?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Usuario?>(id == user.Id ? user : null);
        public Task<PagedResult<Usuario>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(new PagedResult<Usuario>([], page, pageSize, 0));
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(id == user.Id);
        public Task<bool> EmailEmUsoAsync(string email, Guid? ignorarId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public void Add(Usuario entity) { }
        public void Update(Usuario entity) { }
        public void Remove(Usuario entity) { }
    }

    private sealed class FakeUsuarioEmpresaRepository(Usuario user, UsuarioEmpresa link) : IUsuarioEmpresaRepository
    {
        public Task<UsuarioEmpresa?> ObterVinculoAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken = default)
            => Task.FromResult<UsuarioEmpresa?>(usuarioId == user.Id && empresaId == link.EmpresaId ? link : null);
        public Task<IReadOnlyList<UsuarioEmpresa>> ListarPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<UsuarioEmpresa>>(usuarioId == user.Id ? [link] : []);
        public Task<IReadOnlyList<UsuarioEmpresa>> ListarPorEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<UsuarioEmpresa>>([]);
        public Task<UsuarioEmpresa?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<UsuarioEmpresa?>(null);
        public Task<PagedResult<UsuarioEmpresa>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(new PagedResult<UsuarioEmpresa>([], page, pageSize, 0));
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public void Add(UsuarioEmpresa entity) { }
        public void Update(UsuarioEmpresa entity) { }
        public void Remove(UsuarioEmpresa entity) { }
    }

    private sealed class FakeEmpresaRepository(Empresa company) : IRepository<Empresa>
    {
        public Task<Empresa?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Empresa?>(id == company.Id ? company : null);
        public Task<PagedResult<Empresa>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(new PagedResult<Empresa>([], page, pageSize, 0));
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(id == company.Id);
        public void Add(Empresa entity) { }
        public void Update(Empresa entity) { }
        public void Remove(Empresa entity) { }
    }
}
