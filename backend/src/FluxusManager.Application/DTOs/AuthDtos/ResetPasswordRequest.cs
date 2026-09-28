namespace FluxusManager.Application.DTOs.AuthDtos;

public record ResetPasswordRequest(string Token, string NovaSenha);
