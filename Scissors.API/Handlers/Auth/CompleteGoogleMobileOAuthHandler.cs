using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Scissors.API.Configuration;
using Scissors.API.Data;

namespace Scissors.API.Handlers.Auth;

public static class CompleteGoogleMobileOAuthHandler
{
    public static async Task<IResult> Handle(
        [FromBody] CompleteGoogleOAuthMobileRequestDTO dto,
        ScissorsDbContext db,
        IConfigurationManager<OpenIdConnectConfiguration> configurationManager,
        IHttpClientFactory httpClientFactory,
        ApiAppSettings appSettings)
    {
        var httpClient = httpClientFactory.CreateClient();

        var handler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false,
        };
        var configuration = await configurationManager.GetConfigurationAsync(CancellationToken.None);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://accounts.google.com",
            ValidateAudience = true,
            ValidAudience = appSettings.OAuth.Google.Mobile.ClientId,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        var principal = handler.ValidateToken(
            dto.IdToken,
            validationParameters,
            out var validatedToken);

        var googleUserId = principal.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("Google user ID missing");

        var externalIdentity = await db.ExternalIdentities
            .SingleOrDefaultAsync(e => e.Subject == googleUserId && e.Provider == ExternalIdentityProvider.Google);

        int userId;
        if (externalIdentity is null)
        {
            var user = new User
            {
                ExternalIdentities = [new ExternalIdentity {
                    Provider = ExternalIdentityProvider.Google,
                    Subject = googleUserId,
                }]
            };
            db.Add(user);
            await db.SaveChangesAsync();

            userId = user.Id;
        }
        else
        {
            userId = externalIdentity.UserId;
        }

        await UpsertDeviceAsync(db, dto.Platform, userId, dto.DeviceId);

        var claims = new[]
        {
            // TODO: custom userId claim
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())
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
        var refreshToken = Convert.ToBase64String(refreshTokenBytes);

        var refreshTokenHash = Convert.ToBase64String(SHA256.HashData(
                Encoding.UTF8.GetBytes(refreshToken)));

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = refreshTokenHash,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        });

        await db.SaveChangesAsync();

        return Results.Ok(new AuthenticationResponseDTO
        {
            AccessToken = jwt,
            RefreshToken = refreshToken,
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
