using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Scissors.API.Configuration;
using Scissors.API.Handlers.Auth;
using Scissors.API.Tests.Infrastructure;
using Xunit;

namespace Scissors.API.Tests;

public class MobileRefreshTokenHandlerTests
{
    [Theory]
    [InlineData("missing")]
    [InlineData("revoked")]
    [InlineData("expired")]
    public async Task RejectsInvalidRefreshTokens(string scenario)
    {
        using var db = ApiTestHelpers.CreateDbContext();
        var settings = CreateSettings();
        var refreshToken = "refresh-token";

        if (scenario is not "missing")
        {
            var token = new RefreshToken
            {
                UserId = 7,
                TokenHash = Hash(refreshToken),
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                ExpiresAt = scenario == "expired"
                    ? DateTimeOffset.UtcNow.AddMinutes(-1)
                    : DateTimeOffset.UtcNow.AddDays(1),
                RevokedAt = scenario == "revoked" ? DateTimeOffset.UtcNow : null,
            };
            db.RefreshTokens.Add(token);
            await db.SaveChangesAsync();
        }

        var result = await MobileRefreshTokenHandler.Handle(
            new GetMobileRefreshTokenRequestDTO { RefreshToken = refreshToken },
            db,
            settings);

        var (statusCode, _, _) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
    }

    [Fact]
    public async Task RotatesTheRefreshTokenAndReturnsANewAccessToken()
    {
        using var db = ApiTestHelpers.CreateDbContext();
        var settings = CreateSettings();
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

        var result = await MobileRefreshTokenHandler.Handle(
            new GetMobileRefreshTokenRequestDTO { RefreshToken = refreshToken },
            db,
            settings);

        var (statusCode, body, _) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status200OK, statusCode);

        var dto = System.Text.Json.JsonSerializer.Deserialize<GetMobileRefreshTokenResponseDTO>(body, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(dto.RefreshToken));
        Assert.True(dto.AccessTokenExpiresAt >= before.AddMinutes(14));
        Assert.True(dto.AccessTokenExpiresAt <= before.AddMinutes(16));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(dto.AccessToken);
        Assert.Equal("42", jwt.Subject);

        var tokens = await db.RefreshTokens.OrderBy(token => token.Id).ToListAsync();
        Assert.Equal(2, tokens.Count);
        Assert.NotNull(tokens[0].RevokedAt);
        Assert.Equal(42, tokens[1].UserId);
        Assert.NotEqual(Hash(refreshToken), tokens[1].TokenHash);
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
