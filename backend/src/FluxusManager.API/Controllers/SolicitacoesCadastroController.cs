using FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;
using FluxusManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FluxusManager.API.Controllers;

/// <summary>
/// Rotas públicas da solicitação de cadastro (limitadas por IP como toda rota anônima). As respostas de envio são
/// sempre iguais, para não revelar se um CNPJ ou e-mail já é cliente ou já tem pedido.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("solicitacoes-cadastro")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class SolicitacoesCadastroController(ISolicitacaoCadastroService service) : ControllerBase
{
    private static readonly MensagemResponse Recebida = new(
        "Recebemos o pedido. Se os dados puderem seguir para análise, você vai receber um e-mail para confirmar o endereço.");

    private static readonly MensagemResponse Reenviada = new(
        "Se houver um pedido aguardando confirmação para este e-mail, enviamos um novo link.");

    [HttpPost]
    [ProducesResponseType<MensagemResponse>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Solicitar(CriarSolicitacaoCadastroRequest request, CancellationToken cancellationToken)
    {
        await service.SolicitarAsync(request, cancellationToken);
        return Accepted(Recebida);
    }

    [HttpPost("verificar")]
    public Task<VerificacaoSolicitacaoResponse> Verificar(TokenSolicitacaoCadastroRequest request, CancellationToken cancellationToken)
        => service.VerificarAsync(request, cancellationToken);

    [HttpPost("reenviar-verificacao")]
    [ProducesResponseType<MensagemResponse>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ReenviarVerificacao(ReenviarVerificacaoRequest request, CancellationToken cancellationToken)
    {
        await service.ReenviarVerificacaoAsync(request, cancellationToken);
        return Accepted(Reenviada);
    }

    [HttpPost("acompanhar")]
    public Task<AcompanhamentoSolicitacaoResponse> Acompanhar(TokenSolicitacaoCadastroRequest request, CancellationToken cancellationToken)
        => service.AcompanharAsync(request, cancellationToken);
}
