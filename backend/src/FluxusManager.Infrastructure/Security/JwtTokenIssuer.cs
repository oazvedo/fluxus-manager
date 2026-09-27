using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluxusManager.Application.Interfaces;
using FluxusManager.Application.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FluxusManager.Infrastructure.Security;

public class JwtTokenIssuer(IOptions<JwtOptions> options) : IJwtTokenIssuer
{
    public (string Token, int ExpiresIn) Create(Guid userId, string email, Guid tenantId, string role, IReadOnlyCollection<string> permissions)
    {
        var settings = options.Value;
        var expires = DateTime.UtcNow.AddMinutes(settings.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("tenant_id", tenantId.ToString()),
            new("role", role)
        };
        claims.AddRange(permissions.Select(permission => new Claim("permissions", permission)));
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, expires: expires, signingCredentials: credentials);
        return (new JwtSecurityTokenHandler().WriteToken(jwt), (int)(expires - DateTime.UtcNow).TotalSeconds);
    }
}
