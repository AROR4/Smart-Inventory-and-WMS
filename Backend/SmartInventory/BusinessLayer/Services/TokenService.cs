using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartInventoryManagement.BusinessLayer.Interfaces;
using SmartInventoryManagement.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartInventoryManagement.BusinessLayer.Services
{
    public class TokenService : ITokenService
    {
        private readonly string _key;

        private readonly string _issuer;

        private readonly string _audience;

        private readonly int _durationInMinutes;

        public TokenService(IConfiguration configuration)
        {
            _key = configuration["JWT:Key"]
                ?? throw new Exception("JWT Key is missing.");

            _issuer = configuration["JWT:Issuer"]
                ?? throw new Exception("JWT Issuer is missing.");

            _audience = configuration["JWT:Audience"]
                ?? throw new Exception("JWT Audience is missing.");

            _durationInMinutes = Convert.ToInt32(
                configuration["JWT:DurationInMinutes"]);
        }

        public string GenerateToken(TokenRequest request)
        {
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    request.Id.ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    request.Id.ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Email,
                    request.Email),

                new Claim(
                    ClaimTypes.Name,
                    request.Name),

                new Claim(
                    ClaimTypes.Role,
                    request.Role)
            };

            if (request.AssignedWarehouseId.HasValue)
            {
                claims.Add(
                    new Claim(
                        "AssignedWarehouseId",
                        request.AssignedWarehouseId
                            .Value
                            .ToString()));
            }

            if (request.SupplierId.HasValue)
            {
                claims.Add(
                    new Claim(
                        "SupplierId",
                        request.SupplierId
                            .Value
                            .ToString()));
            }

            var securityKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_key));

            var credentials =
                new SigningCredentials(
                    securityKey,
                    SecurityAlgorithms.HmacSha256);

            var token =
                new JwtSecurityToken(
                    issuer: _issuer,
                    audience: _audience,
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(
                        _durationInMinutes),
                    signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
        public string GeneratePasswordSetupToken(
            int userId)
        {
            var claims = new List<Claim>
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    userId.ToString()),

                new Claim(
                    "Purpose",
                    "PasswordSetup")
            };

            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_key));

            var credentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }

        public ClaimsPrincipal ValidateToken(
        string token)
        {
            var tokenHandler =
                new JwtSecurityTokenHandler();

            var validationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = _issuer,
                    ValidAudience = _audience,

                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(_key))
                };

            return tokenHandler.ValidateToken(
                token,
                validationParameters,
                out _);
        }
    }
}