using FluentValidation;
using FluxusManager.Application.DTOs.PerfisDtos;

namespace FluxusManager.Application.Validators.PerfisValidators;

public abstract class PerfilRequestValidator<TRequest> : AbstractValidator<TRequest> where TRequest : IPerfilRequest
{
    protected PerfilRequestValidator()
    {
        RuleFor(request => request.Nome).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Descricao).MaximumLength(250);
        RuleFor(request => request.Permissoes).NotNull().Must(lista => lista is not null && lista.Distinct(StringComparer.Ordinal).Count() == lista.Count)
            .WithMessage("A lista de permissões não pode conter itens repetidos.");
        RuleForEach(request => request.Permissoes).NotEmpty().MaximumLength(80);
    }
}

public sealed class CriarPerfilRequestValidator : PerfilRequestValidator<CriarPerfilRequest>
{
    public CriarPerfilRequestValidator() { }
}

public sealed class AtualizarPerfilRequestValidator : PerfilRequestValidator<AtualizarPerfilRequest>
{
    public AtualizarPerfilRequestValidator() { }
}
