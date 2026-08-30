using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
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
using Scissors.API.Models.Entities;

namespace Scissors.API.Handlers.Auth;

public static class CompleteGoogleDesktopOAuthHandler
{
    public static async Task<IResult> Handle(
        [FromBody] CompleteGoogleOAuthDesktopRequestDTO dto,
        ScissorsDbContext db,
        IConfigurationManager<OpenIdConnectConfiguration> configurationManager,
        IHttpClientFactory httpClientFactory,
        ApiAppSettings appSettings)
    {
        var httpClient = httpClientFactory.CreateClient();

        var response = await httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = dto.Code,
                ["client_id"] = appSettings.OAuth.Google.Desktop.ClientId,
                ["client_secret"] = appSettings.OAuth.Google.Desktop.ClientSecret,
                ["redirect_uri"] = appSettings.OAuth.Google.Desktop.RedirectUri,
                ["grant_type"] = "authorization_code",
                ["code_verifier"] = dto.CodeVerifier
            }));

        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponseDTO>();

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
            ValidAudience = appSettings.OAuth.Google.Desktop.ClientId,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        var principal = handler.ValidateToken(
            tokenResponse?.IdToken,
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
}
