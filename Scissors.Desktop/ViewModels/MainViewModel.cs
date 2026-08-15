using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Scissors.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private static readonly HttpClient HttpClient = new();

    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";

    [ObservableProperty]
    public partial string? LastSendStatus { get; set; }

    private string _apiUrl { get; }
    private string _clientId { get; }
    private string _redirectUri { get; }
    private string? _state { get; set; }

    public ObservableCollection<ClipboardEntry> ClipboardEntries { get; } = new();

    public MainViewModel()
        : this(configuration: null)
    {
    }

    public MainViewModel(IConfiguration? configuration)
    {
        var apiUrl = configuration?["ApiUrl"];
        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            throw new InvalidOperationException("ApiUrl cannot be null.");
        }

        _apiUrl = apiUrl;
        
        var clientId = configuration?["OAuth:Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("ClientId cannot be null.");
        }

        _clientId = clientId;

        var redirectUri = configuration?["OAuth:Google:RedirectUri"];
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            throw new InvalidOperationException("RedirectUri cannot be null.");
        }

        _redirectUri = redirectUri;
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
                    ?client_id={_clientId}
                    &response_type=code
                    &scope=openid%20email%20profile
                    &redirect_uri={_redirectUri}
                    &state={_state}
                    &code_challenge=YOUR_CODE_CHALLENGE
                    &code_challenge_method=S256"
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

        if (!Uri.TryCreate(_apiUrl + "/clippings", UriKind.Absolute, out var endpoint))
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
