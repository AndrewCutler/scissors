using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Scissors.Configuration;

namespace Scissors.Services;

public sealed class ClippingHubConnectionService : IClippingHubConnectionService
{
    private readonly DesktopAppSettings _settings;
    private readonly AuthSession _authSession;
    private readonly IClippingStore _clippingStore;
    private readonly ILogger<ClippingHubConnectionService> _logger;
    private readonly HubConnection _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private volatile bool _allowReconnect;

    public ClippingHubConnectionService(
        DesktopAppSettings settings,
        AuthSession authSession,
        IClippingStore clippingStore,
        ILogger<ClippingHubConnectionService> logger)
    {
        _settings = settings;
        _authSession = authSession;
        _clippingStore = clippingStore;
        _logger = logger;

        _connection = new HubConnectionBuilder()
            .WithUrl(BuildHubUri(_settings.ApiUrl), options =>
            {
                options.AccessTokenProvider = async () => _authSession.AccessToken;
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<ClippingResponseDTO>("NewClipping", clippingDto =>
        {
            var clipping = Clipping.FromDTO(clippingDto);

            Dispatcher.UIThread.Post(() =>
            {
                _clippingStore.Add(clipping);
            });
        });

        _connection.Reconnecting += error =>
        {
            _logger.LogWarning(error, "Clipping hub connection is reconnecting.");
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            _logger.LogInformation("Clipping hub connection reconnected with connection id {ConnectionId}.", connectionId);
            return Task.CompletedTask;
        };

        _connection.Closed += async error =>
        {
            if (!_allowReconnect || !_authSession.IsAuthenticated)
            {
                _logger.LogInformation("Clipping hub connection closed.");
                return;
            }

            _logger.LogWarning(error, "Clipping hub connection closed. Retrying in 5 seconds.");

            while (_authSession.IsAuthenticated && _connection.State == HubConnectionState.Disconnected)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));

                if (!_authSession.IsAuthenticated)
                {
                    return;
                }

                try
                {
                    await StartAsync();
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Retrying clipping hub connection failed.");
                }
            }
        };
    }

    public async Task StartAsync()
    {
        if (!_authSession.IsAuthenticated)
        {
            throw new InvalidOperationException("Cannot connect to the clipping hub; the user is not authenticated.");
        }

        _allowReconnect = true;

        await _connectionLock.WaitAsync();
        try
        {
            if (_connection.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting)
            {
                return;
            }

            await ConnectWithRetryAsync();
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task StopAsync()
    {
        _allowReconnect = false;

        await _connectionLock.WaitAsync();
        try
        {
            if (_connection.State == HubConnectionState.Disconnected)
            {
                return;
            }

            await _connection.StopAsync();
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task ConnectWithRetryAsync()
    {
        Exception? lastError = null;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await _connection.StartAsync();
                _logger.LogInformation("Clipping hub connection started.");
                return;
            }
            catch (Exception ex)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Failed to start clipping hub connection on attempt {Attempt}.", attempt);

                if (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
        }

        throw new InvalidOperationException("Failed to connect to the clipping hub.", lastError);
    }

    private static Uri BuildHubUri(string apiUrl)
    {
        var apiUri = new Uri(apiUrl, UriKind.Absolute);
        var builder = new UriBuilder(apiUri)
        {
            Path = string.Empty,
            Query = string.Empty,
            Fragment = string.Empty,
        };

        return new Uri(builder.Uri, "clippingsHub");
    }
}
