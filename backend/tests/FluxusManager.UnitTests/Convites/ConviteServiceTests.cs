using System.Text.RegularExpressions;
using FluxusManager.Application.DTOs.ConvitesDtos;
using FluxusManager.Application.Email;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Options;
using FluxusManager.Application.Security;
using FluxusManager.Application.Services;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Exceptions;
using FluxusManager.Infrastructure.Database;
using FluxusManager.Infrastructure.Repositories;
using FluxusManager.Infrastructure.Security;
using FluxusManager.UnitTests.MultiTenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FluxusManager.UnitTests.Convites;

public sealed partial class ConviteServiceTests : IDisposable
{
    private readonly TenantTestDatabase _database = new();
    private readonly PasswordHasher _hasher = new();
    private readonly EmailsEnviados _emails = new();
    private readonly Relogio _clock = new();
    private readonly Guid _empresaId;
    private readonly Guid _perfilId;
    private readonly Guid _outroPerfilId;

    public ConviteServiceTests()
    {
        var empresa = new Empresa("Acme Ltda", "Acme", "11222333000181");
        var outra = new Empresa("Outra Ltda", null, "11444777000161");
        var perfil = Perfil.CriarPadrao(empresa.Id, "Consulta", "", PermissionCatalog.ReadOnly);
        var outroPerfil = Perfil.CriarPadrao(outra.Id, "Consulta", "", PermissionCatalog.ReadOnly);
        using var context = _database.CreateContext(tenantId: null);
        context.AddRange(empresa, outra, perfil, outroPerfil, new Usuario("Bia", "bia@acme.com", _hasher.Hash("senhaAntiga1")));
        context.SaveChanges();
        (_empresaId, _perfilId, _outroPerfilId) = (empresa.Id, perfil.Id, outroPerfil.Id);
    }

    public void Dispose() => _database.Dispose();

    private (ConviteService Service, AppDbContext Context) CriarService(bool comTenant = true)
    {
        var tenant = new TenantContext();
        if (comTenant)
            tenant.SetTenant(_empresaId);
        var context = _database.CreateContext(tenant);
        var audit = new AuditContext(tenant);
        var service = new ConviteService(
            new ConviteRepository(context), new PerfilRepository(context), new EmpresaRepository(context),
            new UsuarioRepository(context), new UsuarioEmpresaRepository(context), _hasher, _emails, tenant, audit,
            new UnitOfWork(context, audit), _clock, Options.Create(new ConviteOptions { ValidadeHoras = 48 }),
            Options.Create(new FrontendOptions { Url = "https://app.fluxus.local/" }));
        return (service, context);
    }

    private async Task<(ConviteResponse Convite, string Token)> ConvidarAsync(string email = "ana@acme.com")
    {
        var (service, context) = CriarService();
        await using var _ = context;
        var convite = await service.CriarAsync(new CriarConviteRequest(email, _perfilId));
        return (convite, UltimoToken());
    }

    private string UltimoToken() => TokenNoLink().Match(_emails.Mensagens[^1].Texto).Groups[1].Value;

    private async Task<T> ComServicePublicoAsync<T>(Func<ConviteService, Task<T>> acao)
    {
        var (service, context) = CriarService(comTenant: false);
        await using var _ = context;
        return await acao(service);
    }

    private async Task AceitarAsync(string token, string? nome = "Ana", string? senha = "segredo123")
    {
        var (service, context) = CriarService(comTenant: false);
        await using var _ = context;
        await service.AceitarAsync(new AceitarConviteRequest(token, nome, senha));
    }

    [Fact]
    public async Task Criar_NormalizaEmail_EnviaLinkEGuardaSoOHash()
    {
        var (convite, token) = await ConvidarAsync("  Ana@ACME.com ");

        Assert.Equal("ana@acme.com", convite.Email);
        Assert.Equal("Pendente", convite.Status);
        Assert.Equal("Consulta", convite.Perfil);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime.AddHours(48), convite.ExpiraEm, TimeSpan.FromSeconds(1));
        var email = Assert.Single(_emails.Mensagens);
        Assert.Equal("ana@acme.com", email.Para);
        Assert.Contains("Acme", email.Assunto);
        Assert.Contains($"https://app.fluxus.local/convites/aceitar?token={token}", email.Texto);
        await using var context = _database.CreateContext(_empresaId);
        var salvo = await context.Set<Convite>().SingleAsync();
        Assert.Equal(SecretToken.Hash(token), salvo.TokenHash);
    }

    [Fact]
    public async Task Criar_ComConvitePendenteParaOEmail_LancaConflict()
    {
        await ConvidarAsync();

        await Assert.ThrowsAsync<ConflictException>(() => ConvidarAsync("ANA@acme.com"));
    }

    [Fact]
    public async Task Criar_ComConviteAnteriorVencido_CancelaOAnteriorECriaOutro()
    {
        var (anterior, _) = await ConvidarAsync();
        _clock.Avancar(TimeSpan.FromHours(49));

        var (novo, _) = await ConvidarAsync();

        await using var context = _database.CreateContext(_empresaId);
        var status = await context.Set<Convite>().ToDictionaryAsync(c => c.Id, c => c.Status);
        Assert.Equal(ConviteStatus.Cancelado, status[anterior.Id]);
        Assert.Equal(ConviteStatus.Pendente, status[novo.Id]);
    }

    [Fact]
    public async Task Criar_ParaUsuarioJaVinculado_LancaConflict()
    {
        var (_, token) = await ConvidarAsync("bia@acme.com");
        await AceitarAsync(token, nome: null, senha: null);

        await Assert.ThrowsAsync<ConflictException>(() => ConvidarAsync("bia@acme.com"));
    }

    [Fact]
    public async Task Criar_ParaUsuarioComVinculoInativo_PedeParaReativar()
    {
        var (_, token) = await ConvidarAsync("bia@acme.com");
        await AceitarAsync(token, nome: null, senha: null);
        await using (var context = _database.CreateContext(_empresaId))
        {
            (await context.Set<UsuarioEmpresa>().SingleAsync()).Inativar();
            await context.SaveChangesAsync();
        }

        var erro = await Assert.ThrowsAsync<ConflictException>(() => ConvidarAsync("bia@acme.com"));
        Assert.Contains("Reative o vínculo", erro.Message);
    }

    [Fact]
    public async Task Criar_ParaUsuarioInativo_LancaBusinessRule()
    {
        await using (var context = _database.CreateContext(_empresaId))
        {
            (await context.Set<Usuario>().SingleAsync(u => u.Email == "bia@acme.com")).Inativar();
            await context.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<BusinessRuleException>(() => ConvidarAsync("bia@acme.com"));
        Assert.Empty(_emails.Mensagens);
    }

    [Fact]
    public async Task Criar_ComPerfilDeOutraEmpresa_LancaNotFound()
    {
        var (service, context) = CriarService();
        await using var _ = context;

        await Assert.ThrowsAsync<NotFoundException>(() => service.CriarAsync(new CriarConviteRequest("ana@acme.com", _outroPerfilId)));
    }

    [Fact]
    public async Task Criar_QuandoOEnvioFalha_NaoGravaOConvite()
    {
        _emails.Falhar = true;

        await Assert.ThrowsAsync<IOException>(() => ConvidarAsync());

        await using var context = _database.CreateContext(_empresaId);
        Assert.False(await context.Set<Convite>().AnyAsync());
    }

    [Fact]
    public async Task Aceitar_ComNovoUsuario_CriaUsuarioVinculoEMarcaAceito()
    {
        var (convite, token) = await ConvidarAsync();

        await AceitarAsync(token, nome: "  Ana Souza ", senha: "segredo123");

        await using var context = _database.CreateContext(_empresaId);
        var usuario = await context.Set<Usuario>().SingleAsync(u => u.Email == "ana@acme.com");
        Assert.Equal("Ana Souza", usuario.Nome);
        Assert.True(_hasher.Verificar("segredo123", usuario.SenhaHash));
        var vinculo = await context.Set<UsuarioEmpresa>().SingleAsync(v => v.UsuarioId == usuario.Id);
        Assert.Equal((_empresaId, _perfilId), (vinculo.EmpresaId, vinculo.PerfilId));
        var aceito = await context.Set<Convite>().SingleAsync(c => c.Id == convite.Id);
        Assert.Equal(ConviteStatus.Aceito, aceito.Status);
        Assert.Equal(usuario.Id, aceito.UsuarioId);
    }

    [Theory]
    [InlineData(null, "segredo123")]
    [InlineData("Ana", null)]
    [InlineData(" ", "segredo123")]
    public async Task Aceitar_ComNovoUsuarioSemNomeOuSenha_LancaBusinessRule(string? nome, string? senha)
    {
        var (_, token) = await ConvidarAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() => AceitarAsync(token, nome, senha));
    }

    [Fact]
    public async Task Aceitar_ComUsuarioExistente_SoCriaOVinculo_SemTrocarASenha()
    {
        var (_, token) = await ConvidarAsync("bia@acme.com");

        await AceitarAsync(token, nome: "Outro Nome", senha: "outraSenha9");

        await using var context = _database.CreateContext(_empresaId);
        var bia = await context.Set<Usuario>().SingleAsync(u => u.Email == "bia@acme.com");
        Assert.Equal("Bia", bia.Nome);
        Assert.True(_hasher.Verificar("senhaAntiga1", bia.SenhaHash));
        Assert.True(await context.Set<UsuarioEmpresa>().AnyAsync(v => v.UsuarioId == bia.Id && v.EmpresaId == _empresaId));
    }

    [Fact]
    public async Task Aceitar_DuasVezes_LancaBusinessRule()
    {
        var (_, token) = await ConvidarAsync();
        await AceitarAsync(token);

        var erro = await Assert.ThrowsAsync<BusinessRuleException>(() => AceitarAsync(token));
        Assert.Contains("já foi aceito", erro.Message);
    }

    [Fact]
    public async Task Aceitar_ConviteExpirado_LancaBusinessRule()
    {
        var (_, token) = await ConvidarAsync();
        _clock.Avancar(TimeSpan.FromHours(48));

        var erro = await Assert.ThrowsAsync<BusinessRuleException>(() => AceitarAsync(token));
        Assert.Contains("expirou", erro.Message);
    }

    [Fact]
    public async Task Aceitar_ComTokenDesconhecido_LancaNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => AceitarAsync(SecretToken.Create()));
    }

    [Fact]
    public async Task Reenviar_TrocaOTokenERenovaOPrazo_EOLinkAntigoDeixaDeValer()
    {
        var (convite, antigo) = await ConvidarAsync();
        _clock.Avancar(TimeSpan.FromHours(50));

        var (service, context) = CriarService();
        ConviteResponse reenviado;
        await using (context)
            reenviado = await service.ReenviarAsync(convite.Id);

        Assert.Equal("Pendente", reenviado.Status);
        Assert.True(reenviado.ExpiraEm > convite.ExpiraEm);
        Assert.Equal(2, _emails.Mensagens.Count);
        await Assert.ThrowsAsync<NotFoundException>(() => AceitarAsync(antigo));
        await AceitarAsync(UltimoToken());
    }

    [Fact]
    public async Task Cancelar_ImpedeOAceite_EConviteAceitoNaoPodeSerCancelado()
    {
        var (cancelado, tokenCancelado) = await ConvidarAsync();
        var (aceito, tokenAceito) = await ConvidarAsync("bia@acme.com");
        await AceitarAsync(tokenAceito);

        var (service, context) = CriarService();
        await using (context)
        {
            await service.CancelarAsync(cancelado.Id);
            await Assert.ThrowsAsync<BusinessRuleException>(() => service.CancelarAsync(aceito.Id));
        }

        var erro = await Assert.ThrowsAsync<BusinessRuleException>(() => AceitarAsync(tokenCancelado));
        Assert.Contains("cancelado", erro.Message);
    }

    [Fact]
    public async Task Consultar_InformaEmpresaPerfilESeOUsuarioJaExiste()
    {
        var (_, tokenNovo) = await ConvidarAsync();
        var (_, tokenBia) = await ConvidarAsync("bia@acme.com");

        var novo = await ComServicePublicoAsync(s => s.ConsultarAsync(new ConsultarConviteRequest(tokenNovo)));
        var bia = await ComServicePublicoAsync(s => s.ConsultarAsync(new ConsultarConviteRequest(tokenBia)));

        Assert.Equal(("ana@acme.com", "Acme", "Consulta", false), (novo.Email, novo.Empresa, novo.Perfil, novo.UsuarioExistente));
        Assert.True(bia.UsuarioExistente);
    }

    [GeneratedRegex("token=([0-9A-F]{64})")]
    private static partial Regex TokenNoLink();

    private sealed class EmailsEnviados : IEmailSender
    {
        public List<MensagemEmail> Mensagens { get; } = [];
        public bool Falhar { get; set; }

        public Task EnviarAsync(MensagemEmail mensagem, CancellationToken cancellationToken = default)
        {
            if (Falhar)
                throw new IOException("SMTP indisponível.");
            Mensagens.Add(mensagem);
            return Task.CompletedTask;
        }
    }

    private sealed class Relogio : TimeProvider
    {
        private DateTimeOffset _agora = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _agora;
        public void Avancar(TimeSpan tempo) => _agora += tempo;
    }
}
