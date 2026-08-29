using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scissors.API.Configuration;
using Scissors.API.Data;
using Scissors.API.Models.Entities;
using Serilog;

namespace Scissors.API.Handlers.Auth;

public static class WebRefreshTokenHandler
{
    public static async Task<IResult> Handle(
        HttpContext context,
        ScissorsDbContext db,
        ApiAppSettings appSettings)
    {
        if (!context.Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
        {
            Log.Information("Refresh failed: no refresh token found in cookies.");
            return Results.Unauthorized();
        }

        Console.WriteLine(refreshToken);

        var refreshTokenHash = Convert.ToBase64String(SHA256.HashData(
                Encoding.UTF8.GetBytes(refreshToken)));

        var tokenFromStorage = await db.RefreshTokens
            .SingleOrDefaultAsync(rt => rt.TokenHash == refreshTokenHash);

        if (tokenFromStorage is null)
        {
            Log.Information("Refresh failed: no refresh token found.");
            return Results.Unauthorized();
        }

        if (tokenFromStorage.RevokedAt is not null)
        {
            Log.Information("Refresh failed: refresh token is revoked.");
            return Results.Unauthorized();
        }

        if (tokenFromStorage.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            Log.Information("Refresh failed: refresh token is expired.");
            return Results.Unauthorized();
        }

        tokenFromStorage.RevokedAt = DateTimeOffset.UtcNow;

        // TODO: move to method and update auth/google route too
        var claims = new[]
        {
            // TODO: custom userId claim
            new Claim(JwtRegisteredClaimNames.Sub, tokenFromStorage.UserId.ToString())
        };
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(appSettings.Jwt.Secret));
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: appSettings.Jwt.Issuer,
            audience: appSettings.Jwt.Audience,
            claims: claims,
            expires: expiresAt.DateTime,
            signingCredentials: credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshTokenBytes = RandomNumberGenerator.GetBytes(64);
        var newRefreshToken = Convert.ToBase64String(refreshTokenBytes);

        var newRefreshTokenHash = Convert.ToBase64String(SHA256.HashData(
                Encoding.UTF8.GetBytes(newRefreshToken)));

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = tokenFromStorage.UserId,
            TokenHash = newRefreshTokenHash,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        });

        await db.SaveChangesAsync();

        context.Response.Cookies.Append("refreshToken", newRefreshTokenHash, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth"
        });

        return Results.Ok(new GetWebRefreshTokenResponseDTO
        {
            AccessToken = jwt,
            AccessTokenExpiresAt = expiresAt,
        });
    }
}
