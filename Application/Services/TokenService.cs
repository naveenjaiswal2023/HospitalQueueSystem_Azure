using HospitalQueueSystem.Application.DTO;
using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.Infrastructure.Data;
using Jose;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HospitalQueueSystem.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtDto _jwt;
        private readonly UserManager<ApplicationUser> _userManager;

        public TokenService(IOptions<JwtDto> jwtOptions, UserManager<ApplicationUser> userManager)
        {
            _jwt = jwtOptions.Value;
            _userManager = userManager;
        }

        public async Task<TokenDto> GenerateToken(ApplicationUser user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
    };

            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Optional: generate refresh token here if needed
            //var refreshToken = GenerateRefreshToken();

            return new TokenDto
            {
                Token = tokenString,
                Expiration = expires,
                UserId = user.Id,
                Role = userRoles.FirstOrDefault() ?? string.Empty,
                //RefreshToken = refreshToken
            };
        }

    }

}
