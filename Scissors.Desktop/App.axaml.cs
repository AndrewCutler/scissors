using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Scissors.Configuration;
using Scissors.ViewModels;
using Scissors.Views;

namespace Scissors;

public partial class App : Application
{
    public static DesktopAppSettings AppSettings { get; set; } = DesktopAppSettings.CreateDesignTimeDefaults();

    private TrayIcon? _trayIcon;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(AppSettings),
            };
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
