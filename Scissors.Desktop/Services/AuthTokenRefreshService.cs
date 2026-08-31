using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Scissors.Services;

public sealed class AuthTokenRefreshService : IAuthTokenRefreshService, IDisposable
{
    private static readonly TimeSpan RefreshLeadTime = TimeSpan.FromMinutes(5);

    private readonly AuthSession _authSession;
    private readonly IScissorsApiClient _apiClient;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IDeviceStorage _deviceStorage;
    private readonly ILogger<AuthTokenRefreshService> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly Timer _refreshTimer;
    private bool _disposed;

    public AuthTokenRefreshService(
        AuthSession authSession,
        IScissorsApiClient apiClient,
        IRefreshTokenStore refreshTokenStore,
        IDeviceStorage deviceStorage,
        ILogger<AuthTokenRefreshService> logger)
    {
        _authSession = authSession;
        _apiClient = apiClient;
        _refreshTokenStore = refreshTokenStore;
        _deviceStorage = deviceStorage;
        _logger = logger;

        _refreshTimer = new Timer(OnRefreshTimerElapsed);
        _authSession.PropertyChanged += OnAuthSessionPropertyChanged;
        ScheduleNextRefresh();
    }

    public async Task<bool> RefreshIfNeededAsync(bool force = false)
    {
        if (_disposed)
        {
            return false;
        }

        if (!_authSession.IsAuthenticated || _authSession.ExpiresAt is null)
        {
            return false;
        }

        var timeUntilRefresh = _authSession.ExpiresAt.Value.ToUniversalTime() - DateTime.UtcNow - RefreshLeadTime;
        if (!force && timeUntilRefresh > TimeSpan.Zero)
        {
            return false;
        }

        await _refreshLock.WaitAsync();
        try
        {
            if (_disposed)
            {
                return false;
            }

            if (!_authSession.IsAuthenticated || _authSession.ExpiresAt is null)
            {
                return false;
            }

            if (!force)
            {
                timeUntilRefresh = _authSession.ExpiresAt.Value.ToUniversalTime() - DateTime.UtcNow - RefreshLeadTime;
                if (timeUntilRefresh > TimeSpan.Zero)
                {
                    return false;
                }
            }

            var refreshToken = await _refreshTokenStore.GetAsync();
            if (refreshToken is null)
            {
                _logger.LogWarning("No refresh token is available for auth refresh.");
                return false;
            }

            var deviceId = await _deviceStorage.GetDeviceIdAsync() ?? await _deviceStorage.SetDeviceIdAsync();
            var tokenResponse = await _apiClient.GetRefreshTokenAsync(refreshToken, deviceId.ToString("D"));
            if (tokenResponse is null)
            {
                _logger.LogWarning("Refresh token was rejected by the API.");
                return false;
            }

            _authSession.SetToken(tokenResponse.AccessToken);
            _authSession.SetExpiresAt(tokenResponse.AccessTokenExpiresAt);
            await _refreshTokenStore.SaveAsync(tokenResponse.RefreshToken);

            _logger.LogInformation("Desktop access token refreshed.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh the desktop access token.");
            return false;
        }
        finally
        {
            ScheduleNextRefresh();
            _refreshLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _authSession.PropertyChanged -= OnAuthSessionPropertyChanged;
        _refreshTimer.Dispose();
        _refreshLock.Dispose();
    }

    private void OnAuthSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AuthSession.AccessToken) or nameof(AuthSession.ExpiresAt) or nameof(AuthSession.IsAuthenticated))
        {
            ScheduleNextRefresh();
        }
    }

    private void OnRefreshTimerElapsed(object? state)
    {
        _ = RefreshIfNeededAsync(force: true);
    }

    private void ScheduleNextRefresh()
    {
        if (_disposed)
        {
            return;
        }

        if (!_authSession.IsAuthenticated || _authSession.ExpiresAt is null)
        {
            _refreshTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            return;
        }

        var delay = _authSession.ExpiresAt.Value.ToUniversalTime() - DateTime.UtcNow - RefreshLeadTime;
        if (delay < TimeSpan.Zero)
        {
            delay = TimeSpan.Zero;
        }

        _refreshTimer.Change(delay, Timeout.InfiniteTimeSpan);
    }
}
