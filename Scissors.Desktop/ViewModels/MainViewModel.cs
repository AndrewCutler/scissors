using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Linq;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Scissors.Configuration;
using Scissors.Services;

namespace Scissors.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly IScissorsApiClient _apiClient;
    private readonly IClippingService _clippingService;
    private readonly IClippingHubConnectionService _clippingHubConnectionService;
    private readonly IClippingStore _clippingStore;
    private readonly AuthSession _authSession;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly DesktopAppSettings _settings;

    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";

    [ObservableProperty]
    public partial string? LastSendStatus { get; set; }

    [ObservableProperty]
    public partial string? DebugMessage { get; set; }

    public ReadOnlyObservableCollection<Clipping> Clippings => _clippingStore.Clippings;

    public bool IsAuthenticated => _authSession.IsAuthenticated;
    public bool CanContinueWithGoogle => !_authSession.IsAuthenticated;

    public MainViewModel(
        DesktopAppSettings settings,
        ILogger<MainViewModel> logger,
        IScissorsApiClient apiClient,
        IClippingService clippingService,
        IClippingHubConnectionService clippingHubConnectionService,
        IClippingStore clippingStore,
        AuthSession authSession,
        IRefreshTokenStore refreshTokenStore)
    {
        _settings = settings;
        _logger = logger;
        _apiClient = apiClient;
        _clippingService = clippingService;
        _clippingHubConnectionService = clippingHubConnectionService;
        _clippingStore = clippingStore;
        _authSession = authSession;
        _refreshTokenStore = refreshTokenStore;
        _authSession.PropertyChanged += OnAuthSessionPropertyChanged;
    }

    public async Task StartGoogleOAuthAsync()
    {
        try
        {
            _logger.LogInformation("Starting Google OAuth flow.");
            using var listener = new HttpListener();
            listener.Prefixes.Add(_settings.OAuth.Google.RedirectUri);
            listener.Start();

            var state = OAuthUtility.GenerateState();
            var verifier = OAuthUtility.GenerateCodeVerifier();
            var codeChallenge = OAuthUtility.GenerateCodeChallenge(verifier);
            var authUrl =
                $"https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={Uri.EscapeDataString(_settings.OAuth.Google.ClientId)}" +
                $"&response_type=code" +
                $"&code_challenge_method=S256" +
                $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
                $"&scope={Uri.EscapeDataString("openid email profile")}" +
                $"&redirect_uri={Uri.EscapeDataString(_settings.OAuth.Google.RedirectUri)}" +
                $"&state={Uri.EscapeDataString(state)}";

            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = authUrl
            });

            var context = await listener.GetContextAsync();

            var code = GetQueryValue(context.Request.Url!, "code") ?? throw new InvalidOperationException("Google code is missing.");
            var returnedState = GetQueryValue(context.Request.Url!, "state");

            if (!string.Equals(state, returnedState, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("OAuth state mismatch.");
            }

            var responseHtml = @"
                <html>
                <body>
                    <h2>Login complete</h2>
                    <p>You can close this window and return to Scissors.</p>
                </body>
                </html>
                ";

            var buffer = Encoding.UTF8.GetBytes(responseHtml);
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.Close();

            await SendAuthenticationRequestAsync(code: code, codeVerifier: verifier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google OAuth flow failed.");
        }
    }

    public void CaptureClipboardText(string? text)
    {
        if (!_authSession.IsAuthenticated)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (text == Clippings.FirstOrDefault()?.Text)
        {
            return;
        }

        var clipping = Clipping.FromPaste(DateTimeOffset.UtcNow, text);
        _clippingStore.Add(clipping);
    }

    public async Task SendClippingAsync(Clipping clipping)
    {
        await _clippingService.SaveClippingAsync(clipping);
    }

    public async Task DeleteClippingAsync(Clipping clipping)
    {
        await _clippingService.DeleteClippingAsync(clipping.Id ?? throw new InvalidOperationException("Cannot delete clipping with no Id."));
    }

    private async Task SendAuthenticationRequestAsync(string code, string codeVerifier)
    {
        try
        {
            var tokenResponse = await _apiClient.CompleteGoogleOAuthAsync(code: code, codeVerifier: codeVerifier);
            if (tokenResponse is null)
            {
                _logger.LogWarning("Google auth request returned no token response.");
                return;
            }

            _authSession.SetToken(tokenResponse.AccessToken);
            _authSession.SetExpiresAt(tokenResponse.AccessTokenExpiresAt);
            await _refreshTokenStore.SaveAsync(tokenResponse.RefreshToken ?? throw new ArgumentNullException("refreshToken"));
            var clippings = await _clippingService.GetClippingsAsync();
            _clippingStore.Init(clippings);

            try
            {
                await _clippingHubConnectionService.StartAsync();
            }
            catch (Exception hubEx)
            {
                _logger.LogWarning(hubEx, "Failed to start the clipping hub connection after login.");
            }

            _logger.LogInformation("Google authentication completed and session tokens were updated.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete Google authentication.");
        }
    }

    private static string? GetQueryValue(Uri uri, string key)
    {
        foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split('=', 2);
            var name = Uri.UnescapeDataString(pieces[0]);
            if (!string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
                continue;

            return pieces.Length > 1 ? Uri.UnescapeDataString(pieces[1]) : "";
        }

        return null;
    }

    private void OnAuthSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AuthSession.AccessToken) or nameof(AuthSession.IsAuthenticated))
        {
            OnPropertyChanged(nameof(IsAuthenticated));
            OnPropertyChanged(nameof(CanContinueWithGoogle));

            if (!_authSession.IsAuthenticated)
            {
                _ = StopClippingHubConnectionAsync();
                _clippingStore.Reset();
            }
        }
    }

    private async Task StopClippingHubConnectionAsync()
    {
        try
        {
            await _clippingHubConnectionService.StopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop the clipping hub connection.");
        }
    }
}
