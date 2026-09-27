namespace FluxusManager.Application.DTOs.UsuariosEmpresasDtos;

public record VincularUsuarioEmpresaRequest(Guid UsuarioId, Guid EmpresaId, Guid PerfilId);
