using System;
using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scissors.Configuration;
using Scissors.ViewModels;
using Scissors.Views;

namespace Scissors;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();

        services.AddSingleton(configuration.Get<DesktopAppSettings>()!);
        services.AddSingleton<AuthSession>();
        services.AddSingleton<IRefreshTokenStore, RefreshTokenStore>();
        services.AddTransient<MainWindow>();
        services.AddTransient<MainViewModel>();

        var serviceProvider = services.BuildServiceProvider();

        BuildAvaloniaApp(serviceProvider)
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(IServiceProvider services)
        => AppBuilder.Configure(() => new App(services))
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
