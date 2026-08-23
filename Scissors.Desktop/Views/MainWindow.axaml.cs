using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.Logging;
using Scissors.Services;
using Scissors.ViewModels;

namespace Scissors.Views;

public partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly AuthSession _authSession;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IScissorsApiClient _apiClient;
    private bool _allowClose;

    public MainWindow(
        MainViewModel mainViewModel,
        AuthSession authSession,
        IRefreshTokenStore refreshTokenStore,
        IScissorsApiClient apiClient,
        ILogger<MainWindow> logger)
    {
        _authSession = authSession;
        _refreshTokenStore = refreshTokenStore;
        _apiClient = apiClient;
        _logger = logger;

        InitializeComponent();
        Closing += OnClosing;

        DataContext = mainViewModel;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
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
            _logger.LogInformation("Logged out locally and cleared the stored refresh token.");
        }
        else
        {
            _logger.LogWarning("Logout failed");
        }
    }
}
