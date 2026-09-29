using FluxusManager.Application.Interfaces;
using FluxusManager.Domain.Common;
using FluxusManager.Domain.Entities;
using FluxusManager.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FluxusManager.Infrastructure.Database;

/// <summary>
/// Único caminho para conceder ou revogar o papel de administrador da plataforma (além do seed local): exige acesso
/// ao servidor e à connection string, nunca a um endpoint. A alteração é auditada com o usuário "cli:plataforma".
/// </summary>
public static class PlataformaCommands
{
    public const string AuditUser = "cli:plataforma";

    /// <returns>Código de saída do processo: 0 sucesso, 1 erro de uso ou de dados.</returns>
    public static async Task<int> ExecutarComandoPlataformaAsync(this IServiceProvider services, string[] args,
        TextWriter? saida = null, CancellationToken cancellationToken = default)
    {
        saida ??= Console.Out;
        if (args is not [var comando and ("promover" or "revogar"), var email])
        {
            await saida.WriteLineAsync("Uso: plataforma promover <email> | plataforma revogar <email>");
            return 1;
        }

        await using var scope = services.CreateAsyncScope();
        var usuarios = scope.ServiceProvider.GetRequiredService<IUsuarioRepository>();
        var administradores = scope.ServiceProvider.GetRequiredService<IAdministradorPlataformaRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        scope.ServiceProvider.GetRequiredService<IAuditContext>().SetUser(AuditUser);

        var usuario = await usuarios.ObterPorEmailAsync(EnderecoEmail.Normalizar(email), cancellationToken);
        if (usuario is null)
        {
            await saida.WriteLineAsync("Usuário não encontrado. A pessoa precisa ter uma conta antes de ser promovida.");
            return 1;
        }

        var atual = await administradores.ObterPorUsuarioAsync(usuario.Id, cancellationToken);
        if (comando == "promover")
        {
            if (!usuario.Ativo)
            {
                await saida.WriteLineAsync("Usuário inativo: reative-o antes de promover.");
                return 1;
            }
            if (atual is null)
                administradores.Add(new AdministradorPlataforma(usuario.Id));
        }
        else if (atual is not null)
            administradores.Remove(atual);

        await unitOfWork.CommitAsync(cancellationToken);
        await saida.WriteLineAsync(comando == "promover"
            ? "Usuário é administrador da plataforma."
            : "Usuário não é mais administrador da plataforma.");
        return 0;
    }
}
