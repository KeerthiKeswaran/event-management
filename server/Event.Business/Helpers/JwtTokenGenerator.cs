using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Event.Business.Helpers
{
    public class JwtTokenGenerator
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryHours;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            var section = configuration.GetSection("Jwt");
            _secretKey  = section["SecretKey"]  ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");
            _issuer     = section["Issuer"]     ?? "EventPlatform";
            _audience   = section["Audience"]   ?? "EventPlatformUsers";
            _expiryHours = int.Parse(section["ExpiryHours"] ?? "24");
        }

        public string GenerateUserToken(int userId, string email, string name)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub,   userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Name,  name),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "User")
            };
            return BuildToken(claims);
        }

        public string GenerateAdminToken(string adminId, string email, string name)
        {
            string role = adminId.StartsWith("FIN", StringComparison.OrdinalIgnoreCase) ? "finance" : "admin";
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub,   adminId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Name,  name),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            return BuildToken(claims);
        }

        private string BuildToken(IEnumerable<Claim> claims)
        {
            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer:             _issuer,
                audience:           _audience,
                claims:             claims,
                notBefore:          DateTime.UtcNow,
                expires:            DateTime.UtcNow.AddHours(_expiryHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
