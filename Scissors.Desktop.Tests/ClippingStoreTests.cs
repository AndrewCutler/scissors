using System.Collections.ObjectModel;
using Xunit;

namespace Scissors.Desktop.Tests;

public class ClippingStoreTests
{
    [Fact]
    public void AddKeepsTheCollectionSortedByCapturedAtDescending()
    {
        var store = new ClippingStore();
        var now = new DateTimeOffset(2026, 8, 30, 13, 0, 0, TimeSpan.Zero);

        store.Add(Clipping.FromPaste(now.AddMinutes(-20), "old"));
        store.Add(Clipping.FromPaste(now, "new"));
        store.Add(Clipping.FromPaste(now.AddMinutes(-10), "middle"));

        Assert.Equal(new[] { "new", "middle", "old" }, store.Clippings.Select(clipping => clipping.Text).ToArray());
    }

    [Fact]
    public void AddIgnoresDuplicateServerIdsAndTemporaryIds()
    {
        var store = new ClippingStore();
        var serverClipping = Clipping.FromDTO(new ClippingResponseDTO
        {
            Id = 1,
            Text = "server",
            CapturedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        var duplicateServerClipping = Clipping.FromDTO(new ClippingResponseDTO
        {
            Id = 1,
            Text = "duplicate",
            CapturedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        });

        var temporary = Clipping.FromPaste(DateTimeOffset.UtcNow, "temp");
        var duplicateTemporary = Clipping.FromPaste(DateTimeOffset.UtcNow.AddMinutes(-1), "temp duplicate");
        duplicateTemporary.TemporaryId = temporary.TemporaryId;

        store.Add(serverClipping);
        store.Add(duplicateServerClipping);
        store.Add(temporary);
        store.Add(duplicateTemporary);

        Assert.Equal(2, store.Clippings.Count);
    }

    [Fact]
    public void InitSortsTheCollectionAndCanOnlyBeCalledOnceUntilReset()
    {
        var store = new ClippingStore();
        var items = new List<Clipping>
        {
            Clipping.FromPaste(DateTimeOffset.UtcNow.AddMinutes(-20), "old"),
            Clipping.FromPaste(DateTimeOffset.UtcNow, "new"),
        };

        store.Init(items);

        Assert.Equal(new[] { "new", "old" }, store.Clippings.Select(clipping => clipping.Text).ToArray());

        Assert.Throws<InvalidOperationException>(() => store.Init(items));
    }

    [Fact]
    public void ResetClearsTheStoreAndAllowsASecondInitialization()
    {
        var store = new ClippingStore();
        store.Init(new List<Clipping> { Clipping.FromPaste(DateTimeOffset.UtcNow, "first") });
        store.Reset();

        Assert.Empty(store.Clippings);

        store.Init(new List<Clipping> { Clipping.FromPaste(DateTimeOffset.UtcNow, "second") });
        Assert.Single(store.Clippings);
    }

    [Fact]
    public void RemoveMethodsDeleteTheMatchingEntries()
    {
        var store = new ClippingStore();
        var temporary = Clipping.FromPaste(DateTimeOffset.UtcNow, "temp");
        var saved = Clipping.FromDTO(new ClippingResponseDTO
        {
            Id = 3,
            Text = "saved",
            CapturedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        });

        store.Add(temporary);
        store.Add(saved);

        store.RemoveTemporary(temporary.TemporaryId!.Value);
        store.Remove(3);

        Assert.Empty(store.Clippings);
    }
}
