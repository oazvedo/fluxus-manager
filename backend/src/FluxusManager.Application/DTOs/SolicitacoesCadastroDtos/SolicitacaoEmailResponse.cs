using FluxusManager.Domain.Entities;

namespace FluxusManager.Application.DTOs.SolicitacoesCadastroDtos;

/// <summary>Status "Pendente" inclui o envio em andamento.</summary>
public record SolicitacaoEmailResponse(string Tipo, string Status, int Tentativas, DateTime? UltimaTentativaEm);
