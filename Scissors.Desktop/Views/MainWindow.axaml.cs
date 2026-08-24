using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Scissors.Interop;
using Scissors.Services;
using Scissors.ViewModels;
using Win32KeyInterop = Avalonia.Win32.Input.KeyInterop;

namespace Scissors.Views;

public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly AuthSession _authSession;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IClippingStore _clippingStore;
    private readonly IScissorsApiClient _apiClient;
    private WindowsGlobalHotKey? _globalHotKey;
    private bool _allowClose;

    public MainWindow(
        MainViewModel mainViewModel,
        AuthSession authSession,
        IRefreshTokenStore refreshTokenStore,
        IClippingStore clippingStore,
        IScissorsApiClient apiClient,
        ILogger<MainWindow> logger)
    {
        _authSession = authSession;
        _refreshTokenStore = refreshTokenStore;
        _clippingStore = clippingStore;
        _apiClient = apiClient;
        _logger = logger;

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
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

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

    private void Exit_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.RequestExit();
        }
    }

    private async void Logout_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!_authSession.IsAuthenticated)
        {
            _logger.LogWarning("Attempted to log out without an access token.");
            return;
        }

        var success = await _apiClient.LogOutAsync(_authSession.AccessToken!);

        if (success)
        {
            _authSession.Clear();
            await _refreshTokenStore.DeleteAsync();
            _clippingStore.Reset();
            _logger.LogInformation("Logged out locally and cleared the stored refresh token.");
        }
        else
        {
            _logger.LogWarning("Logout failed");
        }
    }

    private async void SendClipping_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel ||
            sender is not Button button ||
            button.DataContext is not Clipping clipping)
        {
            return;
        }

        try
        {
            await viewModel.SendClippingAsync(clipping);
        }
        catch (NotImplementedException ex)
        {
            _logger.LogDebug(ex, "Send clipping action is not implemented yet.");
        }
    }

    private async void DeleteClipping_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel ||
            sender is not Button button ||
            button.DataContext is not Clipping clipping)
        {
            return;
        }

        try
        {
            await viewModel.DeleteClippingAsync(clipping);
        }
        catch (NotImplementedException ex)
        {
            _logger.LogDebug(ex, "Delete clipping action is not implemented yet.");
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
            if (DataContext is not MainViewModel viewModel || !viewModel.IsAuthenticated)
            {
                return;
            }

            string? text = null;
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is not null)
            {
                text = await clipboard.TryGetTextAsync();
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                viewModel.CaptureClipboardText(text);
                ShowAndActivate();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture clipboard text from the global hotkey.");
        }
    }
}
