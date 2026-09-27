using FluxusManager.Application.DTOs.ConvitesDtos;
using FluxusManager.Application.Email;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Options;
using FluxusManager.Application.Security;
using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Exceptions;
using FluxusManager.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace FluxusManager.Application.Services;

/// <summary>
/// Convites de acesso da empresa selecionada. O e-mail é enviado dentro da transação: se o envio falhar, nada é gravado.
/// O aceite é público e passa a operar na empresa do próprio convite.
/// </summary>
public class ConviteService(
    IConviteRepository convites,
    IPerfilRepository perfis,
    IEmpresaRepository empresas,
    IUsuarioRepository usuarios,
    IUsuarioEmpresaRepository vinculos,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    ITenantContext tenant,
    IAuditContext auditContext,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    IOptions<ConviteOptions> conviteOptions,
    IOptions<FrontendOptions> frontendOptions) : IConviteService
{
    private DateTime Agora => clock.GetUtcNow().UtcDateTime;

    private TimeSpan Validade => TimeSpan.FromHours(conviteOptions.Value.ValidadeHoras);

    public async Task<PagedResult<ConviteResponse>> ListarAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var agora = Agora;
        return (await convites.ListAsync(page, pageSize, cancellationToken)).Map(c => ConviteResponse.DeEntidade(c, agora));
    }

    public async Task<ConviteResponse> ObterAsync(Guid id, CancellationToken cancellationToken = default)
        => ConviteResponse.DeEntidade(await BuscarAsync(id, cancellationToken), Agora);

    public async Task<ConviteResponse> CriarAsync(CriarConviteRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenant.TenantId ?? throw new InvalidOperationException("Não há empresa selecionada para convidar.");
        var email = EnderecoEmail.Normalizar(request.Email);
        var perfil = await perfis.ObterAtivoAsync(request.PerfilId, cancellationToken)
            ?? throw new NotFoundException("Perfil ativo", request.PerfilId);

        await ValidarDestinatarioAsync(email, tenantId, cancellationToken);

        var anterior = await convites.ObterPendentePorEmailAsync(email, cancellationToken);
        if (anterior is not null && anterior.Pendente(Agora))
            throw new ConflictException($"Já existe um convite pendente para '{email}'. Reenvie o convite existente.");

        var token = SecretToken.Create();
        var convite = new Convite(perfil, email, SecretToken.Hash(token), clock.ExpiracaoEmSegundos(Validade));
        var mensagem = await MensagemAsync(convite, token, cancellationToken);

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // O vencido sai de pendente antes, por causa do índice único de convite pendente por e-mail.
            if (anterior is not null)
            {
                anterior.Cancelar(Agora);
                await unitOfWork.CommitAsync(ct);
            }

            convites.Add(convite);
            await unitOfWork.CommitAsync(ct);
            await emailSender.EnviarAsync(mensagem, ct);
        }, cancellationToken);

        return ConviteResponse.DeEntidade(convite, Agora);
    }

    public async Task<ConviteResponse> ReenviarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var convite = await BuscarAsync(id, cancellationToken);
        if (convite.Status != ConviteStatus.Pendente)
            throw new BusinessRuleException("Só convites pendentes ou expirados podem ser reenviados.");
        if (!convite.Perfil.Ativo)
            throw new BusinessRuleException("O perfil deste convite foi inativado. Cancele-o e envie um novo convite.");
        await ValidarDestinatarioAsync(convite.Email, convite.TenantId, cancellationToken);

        var token = SecretToken.Create();
        convite.Renovar(SecretToken.Hash(token), clock.ExpiracaoEmSegundos(Validade));
        var mensagem = await MensagemAsync(convite, token, cancellationToken);

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await unitOfWork.CommitAsync(ct);
            await emailSender.EnviarAsync(mensagem, ct);
        }, cancellationToken);

        return ConviteResponse.DeEntidade(convite, Agora);
    }

    public async Task CancelarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var convite = await BuscarAsync(id, cancellationToken);
        if (convite.Status == ConviteStatus.Aceito)
            throw new BusinessRuleException("Este convite já foi aceito. Para remover o acesso, desvincule o usuário.");
        if (convite.Status == ConviteStatus.Cancelado)
            return;

        convite.Cancelar(Agora);
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task<ConviteDetalhesResponse> ConsultarAsync(ConsultarConviteRequest request, CancellationToken cancellationToken = default)
    {
        var (convite, empresa, perfil) = await AbrirConviteAsync(request.Token, Agora, cancellationToken);
        var usuarioExistente = await usuarios.ObterPorEmailAsync(convite.Email, cancellationToken) is not null;

        return new ConviteDetalhesResponse(convite.Email, NomeEmpresa(empresa), perfil.Nome, convite.ExpiraEm, usuarioExistente);
    }

    public async Task AceitarAsync(AceitarConviteRequest request, CancellationToken cancellationToken = default)
    {
        // Um único instante: o prazo conferido na abertura é o mesmo usado para aceitar.
        var agora = Agora;
        var (convite, _, perfil) = await AbrirConviteAsync(request.Token, agora, cancellationToken);
        var usuario = await usuarios.ObterPorEmailAsync(convite.Email, cancellationToken);
        var novo = usuario is null;

        if (usuario is null)
        {
            if (string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrEmpty(request.Senha))
                throw new BusinessRuleException("Informe o seu nome e uma senha para criar o acesso.");
            usuario = new Usuario(request.Nome.Trim(), convite.Email, passwordHasher.Hash(request.Senha));
        }
        else if (!usuario.Ativo)
            throw new BusinessRuleException("O seu usuário está inativo. Fale com o administrador da empresa.");
        else if (await vinculos.ObterVinculoAsync(usuario.Id, convite.TenantId, cancellationToken) is { } existente)
            throw existente.Ativo
                ? new ConflictException("Você já tem acesso a esta empresa. Entre com o seu e-mail e senha.")
                : new BusinessRuleException("O seu acesso a esta empresa está inativo. Peça ao administrador para reativá-lo.");

        // Rota pública: a auditoria registra quem aceitou.
        auditContext.SetUser(usuario.Id.ToString());
        var vinculo = new UsuarioEmpresa(usuario.Id, convite.TenantId, perfil.Id);
        vinculo.AssociarPerfil(perfil);

        await unitOfWork.ExecuteInTransactionAsync(ct =>
        {
            if (novo)
                usuarios.Add(usuario);
            vinculos.Add(vinculo);
            convite.Aceitar(usuario.Id, agora);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    /// <summary>
    /// Localiza o convite pelo token, confere se ainda vale e <b>define o tenant da requisição</b> como a empresa dele.
    /// </summary>
    private async Task<(Convite Convite, Empresa Empresa, Perfil Perfil)> AbrirConviteAsync(string token, DateTime agora, CancellationToken cancellationToken)
    {
        var convite = await convites.ObterPorTokenHashAsync(SecretToken.Hash(token), cancellationToken)
            ?? throw new NotFoundException("Convite não encontrado. Se ele foi reenviado, use o link do e-mail mais recente.");

        if (convite.Status == ConviteStatus.Aceito)
            throw new BusinessRuleException("Este convite já foi aceito. Entre com o seu e-mail e senha.");
        if (convite.Status == ConviteStatus.Cancelado)
            throw new BusinessRuleException("Este convite foi cancelado. Peça um novo convite ao administrador da empresa.");
        if (convite.Expirado(agora))
            throw new BusinessRuleException("Este convite expirou. Peça um novo convite ao administrador da empresa.");

        tenant.SetTenant(convite.TenantId);
        var empresa = await empresas.GetByIdAsync(convite.TenantId, cancellationToken);
        if (empresa is not { Ativo: true })
            throw new BusinessRuleException("A empresa deste convite não está ativa. Fale com o administrador da empresa.");
        var perfil = await perfis.ObterAtivoAsync(convite.PerfilId, cancellationToken)
            ?? throw new BusinessRuleException("O perfil deste convite não está mais disponível. Peça um novo convite ao administrador da empresa.");

        return (convite, empresa, perfil);
    }

    /// <summary>Montada antes da transação, para ela ficar aberta só durante a gravação e o envio.</summary>
    private async Task<MensagemEmail> MensagemAsync(Convite convite, string token, CancellationToken cancellationToken)
    {
        var empresa = await empresas.GetByIdAsync(convite.TenantId, cancellationToken)
            ?? throw new NotFoundException("Empresa", convite.TenantId);
        if (!empresa.Ativo)
            throw new BusinessRuleException("A empresa está inativa: reative-a antes de enviar convites.");
        var link = frontendOptions.Value.Link($"convites/aceitar?token={token}");

        return EmailTemplates.Convite(convite.Email, NomeEmpresa(empresa), link, Validade);
    }

    /// <summary>Recusa convites que o aceite rejeitaria: usuário inativo ou que já tem vínculo com a empresa.</summary>
    private async Task ValidarDestinatarioAsync(string email, Guid empresaId, CancellationToken cancellationToken)
    {
        if (await usuarios.ObterPorEmailAsync(email, cancellationToken) is not { } usuario)
            return;
        if (!usuario.Ativo)
            throw new BusinessRuleException($"O usuário '{email}' está inativo. Reative-o antes de convidar.");

        var vinculo = await vinculos.ObterVinculoAsync(usuario.Id, empresaId, cancellationToken);
        if (vinculo is { Ativo: true })
            throw new ConflictException($"O e-mail '{email}' já tem acesso a esta empresa.");
        if (vinculo is not null)
            throw new ConflictException($"O e-mail '{email}' tem um acesso inativo a esta empresa. Reative o vínculo em vez de convidar.");
    }

    private async Task<Convite> BuscarAsync(Guid id, CancellationToken cancellationToken)
        => await convites.ObterComPerfilAsync(id, cancellationToken) ?? throw new NotFoundException("Convite", id);

    private static string NomeEmpresa(Empresa empresa) => empresa.NomeFantasia ?? empresa.RazaoSocial;
}
