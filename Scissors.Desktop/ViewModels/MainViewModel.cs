using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Scissors.Configuration;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;

namespace Scissors.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    // TODO: factory
    private static readonly HttpClient HttpClient = new();
    private readonly AuthSession _authSession;
    private readonly IRefreshTokenStore _refreshTokenStore;

    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";

    [ObservableProperty]
    public partial string? LastSendStatus { get; set; }

    [ObservableProperty]
    public partial string? DebugMessage { get; set; }

    public ObservableCollection<ClipboardEntry> ClipboardEntries { get; } = new();

    private readonly DesktopAppSettings _settings;

    public MainViewModel(DesktopAppSettings settings, AuthSession authSession, IRefreshTokenStore refreshTokenStore)
    {
        _authSession = authSession;
        _refreshTokenStore = refreshTokenStore;
        _settings = settings;
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
            Console.WriteLine(ex);
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

        if (!Uri.TryCreate(_settings.ApiUrl + "/clippings", UriKind.Absolute, out var endpoint))
        {
            LastSendStatus = "Invalid endpoint.";
            return;
        }

        try
        {
            var payload = new
            {
                capturedAt = latest.CapturedAt,
                text = latest.Text,
            };

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authSession.AccessToken);
            using var response = await HttpClient.PostAsJsonAsync(endpoint, payload);
            response.EnsureSuccessStatusCode();

            LastSendStatus = $"Sent {latest.CapturedAtText}.";
        }
        catch (Exception ex)
        {
            LastSendStatus = $"Send failed: {ex.Message}";
        }
    }

    // TODO: better name
    private async Task SendAuthenticationRequestAsync(string code)
    {
        if (!Uri.TryCreate(_settings.ApiUrl + "/auth/google", UriKind.Absolute, out var endpoint))
        {
            Console.WriteLine("uh oh");
            return;
        }

        try
        {
            var payload = new
            {
                Code = code,
            };

            using var response = await HttpClient.PostAsJsonAsync(endpoint, payload);
            response.EnsureSuccessStatusCode();

            // TODO: handle response from API and update UI
            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<GoogleAuthResponseDTO>(content);
            _authSession.SetToken(tokenResponse?.AccessToken);
            _authSession.SetExpiresAt(tokenResponse?.AccessTokenExpiresAt);
            await _refreshTokenStore.SaveAsync(tokenResponse?.RefreshToken ?? throw new ArgumentNullException("refreshToken"));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
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
