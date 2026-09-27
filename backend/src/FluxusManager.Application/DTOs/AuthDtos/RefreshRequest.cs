namespace FluxusManager.Application.DTOs.AuthDtos;

/// <summary>EmpresaId opcional permite manter o tenant escolhido após switch-tenant; o vínculo é revalidado.</summary>
public record RefreshRequest(string RefreshToken, Guid? EmpresaId = null);
