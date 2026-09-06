/*
 * CODEX-GENERATED: the contents of this file were fully constructed by a Codex agent and not a human.
*/
using System;
using System.Threading.Tasks;

namespace Scissors.Services;

public static class ClipboardCopy
{
    public static async Task CopyAsync(
        Func<string, Task> setTextAsync,
        string text,
        Action onCopied)
    {
        ArgumentNullException.ThrowIfNull(setTextAsync);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(onCopied);

        await setTextAsync(text);
        onCopied();
    }
}
