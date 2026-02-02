using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PortfolioBalance.Models;

namespace PortfolioBalance.Services;

public class AdvisorAuthService
{
    private readonly IConfiguration _configuration;

    public AdvisorAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == hashedPassword;
    }

    public string GenerateJwtToken(Advisor advisor)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"] ?? "PortfolioBalance";
        var audience = jwtSettings["Audience"] ?? "PortfolioBalanceUsers";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, advisor.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, advisor.Username),
            new Claim(JwtRegisteredClaimNames.Email, advisor.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("role", "advisor"),
            new Claim("isAdvisor", "true")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public int? GetAdvisorIdFromToken(ClaimsPrincipal user)
    {
        var advisorIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst(JwtRegisteredClaimNames.Sub);
        if (advisorIdClaim != null && int.TryParse(advisorIdClaim.Value, out int advisorId))
        {
            return advisorId;
        }
        return null;
    }

    public bool IsAdvisor(ClaimsPrincipal user)
    {
        var isAdvisorClaim = user.FindFirst("isAdvisor");
        return isAdvisorClaim != null && isAdvisorClaim.Value == "true";
    }
}
