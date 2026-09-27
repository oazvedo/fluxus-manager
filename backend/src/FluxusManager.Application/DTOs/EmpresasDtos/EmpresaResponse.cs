using FluxusManager.Domain.Entities;

namespace FluxusManager.Application.DTOs.EmpresasDtos;

/// <summary>Dados da empresa devolvidos pela API. O CNPJ sai sem pontuação.</summary>
public record EmpresaResponse(
    Guid Id,
    string RazaoSocial,
    string? NomeFantasia,
    string Cnpj,
    bool Ativo,
    DateTime CriadoEm,
    DateTime? AtualizadoEm)
{
    public static EmpresaResponse DeEntidade(Empresa empresa) => new(
        empresa.Id,
        empresa.RazaoSocial,
        empresa.NomeFantasia,
        empresa.Cnpj,
        empresa.Ativo,
        empresa.CriadoEm,
        empresa.AtualizadoEm);
}
