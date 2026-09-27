using FluxusManager.Domain.Entities;

namespace FluxusManager.Application.DTOs.FiliaisDtos;

public record FilialResponse(Guid Id, string Nome, string Cnpj, string Endereco, bool Ativo, DateTime CriadoEm, DateTime? AtualizadoEm)
{
    public static FilialResponse DeEntidade(Filial filial) => new(filial.Id, filial.Nome, filial.Cnpj, filial.Endereco, filial.Ativo, filial.CriadoEm, filial.AtualizadoEm);
}
