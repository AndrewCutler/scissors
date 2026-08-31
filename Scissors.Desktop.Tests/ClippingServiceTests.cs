using Microsoft.Extensions.Logging;
using Xunit;

namespace Scissors.Desktop.Tests;

public class ClippingServiceTests
{
    [Fact]
    public async Task GetClippingsThrowsWhenTheUserIsNotAuthenticated()
    {
        var session = new AuthSession();
        var store = new ClippingStore();
        var api = new FakeScissorsApiClient();
        var service = new ClippingService(session, store, api, TestLogger.Create<IClippingService>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetClippingsAsync());
    }

    [Fact]
    public async Task GetClippingsMapsAndSortsTheApiResponse()
    {
        var session = new AuthSession();
        session.SetToken("access-token");
        var store = new ClippingStore();
        var api = new FakeScissorsApiClient
        {
            GetClippingsResult = new List<ClippingResponseDTO>
            {
                new()
                {
                    Id = 1,
                    Text = "older",
                    CapturedAt = new DateTimeOffset(2026, 8, 30, 13, 0, 0, TimeSpan.Zero),
                    CreatedAt = DateTimeOffset.UtcNow,
                },
                new()
                {
                    Id = 2,
                    Text = "newer",
                    CapturedAt = new DateTimeOffset(2026, 8, 30, 14, 0, 0, TimeSpan.Zero),
                    CreatedAt = DateTimeOffset.UtcNow,
                },
            }
        };
        var service = new ClippingService(session, store, api, TestLogger.Create<IClippingService>());

        var clippings = await service.GetClippingsAsync();

        Assert.Equal(new[] { "newer", "older" }, clippings.Select(clipping => clipping.Text).ToArray());
        Assert.Equal("access-token", api.GetClippingsCalls.Single());
    }

    [Fact]
    public async Task SaveClippingRemovesTheTemporaryEntryAndAddsTheSavedClipping()
    {
        var session = new AuthSession();
        session.SetToken("access-token");
        var store = new ClippingStore();
        var pending = Clipping.FromPaste(DateTimeOffset.UtcNow.AddMinutes(-5), "pending");
        store.Add(pending);
        var api = new FakeScissorsApiClient
        {
            SaveClippingResult = new ClippingResponseDTO
            {
                Id = 7,
                Text = "saved",
                CapturedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
            }
        };
        var service = new ClippingService(session, store, api, TestLogger.Create<IClippingService>());

        var saved = await service.SaveClippingAsync(pending);

        Assert.Equal(7, saved.Id);
        Assert.DoesNotContain(store.Clippings, clipping => clipping.TemporaryId == pending.TemporaryId);
        Assert.Contains(store.Clippings, clipping => clipping.Id == 7);
    }

    [Fact]
    public async Task DeleteClippingRemovesTheStoredItem()
    {
        var session = new AuthSession();
        session.SetToken("access-token");
        var store = new ClippingStore();
        var clipping = Clipping.FromDTO(new ClippingResponseDTO
        {
            Id = 7,
            Text = "saved",
            CapturedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        store.Add(clipping);
        var api = new FakeScissorsApiClient();
        var service = new ClippingService(session, store, api, TestLogger.Create<IClippingService>());

        await service.DeleteClippingAsync(clipping);

        Assert.Empty(store.Clippings);
        Assert.Equal(7, api.DeleteClippingCalls.Single().Id);
    }
}
