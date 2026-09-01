using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scissors.API.Configuration;
using Scissors.API.Data;
using Serilog;

namespace Scissors.API.Handlers.Auth;

public static class MobileRefreshTokenHandler
{
    public static async Task<IResult> Handle(
        [FromBody] GetMobileRefreshTokenRequestDTO dto,
        ScissorsDbContext db,
        ApiAppSettings appSettings)
    {
        var refreshTokenHash = Convert.ToBase64String(SHA256.HashData(
                Encoding.UTF8.GetBytes(dto.RefreshToken)));

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
        await UpsertDeviceAsync(db, dto.Platform, tokenFromStorage.UserId, dto.DeviceId);

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

        return Results.Ok(new GetMobileRefreshTokenResponseDTO
        {
            AccessToken = jwt,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresAt = expiresAt,
        });
    }

    private static async Task UpsertDeviceAsync(ScissorsDbContext db, Platform platform, int userId, string deviceId)
    {
        var now = DateTimeOffset.UtcNow;
        var device = await db.Devices.SingleOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId);

        if (device is null)
        {
            db.Devices.Add(new Device
            {
                UserId = userId,
                DeviceId = deviceId,
                Platform = platform,
                IsActive = true,
                LastSeenAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            });
            return;
        }

        device.Platform = platform;
        device.IsActive = true;
        device.LastSeenAt = now;
        device.UpdatedAt = now;
    }
}
