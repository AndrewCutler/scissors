using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Scissors.Interop;
using Win32KeyInterop = Avalonia.Win32.Input.KeyInterop;

namespace Scissors.Views;

public partial class MainWindow : Window
{
    private int _count = 0;
    private WindowsGlobalHotKey? _globalHotKey;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closing += OnClosing;
        Closed += OnClosed;
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        _count++;
        Sync.Text = _count.ToString();
        Console.WriteLine("Click!");
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        _globalHotKey ??= WindowsGlobalHotKey.TryRegister(
            this,
            WindowsHotKeyModifiers.Control | WindowsHotKeyModifiers.Shift,
            (uint)Win32KeyInterop.VirtualKeyFromKey(Key.C),
            OnGlobalHotKeyPressed);
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose)
        {
            _globalHotKey?.Dispose();
            _globalHotKey = null;
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _globalHotKey?.Dispose();
        _globalHotKey = null;
    }

    public void PrepareForExit()
    {
        _allowClose = true;
    }

    public void ShowAndActivate()
    {
        if (!IsVisible)
            Show();

        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;

        Activate();
    }

    private void OnGlobalHotKeyPressed()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _count++;
            Sync.Text = $"Ctrl+Shift+C pressed {_count} time(s)";
            ShowAndActivate();
            Console.WriteLine("Global hotkey: Ctrl+Shift+C");
        });
    }
}
