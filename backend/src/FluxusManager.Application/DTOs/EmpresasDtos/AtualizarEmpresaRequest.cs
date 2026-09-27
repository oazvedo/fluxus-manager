namespace FluxusManager.Application.DTOs.EmpresasDtos;

/// <summary>CNPJ não é alterável; ativar/inativar têm endpoints próprios.</summary>
public record AtualizarEmpresaRequest(string RazaoSocial, string? NomeFantasia);
