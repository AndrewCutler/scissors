using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Scissors.Configuration;
using Scissors.Services;

namespace Scissors.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly IScissorsApiClient _apiClient;
    private readonly AuthSession _authSession;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly DesktopAppSettings _settings;

    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";

    [ObservableProperty]
    public partial string? LastSendStatus { get; set; }

    [ObservableProperty]
    public partial string? DebugMessage { get; set; }

    public ObservableCollection<ClipboardEntry> ClipboardEntries { get; } = new();

    public MainViewModel(
        DesktopAppSettings settings,
        ILogger<MainViewModel> logger,
        IScissorsApiClient apiClient,
        AuthSession authSession,
        IRefreshTokenStore refreshTokenStore)
    {
        _settings = settings;
        _logger = logger;
        _apiClient = apiClient;
        _authSession = authSession;
        _refreshTokenStore = refreshTokenStore;
    }

    public void AddClipboardText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var latest = ClipboardEntries.FirstOrDefault();
        if (string.Equals(latest?.Text, text))
        {
            return;
        }

        ClipboardEntries.Insert(0, new ClipboardEntry(DateTimeOffset.UtcNow, text));
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
            // TODO: configurable
            var authUrl =
                $"https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={Uri.EscapeDataString(_settings.OAuth.Google.ClientId)}" +
                $"&response_type=code" +
                $"&scope={Uri.EscapeDataString("openid email profile")}" +
                $"&redirect_uri={Uri.EscapeDataString(_settings.OAuth.Google.RedirectUri)}" +
                $"&state={Uri.EscapeDataString(state)}";
            // @$"https://accounts.google.com/o/oauth2/v2/auth
            //     ?client_id={_settings.GoogleOAuth.ClientId}
            //     &response_type=code
            //     &scope=openid%20email%20profile
            //     &redirect_uri={_settings.GoogleOAuth.RedirectUri}
            //     &state={state}
            //     &code_challenge=YOUR_CODE_CHALLENGE
            //     &code_challenge_method=S256"

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

            // send code to API
            // return code;
            await SendAuthenticationRequestAsync(code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google OAuth flow failed.");
        }
    }

    public async Task SendLatestClipboardAsync()
    {
        var latest = ClipboardEntries.FirstOrDefault();
        if (latest is null)
        {
            LastSendStatus = "Nothing to send yet.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_authSession.AccessToken))
        {
            LastSendStatus = "You are not signed in.";
            _logger.LogWarning("Attempted to send clipboard content without an access token.");
            return;
        }

        try
        {
            var sent = await _apiClient.SendClippingAsync(_authSession.AccessToken, latest.CapturedAt, latest.Text);
            LastSendStatus = sent ? $"Sent {latest.CapturedAtText}." : "Send failed.";
            if (sent)
            {
                _logger.LogInformation("Sent clipboard captured at {CapturedAt}.", latest.CapturedAt);
            }
            else
            {
                _logger.LogWarning("The API rejected a clipboard send for content captured at {CapturedAt}.", latest.CapturedAt);
            }
        }
        catch (Exception ex)
        {
            LastSendStatus = $"Send failed: {ex.Message}";
            _logger.LogError(ex, "Failed to send clipboard captured at {CapturedAt}.", latest.CapturedAt);
        }
    }

    // TODO: better name
    private async Task SendAuthenticationRequestAsync(string code)
    {
        try
        {
            var tokenResponse = await _apiClient.CompleteGoogleOAuthAsync(code);
            if (tokenResponse is null)
            {
                _logger.LogWarning("Google auth request returned no token response.");
                return;
            }

            _authSession.SetToken(tokenResponse?.AccessToken);
            _authSession.SetExpiresAt(tokenResponse?.AccessTokenExpiresAt);
            await _refreshTokenStore.SaveAsync(tokenResponse?.RefreshToken ?? throw new ArgumentNullException("refreshToken"));
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
}

public sealed record ClipboardEntry
{
    public ClipboardEntry(DateTimeOffset capturedAt, string text)
    {
        CapturedAt = capturedAt;
        Text = text;
    }

    public DateTimeOffset CapturedAt { get; }

    public string CapturedAtText => CapturedAt.ToString("HH:mm:ss");

    public string Text { get; }
}
