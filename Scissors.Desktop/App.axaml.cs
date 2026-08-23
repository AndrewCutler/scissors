using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scissors.Services;
using Scissors.ViewModels;
using Scissors.Views;

namespace Scissors;

public partial class App : Application
{
    private readonly ILogger<App> _logger;
    private readonly IServiceProvider _services;

    private TrayIcon? _trayIcon;

    public App(ILogger<App> logger, IServiceProvider services)
    {
        _logger = logger;
        _services = services;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        var refreshTokenStore = _services.GetRequiredService<IRefreshTokenStore>();
        var refreshToken = await refreshTokenStore.GetAsync();
        if (refreshToken is not null)
        {
            try
            {
                _logger.LogInformation("Attempting to refresh desktop access token at startup.");
                var apiClient = _services.GetRequiredService<IScissorsApiClient>();
                var authSession = _services.GetRequiredService<AuthSession>();
                var tokenResponse = await apiClient.GetRefreshTokenAsync(refreshToken);
                if (tokenResponse is not null)
                {
                    authSession.SetToken(tokenResponse.AccessToken);
                    authSession.SetExpiresAt(tokenResponse.AccessTokenExpiresAt);
                    await refreshTokenStore.SaveAsync(tokenResponse.RefreshToken);
                    _logger.LogInformation("Refresh succeeded");
                    // get clippings here
                }
                else
                {
                    _logger.LogWarning("Stored refresh token was rejected by the API.");
                    await refreshTokenStore.DeleteAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh the desktop session at startup.");
            }
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.MainWindow = _services.GetRequiredService<MainWindow>();
        }

        if (!Design.IsDesignMode)
        {
            CreateTrayIcon();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ShowWindow_OnClick(object? sender, System.EventArgs e)
    {
        ShowMainWindow();
    }

    private void Exit_OnClick(object? sender, System.EventArgs e)
    {
        RequestExit();
    }

    public void RequestExit()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
            desktop.MainWindow is MainWindow window)
        {
            window.PrepareForExit();
        }

        _trayIcon?.Dispose();
        _trayIcon = null;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            desktopLifetime.Shutdown();
        }
    }

    private void ShowMainWindow()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow is not MainWindow window)
        {
            return;
        }

        window.ShowAndActivate();
    }

    private void CreateTrayIcon()
    {
        var trayMenu = new NativeMenu();

        var showWindowItem = new NativeMenuItem
        {
            Header = "Show Window",
        };
        showWindowItem.Click += ShowWindow_OnClick;
        trayMenu.Items.Add(showWindowItem);

        var exitItem = new NativeMenuItem
        {
            Header = "Exit",
        };
        exitItem.Click += Exit_OnClick;
        trayMenu.Items.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://Scissors.Desktop/Assets/avalonia-logo.ico"))),
            ToolTipText = "My App",
            Menu = trayMenu,
        };

        TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });
    }
}
