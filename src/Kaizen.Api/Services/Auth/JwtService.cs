using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kaizen.Api.Services.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Kaizen.Api.Services.Auth;

public class JwtService : IJwtService
{
    private readonly JwtOptions _opt;
    public JwtService(IOptions<JwtOptions> opt) => _opt = opt.Value;

    public (string token, DateTime expiraEm) GerarToken(int consultorId, string nome, string email)
    {
        var expiraEm = DateTime.UtcNow.AddMinutes(_opt.ExpiresMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, consultorId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Name, nome),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: expiraEm,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expiraEm);
    }
}
