using Xunit;

namespace Scissors.Desktop.Tests;

public class MainViewModelTests
{
    [Fact]
    public void CaptureClipboardTextAddsAClippingOnlyWhenAuthenticated()
    {
        var sut = CreateSut();

        sut.ViewModel.CaptureClipboardText("hello");

        Assert.Empty(sut.Store.Clippings);

        sut.AuthSession.SetToken("access-token");
        sut.ViewModel.CaptureClipboardText("hello");

        Assert.Single(sut.Store.Clippings);
        Assert.Equal("hello", sut.Store.Clippings[0].Text);
        Assert.True(sut.Store.Clippings[0].TemporaryId.HasValue);
    }

    [Fact]
    public void CaptureClipboardTextIgnoresBlankOrDuplicateContent()
    {
        var sut = CreateSut();
        sut.AuthSession.SetToken("access-token");
        sut.Store.Init(new List<Clipping>
        {
            Clipping.FromDTO(new ClippingResponseDTO
            {
                Id = 1,
                Text = "first",
                CapturedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
            })
        });

        sut.ViewModel.CaptureClipboardText(string.Empty);
        sut.ViewModel.CaptureClipboardText("first");

        Assert.Single(sut.Store.Clippings);
    }

    [Fact]
    public async Task ClearingTheAuthSessionResetsTheStoreAndStopsTheHubConnection()
    {
        var sut = CreateSut();
        sut.AuthSession.SetToken("access-token");
        sut.Store.Add(Clipping.FromPaste(DateTimeOffset.UtcNow, "temp"));

        sut.AuthSession.Clear();

        var completed = await Task.WhenAny(sut.Hub.StopCalled.Task, Task.Delay(TimeSpan.FromSeconds(1)));

        Assert.Same(sut.Hub.StopCalled.Task, completed);
        Assert.Empty(sut.Store.Clippings);
        Assert.False(sut.ViewModel.IsAuthenticated);
        Assert.True(sut.ViewModel.CanContinueWithGoogle);
    }

    [Fact]
    public async Task SendAndDeleteClippingDelegateToTheClippingService()
    {
        var sut = CreateSut();
        var clipping = Clipping.FromDTO(new ClippingResponseDTO
        {
            Id = 12,
            Text = "saved",
            CapturedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await sut.ViewModel.SendClippingAsync(clipping);
        await sut.ViewModel.DeleteClippingAsync(clipping);

        Assert.Single(sut.ClippingService.SaveClippingCalls);
        Assert.Equal(12, sut.ClippingService.DeletedClippingIds.Single());
    }

    [Fact]
    public async Task DeleteClippingRemovesTemporaryClippingsLocally()
    {
        var sut = CreateSut();
        var clipping = Clipping.FromPaste(DateTimeOffset.UtcNow, "temp");

        await sut.ViewModel.DeleteClippingAsync(clipping);

        Assert.Empty(sut.Store.Clippings);
    }

    private static Sut CreateSut()
    {
        var settings = new Scissors.Configuration.DesktopAppSettings
        {
            ApiUrl = "http://localhost:5098/api/v1",
            OAuth = new Scissors.Configuration.OAuthSettings
            {
                Google = new Scissors.Configuration.GoogleOAuthSettings
                {
                    ClientId = "client",
                    RedirectUri = "http://localhost/callback",
                }
            }
        };
        var store = new ClippingStore();
        var authSession = new AuthSession();
        var api = new FakeScissorsApiClient();
        var clippingService = new FakeClippingService();
        var hub = new FakeClippingHubConnectionService();
        var refreshTokenStore = new FakeRefreshTokenStore();
        var deviceStorage = new FakeDeviceStorage();
        var viewModel = new Scissors.ViewModels.MainViewModel(
            settings,
            TestLogger.Create<Scissors.ViewModels.MainViewModel>(),
            api,
            clippingService,
            hub,
            store,
            authSession,
            refreshTokenStore,
            deviceStorage);

        return new Sut(viewModel, store, authSession, clippingService, hub);
    }

    private sealed record Sut(
        Scissors.ViewModels.MainViewModel ViewModel,
        ClippingStore Store,
        AuthSession AuthSession,
        FakeClippingService ClippingService,
        FakeClippingHubConnectionService Hub);
}
