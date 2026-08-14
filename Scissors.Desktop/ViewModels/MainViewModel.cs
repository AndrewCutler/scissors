using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Scissors.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private static readonly HttpClient HttpClient = new();

    [ObservableProperty]
    public partial string Greeting { get; set; } = "Welcome to Avalonia!";

    [ObservableProperty]
    public partial string? LastSendStatus { get; set; }

    public string PostEndpoint { get; set; } = "http://localhost:5000/api/clipboard";

    public ObservableCollection<ClipboardEntry> ClipboardEntries { get; } = new();

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

    public async Task SendLatestClipboardAsync()
    {
        var latest = ClipboardEntries.FirstOrDefault();
        if (latest is null)
        {
            LastSendStatus = "Nothing to send yet.";
            return;
        }

        if (!Uri.TryCreate(PostEndpoint, UriKind.Absolute, out var endpoint))
        {
            LastSendStatus = "Invalid POST endpoint.";
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
