using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Scissors.Interop;

[Flags]
public enum WindowsHotKeyModifiers : uint
{
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008,
    NoRepeat = 0x4000,
}

public sealed class WindowsGlobalHotKey : IDisposable
{
    private const int GwlWndProc = -4;
    private const uint WmHotKey = 0x0312;

    private readonly IntPtr _hwnd;
    private readonly int _hotKeyId;
    private readonly IntPtr _previousWndProc;
    private readonly WndProcDelegate _wndProcDelegate;
    private bool _disposed;

    private WindowsGlobalHotKey(
        IntPtr hwnd,
        int hotKeyId,
        IntPtr previousWndProc,
        WndProcDelegate wndProcDelegate)
    {
        _hwnd = hwnd;
        _hotKeyId = hotKeyId;
        _previousWndProc = previousWndProc;
        _wndProcDelegate = wndProcDelegate;
    }

    public static WindowsGlobalHotKey? TryRegister(
        Window window,
        WindowsHotKeyModifiers modifiers,
        uint virtualKey,
        Action onPressed,
        int hotKeyId = 0x5301)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        if (!TryGetHwnd(window, out var hwnd))
        {
            return null;
        }

        if (!RegisterHotKey(hwnd, hotKeyId, (uint)modifiers, virtualKey))
        {
            return null;
        }

        var previousWndProcHandle = IntPtr.Zero;
        var wndProcDelegate = new WndProcDelegate((hWnd, msg, wParam, lParam) =>
        {
            if (msg == WmHotKey && wParam == (IntPtr)hotKeyId)
            {
                onPressed();
                return IntPtr.Zero;
            }

            return CallWindowProc(previousWndProcHandle, hWnd, msg, wParam, lParam);
        });

        previousWndProcHandle = SetWindowLongPtr(hwnd, GwlWndProc, Marshal.GetFunctionPointerForDelegate(wndProcDelegate));
        if (previousWndProcHandle == IntPtr.Zero)
        {
            UnregisterHotKey(hwnd, hotKeyId);
            return null;
        }

        return new WindowsGlobalHotKey(hwnd, hotKeyId, previousWndProcHandle, wndProcDelegate);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        UnregisterHotKey(_hwnd, _hotKeyId);
        SetWindowLongPtr(_hwnd, GwlWndProc, _previousWndProc);
    }

    private static bool TryGetHwnd(Window window, out IntPtr hwnd)
    {
        hwnd = IntPtr.Zero;

        var platformHandle = window.TryGetPlatformHandle();
        if (platformHandle is null)
            return false;

        if (!string.Equals(platformHandle.HandleDescriptor, "HWND", StringComparison.OrdinalIgnoreCase))
            return false;

        hwnd = platformHandle.Handle;
        return hwnd != IntPtr.Zero;
    }

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", EntryPoint = "CallWindowProcW", SetLastError = true)]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);

        return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);
}
