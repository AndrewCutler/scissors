using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Scissors.Interop;
using Scissors;
using Scissors.ViewModels;
using Win32KeyInterop = Avalonia.Win32.Input.KeyInterop;

namespace Scissors.Views;

public partial class MainWindow : Window
{
    private WindowsGlobalHotKey? _globalHotKey;
    private bool _allowClose;

    public MainWindow(MainViewModel mainViewModel)
    {
        InitializeComponent();
        Opened += OnOpened;
        Closing += OnClosing;
        Closed += OnClosed;

        DataContext = mainViewModel;
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

    private async void OpenGoogleOAuth_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        await viewModel.StartGoogleOAuthAsync();
    }

    private async void SendLatest_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        await viewModel.SendLatestClipboardAsync();
    }

    private void Exit_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.RequestExit();
        }
    }

    private void OnGlobalHotKeyPressed()
    {
        _ = HandleGlobalHotkeyPressedAsync();
    }

    private async Task HandleGlobalHotkeyPressedAsync()
    {
        try
        {
            string? text = null;
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is not null)
            {
                text = await clipboard.TryGetTextAsync();
            }

            Dispatcher.UIThread.Post(() =>
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.AddClipboardText(text);
                }

                ShowAndActivate();
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
