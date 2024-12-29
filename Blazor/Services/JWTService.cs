namespace Blazor.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Domain_Models;

public class JWTService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationDays;

    public JWTService(IConfiguration configuration)
    {
        if (configuration["JWT:SecretKey"] == null)
            throw new ArgumentNullException("JWT:SecretKey is not set in the configuration");

        _secretKey = configuration["JWT:SecretKey"];
        _issuer = configuration["JWT:Issuer"] ?? "BlazorApp";
        _audience = configuration["JWT:Audience"] ?? "BlazorUsers";
        _expirationDays = int.Parse(configuration["JWT:ExpirationDays"] ?? "7");
    }

    public string GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);

        // Konverter RoleId til rollenavn
        var roleName = user.RoleId switch
        {
            1 => "User",
            2 => "Admin",
            3 => "Dev",
            _ => "User"
        };

        Console.WriteLine($"Generating token for user {user.Username} with role {roleName} (RoleId: {user.RoleId})");

        var claims = new List<Claim>
        {
            // Bruger identifikation
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, roleName),
            
            // Bruger detaljer
            new Claim("firstName", user.FirstName ?? string.Empty),
            new Claim("lastName", user.LastName ?? string.Empty),
            new Claim("isActive", user.IsActive.ToString()),
            
            // Token metadata
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("roleId", user.RoleId.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(_expirationDays),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_secretKey);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public bool IsTokenExpired(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        return jwtToken.ValidTo < DateTime.UtcNow;
    }

    public string GetUserRole(string token)
    {
        var principal = ValidateToken(token);
        if (principal == null) return "User";

        var roleClaim = principal.FindFirst(ClaimTypes.Role);
        return roleClaim?.Value ?? "User";
    }

    public Role GetUserRoleEnum(string token)
    {
        var roleName = GetUserRole(token);
        return roleName switch
        {
            "Admin" => Role.Admin,
            "Dev" => Role.Dev,
            _ => Role.User
        };
    }
}