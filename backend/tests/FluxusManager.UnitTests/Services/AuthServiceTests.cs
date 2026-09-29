using FluxusManager.Application.DTOs.AuthDtos;
using FluxusManager.Application.Email;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Services;
using FluxusManager.Application.Options;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
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
        var link = Vinculo(user, company, "Administrador", ["empresas.visualizar", "empresas.editar"]);
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
        var link = Vinculo(user, company, "Perfil livre ainda sem mapeamento", []);
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
        var link = Vinculo(user, company, "Administrador", ["empresas.editar"]);
        var service = CreateService(user, company, link, new FakeTokenIssuer());

        var response = await service.LoginAsync(new LoginRequest(user.Email, "incorreta"));

        Assert.Null(response);
    }

    [Fact]
    public async Task SwitchTenant_SemVinculoAtivo_RejeitaTroca()
    {
        var user = new Usuario("Ana", "ana@fluxus.com", "hash");
        var company = new Empresa("Fluxus", null, "11222333000181");
        var link = Vinculo(user, company, "Administrador", ["empresas.editar"]);
        link.Inativar();
        var service = CreateService(user, company, link, new FakeTokenIssuer());

        await Assert.ThrowsAsync<NotFoundException>(() => service.SwitchTenantAsync(user.Id, company.Id));
    }

    [Fact]
    public async Task SwitchTenant_ComVinculoAtivo_EmiteTokenParaOTenantSelecionado()
    {
        var user = new Usuario("Ana", "ana@fluxus.com", "hash");
        var company = new Empresa("Fluxus", null, "11222333000181");
        var link = Vinculo(user, company, "Consulta", ["empresas.visualizar"]);
        var tokenIssuer = new FakeTokenIssuer();
        var service = CreateService(user, company, link, tokenIssuer);

        var response = await service.SwitchTenantAsync(user.Id, company.Id);

        Assert.Equal(company.Id, response.TenantId);
        Assert.Equal("Consulta", response.Role);
        Assert.Equal(company.Id, tokenIssuer.TenantId);
        Assert.Contains("empresas.visualizar", response.Permissions);
        Assert.DoesNotContain("empresas.editar", response.Permissions);
    }

    [Fact]
    public async Task Login_ComSenhaErrada_RegistraAFalhaComOsLimitesConfigurados()
    {
        var user = new Usuario("Ana", "ana@fluxus.com", "hash");
        var company = new Empresa("Fluxus", null, "11222333000181");
        var usuarios = new FakeUsuarioRepository(user);
        var service = CreateService(user, company, Vinculo(user, company, "Consulta", []), new FakeTokenIssuer(),
            usuarios: usuarios, login: new LoginOptions { MaxTentativas = 3, BloqueioMinutos = 10 });

        Assert.Null(await service.LoginAsync(new LoginRequest(user.Email, "errada")));

        var falha = Assert.Single(usuarios.Falhas);
        Assert.Equal((user.Id, 3, TimeSpan.FromMinutes(10)), falha);
    }

    [Fact]
    public async Task Login_ComEmailInexistente_AindaVerificaUmaSenha_ESemRegistrarFalha()
    {
        var user = new Usuario("Ana", "ana@fluxus.com", "hash");
        var company = new Empresa("Fluxus", null, "11222333000181");
        var hasher = new FakePasswordHasher();
        var usuarios = new FakeUsuarioRepository(user);
        var service = CreateService(user, company, Vinculo(user, company, "Consulta", []), new FakeTokenIssuer(),
            hasher: hasher, usuarios: usuarios);

        Assert.Null(await service.LoginAsync(new LoginRequest("nao@existe.com", "senha")));

        // Mesmo custo de hash do caminho normal: o tempo de resposta não revela se o e-mail existe.
        Assert.Equal(1, hasher.Verificacoes);
        Assert.Empty(usuarios.Falhas);
    }

    private static AuthService CreateService(Usuario user, Empresa company, UsuarioEmpresa link, FakeTokenIssuer issuer,
        FakePasswordHasher? hasher = null, FakeUsuarioRepository? usuarios = null, LoginOptions? login = null)
        => new(usuarios ?? new FakeUsuarioRepository(user), new FakeUsuarioEmpresaRepository(user, link), new FakeEmpresaRepository(company),
            hasher ?? new FakePasswordHasher(), issuer, new FakeRefreshRepository(), new FakeUnitOfWork(), TimeProvider.System,
            Options.Create(new RefreshTokenOptions()), Options.Create(login ?? new LoginOptions()),
            new FakePasswordResetTokenRepository(), new FakeEmailSender(), Options.Create(new FrontendOptions { Url = "http://localhost:5173" }),
            Options.Create(new PasswordResetOptions()), new FakeAdministradorPlataformaRepository(), NullLogger<AuthService>.Instance);

    private sealed class FakePasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        public Task<PasswordResetToken?> ObterParaUsoAsync(string hash, DateTime agora, CancellationToken cancellationToken = default)
            => Task.FromResult<PasswordResetToken?>(null);
        public void Add(PasswordResetToken token) { }
    }

    private sealed class FakeAdministradorPlataformaRepository : IAdministradorPlataformaRepository
    {
        public Task<bool> EhAdministradorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<AdministradorPlataforma?> ObterPorUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult<AdministradorPlataforma?>(null);
        public Task<AdministradorPlataforma?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PagedResult<AdministradorPlataforma>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Add(AdministradorPlataforma entity) => throw new NotSupportedException();
        public void Update(AdministradorPlataforma entity) => throw new NotSupportedException();
        public void Remove(AdministradorPlataforma entity) => throw new NotSupportedException();
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public Task EnviarAsync(MensagemEmail mensagem, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }


    private static UsuarioEmpresa Vinculo(Usuario user, Empresa company, string nomePerfil, IReadOnlyCollection<string> codigos)
    {
        var perfil = Perfil.CriarPadrao(company.Id, nomePerfil, "", codigos);
        var vinculo = new UsuarioEmpresa(user.Id, company.Id, perfil.Id);
        vinculo.AssociarPerfil(perfil);
        return vinculo;
    }

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
        public int Verificacoes { get; private set; }
        public string Hash(string senha) => "hash";

        public bool Verificar(string senha, string senhaHash)
        {
            Verificacoes++;
            return senha == "senha" && senhaHash == "hash";
        }

        public void VerificarSemUsuario(string senha) => Verificacoes++;
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
        public List<(Guid Id, int MaxTentativas, TimeSpan Bloqueio)> Falhas { get; } = [];

        public Task RegistrarFalhaLoginAsync(Guid id, DateTime agora, int maxTentativas, TimeSpan bloqueio, CancellationToken cancellationToken = default)
        {
            Falhas.Add((id, maxTentativas, bloqueio));
            return Task.CompletedTask;
        }

        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult<Usuario?>(email == user.Email ? user : null);
        public Task RevogarRefreshTokensAsync(Guid usuarioId, DateTime agora, CancellationToken cancellationToken = default) => Task.CompletedTask;
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
        public Task<bool> ExisteAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken = default)
            => Task.FromResult(usuarioId == user.Id && empresaId == link.EmpresaId);

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
