using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Scissors.API.Configuration;
using Scissors.API.Handlers.Auth;
using Scissors.API.Models.Entities;
using Scissors.API.Tests.Infrastructure;
using Xunit;

namespace Scissors.API.Tests;

public class WebRefreshTokenHandlerTests
{
    [Fact]
    public async Task ReturnsUnauthorizedWhenTheRefreshTokenCookieIsMissing()
    {
        using var db = ApiTestHelpers.CreateDbContext();
        var context = new DefaultHttpContext();

        var result = await WebRefreshTokenHandler.Handle(
            context,
            db,
            CreateSettings());

        var (statusCode, _, _) = await ApiTestHelpers.ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status401Unauthorized, statusCode);
    }

    [Fact]
    public async Task RotatesTheRefreshTokenAndSetsTheHttpOnlyCookie()
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

        var context = new DefaultHttpContext
        {
            Response =
            {
                Body = new MemoryStream(),
            }
        };
        context.Request.Headers.Cookie = $"refreshToken={refreshToken}";

        var result = await WebRefreshTokenHandler.Handle(
            context,
            db,
            settings);

        var (statusCode, body, headers) = await ApiTestHelpers.ExecuteAsync(result, context);

        Assert.Equal(StatusCodes.Status200OK, statusCode);
        Assert.True(headers.TryGetValue("Set-Cookie", out var setCookie));
        Assert.Contains("refreshToken=", setCookie.ToString());

        var dto = JsonSerializer.Deserialize<GetWebRefreshTokenResponseDTO>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrWhiteSpace(dto!.AccessToken));
        Assert.True(dto.AccessTokenExpiresAt > DateTimeOffset.UtcNow);

        var tokens = await db.RefreshTokens.OrderBy(token => token.Id).ToListAsync();
        Assert.Equal(2, tokens.Count);
        Assert.NotNull(tokens[0].RevokedAt);
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
                    Web = new Web
                    {
                        ClientId = "web-client-id",
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
