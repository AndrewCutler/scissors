using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Scissors.Configuration;
using System.Diagnostics;

namespace Scissors.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private static readonly HttpClient HttpClient = new();

    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";

    [ObservableProperty]
    public partial string? LastSendStatus { get; set; }

    private readonly DesktopAppSettings _settings;
    private string? _state { get; set; }

    public ObservableCollection<ClipboardEntry> ClipboardEntries { get; } = new();

    public MainViewModel()
        : this(DesktopAppSettings.CreateDesignTimeDefaults())
    {
    }

    public MainViewModel(DesktopAppSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
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
            _state = OAuthUtility.GenerateState();

            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                // TODO: configurable
                FileName = @$"https://accounts.google.com/o/oauth2/v2/auth
                    ?client_id={_settings.GoogleOAuth.ClientId}
                    &response_type=code
                    &scope=openid%20email%20profile
                    &redirect_uri={_settings.GoogleOAuth.RedirectUri}
                    &state={_state}"
                // FileName = @$"https://accounts.google.com/o/oauth2/v2/auth
                //     ?client_id={_settings.GoogleOAuth.ClientId}
                //     &response_type=code
                //     &scope=openid%20email%20profile
                //     &redirect_uri={_settings.GoogleOAuth.RedirectUri}
                //     &state={_state}
                //     &code_challenge=YOUR_CODE_CHALLENGE
                //     &code_challenge_method=S256"
            });
        }
        catch (Exception ex)
        {
            _state = null;
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

            using var response = await HttpClient.PostAsJsonAsync(endpoint, payload);
            response.EnsureSuccessStatusCode();

            LastSendStatus = $"Sent {latest.CapturedAtText}.";
        }
        catch (Exception ex)
        {
            LastSendStatus = $"Send failed: {ex.Message}";
        }
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
