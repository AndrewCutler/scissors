using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Microsoft.Extensions.Configuration;
using Scissors.ViewModels;
using Scissors.Views;

namespace Scissors;

public partial class App : Application
{
    public static IConfiguration AppConfiguration { get; set; } = new ConfigurationBuilder().Build();

    private TrayIcon? _trayIcon;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(AppConfiguration),
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
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is MainWindow window)
            {
                window.PrepareForExit();
            }

            desktop.Shutdown();
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
