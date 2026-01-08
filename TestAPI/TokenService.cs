using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TestAPI.Models;

namespace TestAPI
{
    public interface ITokenService
    {
        string GenerateToken(User user, Role role);
    }

    public class TokenService : ITokenService
    {
        public string GenerateToken(User user, Role role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, role.RoleName)
            };

            var jwt = new JwtSecurityToken(
                issuer: AuthOptionns.ISSUER,
                audience: AuthOptionns.AUDIENCE,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: new SigningCredentials(AuthOptionns.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256)
                );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}
