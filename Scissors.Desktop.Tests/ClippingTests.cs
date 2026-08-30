using Xunit;

namespace Scissors.Desktop.Tests;

public class ClippingTests
{
    [Fact]
    public void FromPasteCreatesAClientSideOnlyClipping()
    {
        var capturedAt = new DateTimeOffset(2026, 8, 30, 13, 14, 15, TimeSpan.Zero);

        var clipping = Clipping.FromPaste(capturedAt, "clipboard text");

        Assert.Null(clipping.Id);
        Assert.NotNull(clipping.TemporaryId);
        Assert.Equal(capturedAt, clipping.CapturedAt);
        Assert.Equal("clipboard text", clipping.Text);
        Assert.True(clipping.ClientSideOnly);
        Assert.False(clipping.HasServerId);
        Assert.Equal(string.Empty, clipping.SyncMark);
        Assert.Equal("13:14:15", clipping.CapturedAtText);
    }

    [Fact]
    public void FromDtoCreatesASyncedClipping()
    {
        var dto = new ClippingResponseDTO
        {
            Id = 9,
            Text = "synced",
            CapturedAt = new DateTimeOffset(2026, 8, 30, 13, 30, 0, TimeSpan.Zero),
            CreatedAt = new DateTimeOffset(2026, 8, 30, 13, 31, 0, TimeSpan.Zero),
        };

        var clipping = Clipping.FromDTO(dto);

        Assert.Equal(dto.Id, clipping.Id);
        Assert.Null(clipping.TemporaryId);
        Assert.False(clipping.ClientSideOnly);
        Assert.True(clipping.HasServerId);
        Assert.Equal("✓", clipping.SyncMark);
    }
}
