namespace FluxusManager.Application.DTOs.EmpresasDtos;

/// <summary>O CNPJ pode vir com ou sem pontuação; a empresa nasce ativa.</summary>
public record CriarEmpresaRequest(string RazaoSocial, string? NomeFantasia, string Cnpj);
