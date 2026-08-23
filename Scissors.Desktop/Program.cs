using System;
using System.IO;
using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scissors.Configuration;
using Scissors.Services;
using Scissors.ViewModels;
using Scissors.Views;
using Serilog;

namespace Scissors;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Scissors",
            "Logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(logDirectory, "scissors-desktop-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true)
            .CreateLogger();

        try
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();

            services.AddLogging(logging =>
            {
                logging.AddSerilog(Log.Logger, dispose: false);
            });

            services.AddSingleton(configuration.Get<DesktopAppSettings>()!);
            services.AddSingleton<AuthSession>();
            services.AddSingleton<IRefreshTokenStore, RefreshTokenStore>();
            services.AddSingleton<IScissorsApiClient, ScissorsApiClient>();
            services.AddTransient<MainWindow>();
            services.AddTransient<MainViewModel>();

            var serviceProvider = services.BuildServiceProvider();

            BuildAvaloniaApp(serviceProvider)
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Scissors.Desktop terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(IServiceProvider services)
        => AppBuilder.Configure(() => new App(
            services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<App>>(),
            services))
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
