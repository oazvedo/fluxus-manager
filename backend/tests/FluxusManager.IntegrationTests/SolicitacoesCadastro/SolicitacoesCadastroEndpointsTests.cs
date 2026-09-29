using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FluxusManager.Application.DTOs.AuthDtos;
using FluxusManager.Application.DTOs.ConvitesDtos;
using FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;
using FluxusManager.Application.Email;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Security;
using FluxusManager.Domain.Entities;
using FluxusManager.Infrastructure.Database;
using FluxusManager.IntegrationTests.Database;
using FluxusManager.IntegrationTests.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Core;

namespace FluxusManager.IntegrationTests.SolicitacoesCadastro;

[Collection(PostgresCollection.Name)]
public sealed partial class SolicitacoesCadastroEndpointsTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Cnpj = "11.222.333/0001-81";
    private const string Email = "ana@acme.com";

    private SolicitacoesFactory _factory = null!;
    private HttpClient _publico = null!;
    private HttpClient _admin = null!;
    private HttpClient _tenant = null!;

    public async Task InitializeAsync()
    {
        _factory = new SolicitacoesFactory(await postgres.CreateDatabaseAsync());
        _publico = _factory.CreateClient();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var admin = new Usuario("Admin Fluxus", "admin@fluxus.local", hasher.Hash("segredo123"));
        var comum = new Usuario("Gestor de empresa", "gestor@cliente.com", hasher.Hash("segredo123"));
        db.AddRange(admin, comum, new AdministradorPlataforma(admin.Id));
        await db.SaveChangesAsync();

        _admin = _factory.CreateClient();
        _admin.DefaultRequestHeaders.Add("X-Test-User", admin.Id.ToString());
        // Autenticado com todas as permissões de tenant (inclusive empresas.editar), mas sem o papel global.
        _tenant = _factory.CreateClient();
        _tenant.DefaultRequestHeaders.Add("X-Test-User", comum.Id.ToString());
    }

    public async Task DisposeAsync()
    {
        _publico.Dispose();
        _admin.Dispose();
        _tenant.Dispose();
        await _factory.DisposeAsync();
    }

    private static CriarSolicitacaoCadastroRequest Pedido(string cnpj = Cnpj, string email = Email)
        => new("Acme Ltda", "Acme", cnpj, "Ana Souza", email, "(11) 99999-0000");

    private Task ProcessarEmailsAsync()
        => _factory.Services.GetRequiredService<ISolicitacaoCadastroEmails>().ProcessarDevidosAsync();

    private string TokenDoUltimoEmail(string caminho)
    {
        var mensagem = _factory.Emails.Mensagens.Last(m => m.Texto.Contains(caminho));
        return Regex.Match(mensagem.Texto, Regex.Escape(caminho) + @"\?token=([0-9A-F]{64})").Groups[1].Value;
    }

    private async Task<string> SolicitarEVerificarAsync(string cnpj = Cnpj, string email = Email)
    {
        using var envio = await _publico.PostAsJsonAsync("/solicitacoes-cadastro", Pedido(cnpj, email));
        Assert.True(envio.StatusCode == HttpStatusCode.Accepted, await envio.Content.ReadAsStringAsync());
        await ProcessarEmailsAsync();
        using var verificacao = await _publico.PostAsJsonAsync("/solicitacoes-cadastro/verificar",
            new TokenSolicitacaoCadastroRequest(TokenDoUltimoEmail("solicitar-cadastro/verificar")));
        Assert.Equal(HttpStatusCode.OK, verificacao.StatusCode);
        await ProcessarEmailsAsync();
        return await IdDaSolicitacaoAsync(email);
    }

    private async Task<string> IdDaSolicitacaoAsync(string email)
    {
        var lista = JsonNode.Parse(await _admin.GetStringAsync("/plataforma/solicitacoes-cadastro"))!;
        return lista["items"]!.AsArray().Single(i => i!["responsavelEmail"]!.GetValue<string>() == email)!["id"]!.GetValue<string>();
    }

    private async Task<T> ConsultarAsync<T>(Func<AppDbContext, Task<T>> consulta)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await consulta(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private Task<int> ContarEmpresasAsync()
        => ConsultarAsync(db => db.Set<Empresa>().IgnoreQueryFilters().CountAsync(e => e.Cnpj == "11222333000181"));

    private static async Task<string?> TipoDoProblemaAsync(HttpResponseMessage resposta)
        => JsonNode.Parse(await resposta.Content.ReadAsStringAsync())!["type"]?.GetValue<string>();

    [Fact]
    public async Task FluxoCompleto_SolicitarVerificarAprovarEAceitarConvite()
    {
        using var envio = await _publico.PostAsJsonAsync("/solicitacoes-cadastro", Pedido());
        Assert.Equal(HttpStatusCode.Accepted, envio.StatusCode);
        Assert.True(envio.Headers.CacheControl?.NoStore);
        Assert.Equal(0, await ContarEmpresasAsync());

        await ProcessarEmailsAsync();
        var tokenVerificacao = TokenDoUltimoEmail("solicitar-cadastro/verificar");
        Assert.Equal(Email, _factory.Emails.Mensagens.Single().Para);

        // O pedido já aparece na fila, mas só pode ser decidido depois da verificação.
        var id = await IdDaSolicitacaoAsync(Email);
        using var cedo = await _admin.PostAsJsonAsync($"/plataforma/solicitacoes-cadastro/{id}/aprovar", new AprovarSolicitacaoRequest(null));
        Assert.Equal(HttpStatusCode.Conflict, cedo.StatusCode);

        using var verificacao = await _publico.PostAsJsonAsync("/solicitacoes-cadastro/verificar", new TokenSolicitacaoCadastroRequest(tokenVerificacao));
        Assert.Equal("PendenteAnalise", JsonNode.Parse(await verificacao.Content.ReadAsStringAsync())!["status"]!.GetValue<string>());

        // Uso único.
        using var repetida = await _publico.PostAsJsonAsync("/solicitacoes-cadastro/verificar", new TokenSolicitacaoCadastroRequest(tokenVerificacao));
        Assert.Equal(HttpStatusCode.NotFound, repetida.StatusCode);
        Assert.EndsWith("/token-invalido", await TipoDoProblemaAsync(repetida));

        await ProcessarEmailsAsync();
        var tokenAcompanhamento = TokenDoUltimoEmail("solicitar-cadastro/acompanhar");
        using var emAnalise = await _publico.PostAsJsonAsync("/solicitacoes-cadastro/acompanhar", new TokenSolicitacaoCadastroRequest(tokenAcompanhamento));
        var acompanhamento = JsonNode.Parse(await emAnalise.Content.ReadAsStringAsync())!;
        Assert.Equal("PendenteAnalise", acompanhamento["status"]!.GetValue<string>());
        Assert.Null(acompanhamento["cnpj"]);
        Assert.Null(acompanhamento["observacaoInterna"]);

        using var aprovacao = await _admin.PostAsJsonAsync($"/plataforma/solicitacoes-cadastro/{id}/aprovar",
            new AprovarSolicitacaoRequest("Cliente indicado pelo comercial."));
        Assert.Equal(HttpStatusCode.OK, aprovacao.StatusCode);
        var detalhe = JsonNode.Parse(await aprovacao.Content.ReadAsStringAsync())!;
        Assert.Equal("Aprovada", detalhe["status"]!.GetValue<string>());
        Assert.Equal("Admin Fluxus", detalhe["decididaPor"]!["nome"]!.GetValue<string>());
        Assert.Equal("Cliente indicado pelo comercial.", detalhe["observacaoInterna"]!.GetValue<string>());
        Assert.Contains(detalhe["emails"]!.AsArray(), e => e!["tipo"]!.GetValue<string>() == "Aprovacao" && e["status"]!.GetValue<string>() == "Enviado");
        var empresaId = Guid.Parse(detalhe["empresaId"]!.GetValue<string>());

        Assert.Equal(1, await ContarEmpresasAsync());
        var perfis = await ConsultarAsync(db => db.Set<Perfil>().IgnoreQueryFilters().Where(p => p.TenantId == empresaId).Select(p => p.Nome).ToListAsync());
        Assert.Equal(["Administrador", "Consulta"], perfis.Order());

        // Convite do responsável como administrador da empresa: aceitar cria a conta e permite entrar.
        var tokenConvite = TokenDoUltimoEmail("convites/aceitar");
        using var aceite = await _publico.PostAsJsonAsync("/convites/aceitar", new AceitarConviteRequest(tokenConvite, "Ana Souza", "segredo123"));
        Assert.Equal(HttpStatusCode.NoContent, aceite.StatusCode);
        using var login = await _publico.PostAsJsonAsync("/auth/login", new LoginRequest(Email, "segredo123"));
        var sessao = (await login.Content.ReadFromJsonAsync<TokenResponse>())!;
        Assert.Equal((empresaId, "Administrador", false), (sessao.TenantId, sessao.Role, sessao.AdministradorPlataforma));

        using var decidido = await _publico.PostAsJsonAsync("/solicitacoes-cadastro/acompanhar",
            new TokenSolicitacaoCadastroRequest(TokenDoUltimoEmail("solicitar-cadastro/acompanhar")));
        Assert.Equal("Aprovada", JsonNode.Parse(await decidido.Content.ReadAsStringAsync())!["status"]!.GetValue<string>());
    }

    [Fact]
    public async Task Aprovar_RepetidoOuConcorrente_CriaUmaUnicaEmpresaEConvite()
    {
        var id = await SolicitarEVerificarAsync();

        var respostas = await Task.WhenAll(Enumerable.Range(0, 5).Select(async _ =>
        {
            using var resposta = await _admin.PostAsJsonAsync($"/plataforma/solicitacoes-cadastro/{id}/aprovar", new AprovarSolicitacaoRequest(null));
            return resposta.StatusCode;
        }));
        using var repetida = await _admin.PostAsJsonAsync($"/plataforma/solicitacoes-cadastro/{id}/aprovar", new AprovarSolicitacaoRequest(null));

        Assert.All(respostas, status => Assert.Equal(HttpStatusCode.OK, status));
        Assert.Equal(HttpStatusCode.OK, repetida.StatusCode);
        Assert.Equal(1, await ContarEmpresasAsync());
        Assert.Equal(1, await ConsultarAsync(db => db.Set<Convite>().IgnoreQueryFilters().CountAsync(c => c.Email == Email)));
        Assert.Single(_factory.Emails.Mensagens, m => m.Texto.Contains("convites/aceitar"));
    }

    [Fact]
    public async Task Recusar_ExigeMotivo_ComunicaOSolicitante_EImpedeAprovacao()
    {
        var id = await SolicitarEVerificarAsync();

        using var semMotivo = await _admin.PostAsJsonAsync($"/plataforma/solicitacoes-cadastro/{id}/recusar", new RecusarSolicitacaoRequest("curto", null));
        Assert.Equal(HttpStatusCode.BadRequest, semMotivo.StatusCode);
        Assert.Contains("motivo", JsonNode.Parse(await semMotivo.Content.ReadAsStringAsync())!["errors"]!.AsObject().Select(e => e.Key));

        const string motivo = "Não encontramos o CNPJ ativo na Receita Federal.";
        using var recusa = await _admin.PostAsJsonAsync($"/plataforma/solicitacoes-cadastro/{id}/recusar",
            new RecusarSolicitacaoRequest(motivo, "Consultado em 28/09."));
        Assert.Equal(HttpStatusCode.OK, recusa.StatusCode);

        var email = _factory.Emails.Mensagens.Last();
        Assert.Contains(motivo, email.Texto);
        Assert.DoesNotContain("Consultado em 28/09.", email.Texto);

        using var acompanhamento = await _publico.PostAsJsonAsync("/solicitacoes-cadastro/acompanhar",
            new TokenSolicitacaoCadastroRequest(TokenDoUltimoEmail("solicitar-cadastro/acompanhar")));
        var dados = JsonNode.Parse(await acompanhamento.Content.ReadAsStringAsync())!;
        Assert.Equal(("Recusada", motivo), (dados["status"]!.GetValue<string>(), dados["motivoRecusa"]!.GetValue<string>()));
        Assert.Null(dados["observacaoInterna"]);

        using var aprovar = await _admin.PostAsJsonAsync($"/plataforma/solicitacoes-cadastro/{id}/aprovar", new AprovarSolicitacaoRequest(null));
        Assert.Equal(HttpStatusCode.Conflict, aprovar.StatusCode);
        Assert.EndsWith("/status-invalido", await TipoDoProblemaAsync(aprovar));
        Assert.Equal(0, await ContarEmpresasAsync());

        var detalhe = JsonNode.Parse(await _admin.GetStringAsync($"/plataforma/solicitacoes-cadastro/{id}"))!;
        var eventos = detalhe["historico"]!.AsArray().Select(e => e!["evento"]!.GetValue<string>()).ToList();
        Assert.Equal(["Criada", "Verificada", "Recusada", "Visualizada"], eventos);
    }

    [Fact]
    public async Task Aprovar_CnpjQueVirouClienteNoMeioDoCaminho_Retorna409SemCriarEmpresa()
    {
        var id = await SolicitarEVerificarAsync();
        await ConsultarAsync(async db =>
        {
            db.Add(new Empresa("Acme Concorrente", null, "11222333000181"));
            return await db.SaveChangesAsync();
        });

        using var aprovacao = await _admin.PostAsJsonAsync($"/plataforma/solicitacoes-cadastro/{id}/aprovar", new AprovarSolicitacaoRequest(null));

        Assert.Equal(HttpStatusCode.Conflict, aprovacao.StatusCode);
        Assert.EndsWith("/cnpj-ja-cadastrado", await TipoDoProblemaAsync(aprovacao));
        Assert.Equal(1, await ContarEmpresasAsync());
        Assert.Equal(0, await ConsultarAsync(db => db.Set<Perfil>().IgnoreQueryFilters().CountAsync()));
    }

    [Fact]
    public async Task EnvioPublico_RespondeIgualParaClienteExistenteEPedidoDuplicado()
    {
        using var novo = await _publico.PostAsJsonAsync("/solicitacoes-cadastro", Pedido());
        var mensagem = await novo.Content.ReadAsStringAsync();

        // Mesmo CNPJ com outro e-mail, e mesmo e-mail com outro CNPJ: não criam outro pedido.
        using var mesmoCnpj = await _publico.PostAsJsonAsync("/solicitacoes-cadastro", Pedido(email: "outra@acme.com"));
        using var mesmoEmail = await _publico.PostAsJsonAsync("/solicitacoes-cadastro", Pedido(cnpj: "45.723.174/0001-10"));
        // CNPJ que já é cliente: nenhum pedido e nenhum e-mail.
        await ConsultarAsync(async db =>
        {
            db.Add(new Empresa("Cliente Antigo", null, "04252011000110"));
            return await db.SaveChangesAsync();
        });
        using var cliente = await _publico.PostAsJsonAsync("/solicitacoes-cadastro", Pedido(cnpj: "04.252.011/0001-10", email: "novo@cliente.com"));

        foreach (var resposta in new[] { mesmoCnpj, mesmoEmail, cliente })
        {
            Assert.Equal(HttpStatusCode.Accepted, resposta.StatusCode);
            Assert.Equal(mensagem, await resposta.Content.ReadAsStringAsync());
        }
        Assert.Equal(1, await ConsultarAsync(db => db.Set<SolicitacaoCadastro>().CountAsync()));
        await ProcessarEmailsAsync();
        Assert.Single(_factory.Emails.Mensagens);

        // Repetir o mesmo pedido ainda sem verificar manda um link novo; o antigo deixa de valer.
        var primeiroToken = TokenDoUltimoEmail("solicitar-cadastro/verificar");
        _factory.Relogio.Avancar(SolicitacaoCadastro.IntervaloMinimoReenvio);
        using var repetido = await _publico.PostAsJsonAsync("/solicitacoes-cadastro", Pedido());
        await ProcessarEmailsAsync();
        Assert.Equal(2, _factory.Emails.Mensagens.Count);
        using var antigo = await _publico.PostAsJsonAsync("/solicitacoes-cadastro/verificar", new TokenSolicitacaoCadastroRequest(primeiroToken));
        Assert.Equal(HttpStatusCode.NotFound, antigo.StatusCode);
    }

    [Fact]
    public async Task EnvioPublico_Invalido_Retorna400ComOsCampos()
    {
        using var resposta = await _publico.PostAsJsonAsync("/solicitacoes-cadastro",
            new { razaoSocial = "", cnpj = "11.222.333/0001-00", responsavelNome = "Ana", responsavelEmail = "nao-e-email", responsavelTelefone = "abc" });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var campos = JsonNode.Parse(await resposta.Content.ReadAsStringAsync())!["errors"]!.AsObject().Select(e => e.Key).ToList();
        Assert.Equal(["cnpj", "razaoSocial", "responsavelEmail", "responsavelTelefone"], campos.Order());
        Assert.Equal(0, await ConsultarAsync(db => db.Set<SolicitacaoCadastro>().CountAsync()));
    }

    [Fact]
    public async Task LinkDeVerificacaoVencido_Retorna410_EPedirNovoLinkFunciona()
    {
        using var _ = await _publico.PostAsJsonAsync("/solicitacoes-cadastro", Pedido());
        await ProcessarEmailsAsync();
        var vencido = TokenDoUltimoEmail("solicitar-cadastro/verificar");

        _factory.Relogio.Avancar(TimeSpan.FromHours(25));

        using var verificacao = await _publico.PostAsJsonAsync("/solicitacoes-cadastro/verificar", new TokenSolicitacaoCadastroRequest(vencido));
        Assert.Equal(HttpStatusCode.Gone, verificacao.StatusCode);
        Assert.EndsWith("/token-expirado", await TipoDoProblemaAsync(verificacao));

        using var reenvio = await _publico.PostAsJsonAsync("/solicitacoes-cadastro/reenviar-verificacao", new ReenviarVerificacaoRequest(Email));
        using var desconhecido = await _publico.PostAsJsonAsync("/solicitacoes-cadastro/reenviar-verificacao", new ReenviarVerificacaoRequest("ninguem@acme.com"));
        Assert.Equal(HttpStatusCode.Accepted, reenvio.StatusCode);
        Assert.Equal(await reenvio.Content.ReadAsStringAsync(), await desconhecido.Content.ReadAsStringAsync());
        await ProcessarEmailsAsync();

        using var nova = await _publico.PostAsJsonAsync("/solicitacoes-cadastro/verificar",
            new TokenSolicitacaoCadastroRequest(TokenDoUltimoEmail("solicitar-cadastro/verificar")));
        Assert.Equal(HttpStatusCode.OK, nova.StatusCode);
    }

    [Fact]
    public async Task PedidoVencidoDeOutraPessoa_NaoSeguraOCnpj()
    {
        using var abandonado = await _publico.PostAsJsonAsync("/solicitacoes-cadastro", Pedido(email: "antigo@acme.com"));
        await ProcessarEmailsAsync();
        _factory.Relogio.Avancar(TimeSpan.FromHours(25));

        await SolicitarEVerificarAsync();

        Assert.Equal(1, await ConsultarAsync(db => db.Set<SolicitacaoCadastro>().CountAsync()));
    }

    [Fact]
    public async Task PedidoCujoEmailNuncaFoiEntregue_NaoSeguraOCnpjDepoisDoPrazo()
    {
        _factory.Emails.Falhar = true;
        using var travado = await _publico.PostAsJsonAsync("/solicitacoes-cadastro", Pedido(email: "nao-existe@acme.com"));
        await ProcessarEmailsAsync();
        _factory.Emails.Falhar = false;

        // Ainda no prazo: o CNPJ continua com o primeiro pedido.
        using var cedo = await _publico.PostAsJsonAsync("/solicitacoes-cadastro", Pedido());
        Assert.Equal(1, await ConsultarAsync(db => db.Set<SolicitacaoCadastro>().CountAsync()));

        _factory.Relogio.Avancar(TimeSpan.FromHours(25));
        await SolicitarEVerificarAsync();

        var ativos = await ConsultarAsync(db => db.Set<SolicitacaoCadastro>().Select(s => s.ResponsavelEmail).ToListAsync());
        Assert.Equal([Email], ativos);
        // Os e-mails do pedido descartado saem da fila junto com ele.
        Assert.DoesNotContain(_factory.Emails.Mensagens, m => m.Para == "nao-existe@acme.com");
    }

    [Fact]
    public async Task ReenvioDeVerificacao_TemIntervaloMinimoELimite()
    {
        using var _ = await _publico.PostAsJsonAsync("/solicitacoes-cadastro", Pedido());
        await ProcessarEmailsAsync();

        for (var i = 0; i < 3; i++)
            await _publico.PostAsJsonAsync("/solicitacoes-cadastro/reenviar-verificacao", new ReenviarVerificacaoRequest(Email));
        await ProcessarEmailsAsync();
        Assert.Single(_factory.Emails.Mensagens);

        for (var i = 0; i < SolicitacaoCadastro.MaxReenviosVerificacao + 2; i++)
        {
            _factory.Relogio.Avancar(SolicitacaoCadastro.IntervaloMinimoReenvio);
            using var reenvio = await _publico.PostAsJsonAsync("/solicitacoes-cadastro/reenviar-verificacao", new ReenviarVerificacaoRequest(Email));
            Assert.Equal(HttpStatusCode.Accepted, reenvio.StatusCode);
            await ProcessarEmailsAsync();
        }

        Assert.Equal(1 + SolicitacaoCadastro.MaxReenviosVerificacao, _factory.Emails.Mensagens.Count);
    }

    [Fact]
    public async Task PainelDaPlataforma_ExigePapelGlobal_MesmoComPermissoesDeTenant()
    {
        var id = await SolicitarEVerificarAsync();
        using (var semLogin = new SolicitacoesFactory(_factory.ConnectionString, autenticacaoDeTeste: false))
        using (var anonimo = semLogin.CreateClient())
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/plataforma/solicitacoes-cadastro")).StatusCode);

        foreach (var (metodo, rota, corpo) in new (HttpMethod, string, object?)[]
        {
            (HttpMethod.Get, "/plataforma/solicitacoes-cadastro", null),
            (HttpMethod.Get, $"/plataforma/solicitacoes-cadastro/{id}", null),
            (HttpMethod.Post, $"/plataforma/solicitacoes-cadastro/{id}/aprovar", new AprovarSolicitacaoRequest(null)),
            (HttpMethod.Post, $"/plataforma/solicitacoes-cadastro/{id}/recusar", new RecusarSolicitacaoRequest("Motivo suficiente para recusar.", null)),
            (HttpMethod.Post, $"/plataforma/solicitacoes-cadastro/{id}/reenviar-emails", null),
        })
        {
            using var requisicao = new HttpRequestMessage(metodo, rota) { Content = corpo is null ? null : JsonContent.Create(corpo) };
            using var resposta = await _tenant.SendAsync(requisicao);
            Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        }

        Assert.Equal(0, await ContarEmpresasAsync());
        var detalhe = JsonNode.Parse(await _admin.GetStringAsync($"/plataforma/solicitacoes-cadastro/{id}"))!;
        Assert.Equal("PendenteAnalise", detalhe["status"]!.GetValue<string>());
    }

    [Fact]
    public async Task ComandoDaPlataforma_PromoveERevoga_ComEfeitoImediato()
    {
        using var semPapel = await _tenant.GetAsync("/plataforma/solicitacoes-cadastro");
        Assert.Equal(HttpStatusCode.Forbidden, semPapel.StatusCode);

        var saida = new StringWriter();
        Assert.Equal(0, await _factory.Services.ExecutarComandoPlataformaAsync(["promover", "GESTOR@cliente.com"], saida));
        using var promovido = await _tenant.GetAsync("/plataforma/solicitacoes-cadastro");
        Assert.Equal(HttpStatusCode.OK, promovido.StatusCode);

        Assert.Equal(0, await _factory.Services.ExecutarComandoPlataformaAsync(["revogar", "gestor@cliente.com"], saida));
        using var revogado = await _tenant.GetAsync("/plataforma/solicitacoes-cadastro");
        Assert.Equal(HttpStatusCode.Forbidden, revogado.StatusCode);

        Assert.Equal(1, await _factory.Services.ExecutarComandoPlataformaAsync(["promover", "ninguem@cliente.com"], saida));
        Assert.Equal(1, await _factory.Services.ExecutarComandoPlataformaAsync(["conceder"], saida));
    }

    [Fact]
    public async Task FalhaNoEnvioDoEmail_FicaRegistrada_EReenviarEntregaSemDuplicar()
    {
        var id = await SolicitarEVerificarAsync();
        _factory.Emails.Falhar = true;

        using var aprovacao = await _admin.PostAsJsonAsync($"/plataforma/solicitacoes-cadastro/{id}/aprovar", new AprovarSolicitacaoRequest(null));
        Assert.Equal(HttpStatusCode.OK, aprovacao.StatusCode);
        var detalhe = JsonNode.Parse(await aprovacao.Content.ReadAsStringAsync())!;
        Assert.True(detalhe["emailPendente"]!.GetValue<bool>());
        var entrega = detalhe["emails"]!.AsArray().Single(e => e!["tipo"]!.GetValue<string>() == "Aprovacao")!;
        Assert.Equal(("Falhou", 1), (entrega["status"]!.GetValue<string>(), entrega["tentativas"]!.GetValue<int>()));

        // O reenvio automático espera a próxima tentativa; antes disso nada é enviado.
        await ProcessarEmailsAsync();
        Assert.DoesNotContain(_factory.Emails.Mensagens, m => m.Texto.Contains("convites/aceitar"));

        _factory.Emails.Falhar = false;
        using var reenvio = await _admin.PostAsync($"/plataforma/solicitacoes-cadastro/{id}/reenviar-emails", null);
        var depois = JsonNode.Parse(await reenvio.Content.ReadAsStringAsync())!;
        Assert.False(depois["emailPendente"]!.GetValue<bool>());
        Assert.Single(_factory.Emails.Mensagens, m => m.Texto.Contains("convites/aceitar"));

        // O convite enviado depois da falha é o que vale.
        using var aceite = await _publico.PostAsJsonAsync("/convites/aceitar",
            new AceitarConviteRequest(TokenDoUltimoEmail("convites/aceitar"), "Ana Souza", "segredo123"));
        Assert.Equal(HttpStatusCode.NoContent, aceite.StatusCode);
        Assert.Equal(1, await ContarEmpresasAsync());
        Assert.Equal(1, await ConsultarAsync(db => db.Set<Convite>().IgnoreQueryFilters().CountAsync()));
    }

    [Fact]
    public async Task FalhaNoEmailDeVerificacao_EReenviadaAutomaticamenteComEspera()
    {
        _factory.Emails.Falhar = true;
        using var _ = await _publico.PostAsJsonAsync("/solicitacoes-cadastro", Pedido());
        await ProcessarEmailsAsync();
        Assert.Empty(_factory.Emails.Mensagens);

        _factory.Emails.Falhar = false;
        await ProcessarEmailsAsync();
        Assert.Empty(_factory.Emails.Mensagens);

        _factory.Relogio.Avancar(TimeSpan.FromMinutes(2));
        await ProcessarEmailsAsync();
        Assert.Single(_factory.Emails.Mensagens);
        Assert.Equal(1, await ConsultarAsync(db => db.Set<SolicitacaoCadastro>().CountAsync()));
    }

    [Fact]
    public async Task TokensNaoAparecemEmLogsNemNaAuditoria()
    {
        var id = await SolicitarEVerificarAsync();
        using var _ = await _admin.PostAsJsonAsync($"/plataforma/solicitacoes-cadastro/{id}/aprovar", new AprovarSolicitacaoRequest(null));
        var tokens = _factory.Emails.Mensagens
            .SelectMany(m => Regex.Matches(m.Texto, "token=([0-9A-F]{64})").Select(t => t.Groups[1].Value))
            .ToList();
        Assert.NotEmpty(tokens);

        var logs = _factory.Logs.Events.Select(e => e.RenderMessage() + string.Join(' ', e.Properties.Values)).ToList();
        var auditoria = await ConsultarAsync(db => db.Database.SqlQuery<string>(
            $"SELECT coalesce(new_values::text, '') AS \"Value\" FROM audit.change_log WHERE table_name LIKE 'solicitacoes_cadastro%'").ToListAsync());

        Assert.NotEmpty(auditoria);
        foreach (var token in tokens)
        {
            Assert.DoesNotContain(logs, l => l.Contains(token));
            Assert.DoesNotContain(auditoria, a => a.Contains(token) || a.Contains(SecretToken.Hash(token)));
        }
    }

    [Fact]
    public async Task ListaFiltraPorStatusEPeriodo()
    {
        await SolicitarEVerificarAsync();
        using var _ = await _publico.PostAsJsonAsync("/solicitacoes-cadastro", Pedido(cnpj: "45.723.174/0001-10", email: "bia@beta.com"));

        var pendentes = JsonNode.Parse(await _admin.GetStringAsync("/plataforma/solicitacoes-cadastro?status=PendenteAnalise"))!;
        var hoje = DateOnly.FromDateTime(_factory.Relogio.GetUtcNow().UtcDateTime);
        var amanha = hoje.AddDays(1).ToString("yyyy-MM-dd");
        var futuras = JsonNode.Parse(await _admin.GetStringAsync($"/plataforma/solicitacoes-cadastro?de={amanha}"))!;

        Assert.Equal(Email, pendentes["items"]!.AsArray().Single()!["responsavelEmail"]!.GetValue<string>());
        Assert.Empty(futuras["items"]!.AsArray());
        Assert.Equal(2, JsonNode.Parse(await _admin.GetStringAsync("/plataforma/solicitacoes-cadastro"))!["items"]!.AsArray().Count);
    }

    [Fact]
    public async Task RotasPublicas_UsamOLimitePorIp()
    {
        using var factory = new LimiteFactory();
        using var client = factory.CreateClient();
        var invalido = new { razaoSocial = "", cnpj = "", responsavelNome = "", responsavelEmail = "" };

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/solicitacoes-cadastro", invalido)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/solicitacoes-cadastro", invalido)).StatusCode);
        using var bloqueada = await client.PostAsJsonAsync("/solicitacoes-cadastro", invalido);

        Assert.Equal(HttpStatusCode.TooManyRequests, bloqueada.StatusCode);
        // A contagem é por rota: verificar e acompanhar continuam respondendo.
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/solicitacoes-cadastro/verificar", new { token = "" })).StatusCode);
    }

    private sealed class LimiteFactory() : ApiFactory("Development", useTestAuthentication: false)
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("RateLimiting:RequisicoesPorMinuto", "2");
        }
    }

    internal sealed class EmailsEnviados : IEmailSender
    {
        public List<MensagemEmail> Mensagens { get; } = [];
        public bool Falhar { get; set; }

        public Task EnviarAsync(MensagemEmail mensagem, CancellationToken cancellationToken = default)
        {
            if (Falhar)
                throw new InvalidOperationException($"SMTP indisponível para {mensagem.Para}");
            lock (Mensagens)
                Mensagens.Add(mensagem);
            return Task.CompletedTask;
        }
    }

    internal sealed class Relogio : TimeProvider
    {
        private DateTimeOffset _agora = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _agora;
        public void Avancar(TimeSpan tempo) => _agora += tempo;
    }

    private sealed class SolicitacoesFactory(string connectionString, bool autenticacaoDeTeste = true)
        : ApiComBancoFactory(connectionString, useTestAuthentication: autenticacaoDeTeste, useRealAuthService: true)
    {
        public EmailsEnviados Emails { get; } = new();
        public Relogio Relogio { get; } = new();
        public CollectingSink Logs { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(Emails);
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Relogio);
                services.AddSingleton<ILogEventSink>(Logs);
            });
        }
    }
}
