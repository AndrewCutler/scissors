using Xunit;
using Scissors.Services;

namespace Scissors.Desktop.Tests;

public class AuthTokenRefreshServiceTests
{
    [Fact]
    public async Task RefreshIfNeededReturnsFalseWhenTheUserIsNotAuthenticated()
    {
        var session = new AuthSession();
        var api = new FakeScissorsApiClient();
        var store = new FakeRefreshTokenStore();
        var service = new AuthTokenRefreshService(session, api, store, TestLogger.Create<AuthTokenRefreshService>());

        try
        {
            var refreshed = await service.RefreshIfNeededAsync(force: true);

            Assert.False(refreshed);
        }
        finally
        {
            service.Dispose();
        }
    }

    [Fact]
    public async Task RefreshIfNeededReturnsFalseWhenTheRefreshTokenIsMissing()
    {
        var session = new AuthSession();
        session.SetToken("access-token");
        session.SetExpiresAt(DateTime.UtcNow.AddHours(1));
        var api = new FakeScissorsApiClient();
        var store = new FakeRefreshTokenStore();
        var service = new AuthTokenRefreshService(session, api, store, TestLogger.Create<AuthTokenRefreshService>());

        try
        {
            var refreshed = await service.RefreshIfNeededAsync(force: true);

            Assert.False(refreshed);
        }
        finally
        {
            service.Dispose();
        }
    }

    [Fact]
    public async Task RefreshIfNeededReturnsFalseWhenTheApiRejectsTheRefreshToken()
    {
        var session = new AuthSession();
        session.SetToken("access-token");
        session.SetExpiresAt(DateTime.UtcNow.AddHours(1));
        var api = new FakeScissorsApiClient();
        var store = new FakeRefreshTokenStore { Value = "refresh-token" };
        var service = new AuthTokenRefreshService(session, api, store, TestLogger.Create<AuthTokenRefreshService>());

        try
        {
            var refreshed = await service.RefreshIfNeededAsync(force: true);

            Assert.False(refreshed);
            Assert.Equal("refresh-token", api.RefreshTokenRequested);
        }
        finally
        {
            service.Dispose();
        }
    }

    [Fact]
    public async Task RefreshIfNeededUpdatesTheSessionAndStoresTheNewRefreshToken()
    {
        var session = new AuthSession();
        session.SetToken("access-token");
        session.SetExpiresAt(DateTime.UtcNow.AddHours(1));
        var api = new FakeScissorsApiClient
        {
            RefreshTokenResult = new GetRefreshTokenResponseDTO
            {
                AccessToken = "new-access-token",
                RefreshToken = "new-refresh-token",
                AccessTokenExpiresAt = DateTime.UtcNow.AddHours(2),
            }
        };
        var store = new FakeRefreshTokenStore { Value = "refresh-token" };
        var service = new AuthTokenRefreshService(session, api, store, TestLogger.Create<AuthTokenRefreshService>());

        try
        {
            var refreshed = await service.RefreshIfNeededAsync(force: true);

            Assert.True(refreshed);
            Assert.Equal("new-access-token", session.AccessToken);
            Assert.Equal("new-refresh-token", store.Value);
            Assert.Contains("new-refresh-token", store.SavedTokens);
        }
        finally
        {
            service.Dispose();
        }
    }
}
