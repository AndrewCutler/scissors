using Microsoft.Extensions.Logging;
using Scissors.Services;

namespace Scissors.Desktop.Tests;

internal sealed class FakeScissorsApiClient : IScissorsApiClient
{
    public List<string> GetClippingsCalls { get; } = [];
    public List<(string AccessToken, DateTimeOffset CapturedAt, string Text)> SaveClippingCalls { get; } = [];
    public List<(string AccessToken, int Id)> DeleteClippingCalls { get; } = [];
    public string? RefreshTokenRequested { get; private set; }

    public List<ClippingResponseDTO> GetClippingsResult { get; set; } = [];
    public GetRefreshTokenResponseDTO? RefreshTokenResult { get; set; }
    public GoogleAuthResponseDTO? GoogleAuthResult { get; set; }
    public bool LogoutResult { get; set; } = true;
    public ClippingResponseDTO SaveClippingResult { get; set; } = new()
    {
        Id = 100,
        Text = "saved",
        CapturedAt = DateTimeOffset.UtcNow,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    public Task<List<ClippingResponseDTO>> GetClippingsAsync(string accessToken)
    {
        GetClippingsCalls.Add(accessToken);
        return Task.FromResult(GetClippingsResult);
    }

    public Task<GetRefreshTokenResponseDTO?> GetRefreshTokenAsync(string refreshToken)
    {
        RefreshTokenRequested = refreshToken;
        return Task.FromResult(RefreshTokenResult);
    }

    public Task<GoogleAuthResponseDTO?> CompleteGoogleOAuthAsync(string code)
        => Task.FromResult(GoogleAuthResult);

    public Task<bool> LogOutAsync(string accessToken)
        => Task.FromResult(LogoutResult);

    public Task<ClippingResponseDTO> SaveClippingAsync(string accessToken, DateTimeOffset capturedAt, string text)
    {
        SaveClippingCalls.Add((accessToken, capturedAt, text));
        return Task.FromResult(SaveClippingResult);
    }

    public Task DeleteClippingAsync(string accessToken, int id)
    {
        DeleteClippingCalls.Add((accessToken, id));
        return Task.CompletedTask;
    }
}

internal sealed class FakeClippingService : IClippingService
{
    public List<Clipping> GetClippingsResult { get; set; } = [];
    public Clipping? SaveClippingResult { get; set; }
    public List<Clipping> SaveClippingCalls { get; } = [];
    public List<Clipping> AddedClippings { get; } = [];
    public List<int> DeletedClippingIds { get; } = [];

    public Task<List<Clipping>> GetClippingsAsync()
        => Task.FromResult(GetClippingsResult);

    public Task<Clipping> SaveClippingAsync(Clipping clipping)
    {
        SaveClippingCalls.Add(clipping);
        SaveClippingResult ??= clipping;
        return Task.FromResult(SaveClippingResult);
    }

    public Task DeleteClippingAsync(int id)
    {
        DeletedClippingIds.Add(id);
        return Task.CompletedTask;
    }

    public void AddClipping(Clipping clipping)
    {
        AddedClippings.Add(clipping);
    }
}

internal sealed class FakeClippingHubConnectionService : IClippingHubConnectionService
{
    public int StartCalls { get; private set; }
    public int StopCalls { get; private set; }
    public TaskCompletionSource<bool> StopCalled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task StartAsync()
    {
        StartCalls++;
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        StopCalls++;
        StopCalled.TrySetResult(true);
        return Task.CompletedTask;
    }
}

internal sealed class FakeRefreshTokenStore : IRefreshTokenStore
{
    public string? Value { get; set; }
    public List<string> SavedTokens { get; } = [];
    public int DeleteCalls { get; private set; }

    public Task<string?> GetAsync()
        => Task.FromResult(Value);

    public Task SaveAsync(string refreshToken)
    {
        Value = refreshToken;
        SavedTokens.Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task DeleteAsync()
    {
        Value = null;
        DeleteCalls++;
        return Task.CompletedTask;
    }
}

internal static class TestLogger
{
    public static ILogger<T> Create<T>() => LoggerFactory.Create(builder => { }).CreateLogger<T>();
}
