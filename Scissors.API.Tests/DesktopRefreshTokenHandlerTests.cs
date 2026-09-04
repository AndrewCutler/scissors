/*
 * CODEX-GENERATED: the contents of this file were fully constructed by a Codex agent and not a human.
 */

using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Scissors.API.Configuration;
using Scissors.API.Handlers.Auth;
using Scissors.API.Tests.Infrastructure;
using Xunit;

namespace Scissors.API.Tests;

public class DesktopRefreshTokenHandlerTests
{
    [Fact]
    public async Task ReturnsUnauthorizedWhenTheRefreshTokenIsMissing()
    {
        using var db = ApiTestHelpers.CreateDbContext();
        var result = await DesktopRefreshTokenHandler.Handle(
            new GetDesktopRefreshTokenRequestDTO
            {
                RefreshToken = "missing-token",
                DeviceId = "desktop-device",
            },
            db,
            CreateSettings());

        var (statusCode, _, _) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
    }

    [Fact]
    public async Task RotatesTheRefreshTokenAndUpsertsTheDesktopDevice()
    {
        using var db = ApiTestHelpers.CreateDbContext();
        var refreshToken = "refresh-token";
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = 42,
            TokenHash = Hash(refreshToken),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
        });
        await db.SaveChangesAsync();
        var before = DateTimeOffset.UtcNow;

        var result = await DesktopRefreshTokenHandler.Handle(
            new GetDesktopRefreshTokenRequestDTO
            {
                RefreshToken = refreshToken,
                DeviceId = "desktop-device",
            },
            db,
            CreateSettings());

        var (statusCode, body, _) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status200OK, statusCode);

        var dto = JsonSerializer.Deserialize<GetMobileRefreshTokenResponseDTO>(
            body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(dto.RefreshToken));
        Assert.InRange(
            dto.AccessTokenExpiresAt,
            before.AddMinutes(14),
            before.AddMinutes(16));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(dto.AccessToken);
        Assert.Equal("42", jwt.Subject);

        var tokens = await db.RefreshTokens.OrderBy(token => token.Id).ToListAsync();
        Assert.Equal(2, tokens.Count);
        Assert.NotNull(tokens[0].RevokedAt);
        Assert.NotEqual(Hash(refreshToken), tokens[1].TokenHash);

        var device = await db.Devices.SingleAsync();
        Assert.Equal(42, device.UserId);
        Assert.Equal("desktop-device", device.DeviceId);
        Assert.Equal(Platform.Desktop, device.Platform);
        Assert.True(device.IsActive);
    }

    private static ApiAppSettings CreateSettings()
    {
        return new ApiAppSettings
        {
            ConnectionStrings = new ConnectionStrings
            {
                Postgres = "Host=localhost;Database=scissors"
            },
            OAuth = new OAuth
            {
                Google = new GoogleOAuthSettings
                {
                    Desktop = new Desktop
                    {
                        ClientId = "desktop-client-id",
                        ClientSecret = "desktop-secret",
                        RedirectUri = "http://localhost/callback",
                    },
                    Mobile = new Mobile
                    {
                        ClientId = "mobile-client-id",
                    }
                }
            },
            Jwt = new Jwt
            {
                Issuer = "issuer",
                Audience = "audience",
                Secret = "super-secret-super-secret-super-secret-1234",
            }
        };
    }

    private static string Hash(string value)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
