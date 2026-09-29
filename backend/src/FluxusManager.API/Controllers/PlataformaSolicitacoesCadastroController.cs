using System.ComponentModel.DataAnnotations;
using FluxusManager.API.Security;
using FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FluxusManager.API.Controllers;

/// <summary>Fila de análise do administrador da plataforma. Não depende da empresa selecionada no token.</summary>
[ApiController]
[Route("plataforma/solicitacoes-cadastro")]
[Authorize(Policy = PlataformaAdminRequirement.Policy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PlataformaSolicitacoesCadastroController(ISolicitacaoCadastroAdminService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<SolicitacaoResumoResponse>> Listar(
        SolicitacaoCadastroStatus? status = null, DateOnly? de = null, DateOnly? ate = null,
        [Range(1, int.MaxValue, ErrorMessage = "A página deve ser maior ou igual a 1.")] int page = 1,
        [Range(1, 100, ErrorMessage = "O tamanho da página deve estar entre 1 e 100.")] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => service.ListarAsync(status, de, ate, page, pageSize, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<SolicitacaoDetalheResponse> Obter(Guid id, CancellationToken cancellationToken)
        => service.ObterAsync(id, User.UsuarioId(), cancellationToken);

    [HttpPost("{id:guid}/aprovar")]
    public Task<SolicitacaoDetalheResponse> Aprovar(Guid id, AprovarSolicitacaoRequest request, CancellationToken cancellationToken)
        => service.AprovarAsync(id, request, User.UsuarioId(), cancellationToken);

    [HttpPost("{id:guid}/recusar")]
    public Task<SolicitacaoDetalheResponse> Recusar(Guid id, RecusarSolicitacaoRequest request, CancellationToken cancellationToken)
        => service.RecusarAsync(id, request, User.UsuarioId(), cancellationToken);

    [HttpPost("{id:guid}/reenviar-emails")]
    public Task<SolicitacaoDetalheResponse> ReenviarEmails(Guid id, CancellationToken cancellationToken)
        => service.ReenviarEmailsAsync(id, User.UsuarioId(), cancellationToken);
}
