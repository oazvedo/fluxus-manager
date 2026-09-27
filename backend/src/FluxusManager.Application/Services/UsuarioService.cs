using FluxusManager.Application.DTOs.UsuariosDtos;
using FluxusManager.Application.Interfaces;
using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Exceptions;
using FluxusManager.Domain.Interfaces;

namespace FluxusManager.Application.Services;

public class UsuarioService(
    IUsuarioRepository usuarios,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher) : IUsuarioService
{
    public async Task<UsuarioResponse> CriarAsync(CriarUsuarioRequest request, CancellationToken cancellationToken = default)
    {
        var email = EnderecoEmail.Normalizar(request.Email);

        if (await usuarios.EmailEmUsoAsync(email, cancellationToken: cancellationToken))
            throw new ConflictException($"O e-mail '{email}' já está em uso.");

        var usuario = new Usuario(request.Nome.Trim(), email, passwordHasher.Hash(request.Senha));

        usuarios.Add(usuario);
        await unitOfWork.CommitAsync(cancellationToken);

        return UsuarioResponse.DeEntidade(usuario);
    }

    public async Task<UsuarioResponse> ObterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await BuscarAsync(id, cancellationToken);

        return UsuarioResponse.DeEntidade(usuario);
    }

    public async Task<PagedResult<UsuarioResponse>> ListarAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var pagina = await usuarios.ListAsync(page, pageSize, cancellationToken);

        return pagina.Map(UsuarioResponse.DeEntidade);
    }

    public async Task<UsuarioResponse> AtualizarAsync(Guid id, AtualizarUsuarioRequest request, CancellationToken cancellationToken = default)
    {
        var usuario = await BuscarAsync(id, cancellationToken);
        var email = EnderecoEmail.Normalizar(request.Email);

        if (await usuarios.EmailEmUsoAsync(email, ignorarId: id, cancellationToken))
            throw new ConflictException($"O e-mail '{email}' já está em uso.");

        usuario.Atualizar(request.Nome.Trim(), email);
        await unitOfWork.CommitAsync(cancellationToken);

        return UsuarioResponse.DeEntidade(usuario);
    }

    public async Task AtivarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await BuscarAsync(id, cancellationToken);

        usuario.Ativar();
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task InativarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await BuscarAsync(id, cancellationToken);

        usuario.Inativar();
        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task DesbloquearAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var usuario = await BuscarAsync(id, cancellationToken);

        usuario.Desbloquear();
        await unitOfWork.CommitAsync(cancellationToken);
    }

    private async Task<Usuario> BuscarAsync(Guid id, CancellationToken cancellationToken)
        => await usuarios.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Usuário", id);
}
