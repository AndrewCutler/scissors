/*
 * CODEX-GENERATED: the contents of this file were fully constructed by a Codex agent and not a human.
*/
using Scissors.Services;
using Xunit;

namespace Scissors.Desktop.Tests;

public class ClipboardCopyTests
{
    [Fact]
    public async Task CopyAsyncPassesExactTextToClipboard()
    {
        string? copiedText = null;

        await ClipboardCopy.CopyAsync(
            text =>
            {
                copiedText = text;
                return Task.CompletedTask;
            },
            "line one\nline two",
            () => { });

        Assert.Equal("line one\nline two", copiedText);
    }

    [Fact]
    public async Task CopyAsyncReportsSuccessAfterClipboardCompletes()
    {
        var clipboardCompleted = false;
        var copiedReported = false;

        await ClipboardCopy.CopyAsync(
            _ =>
            {
                clipboardCompleted = true;
                return Task.CompletedTask;
            },
            "copied text",
            () => copiedReported = clipboardCompleted);

        Assert.True(copiedReported);
    }

    [Fact]
    public async Task CopyAsyncDoesNotReportSuccessWhenClipboardFails()
    {
        var copiedReported = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ClipboardCopy.CopyAsync(
                _ => Task.FromException(new InvalidOperationException("clipboard unavailable")),
                "copied text",
                () => copiedReported = true));

        Assert.False(copiedReported);
    }
}
