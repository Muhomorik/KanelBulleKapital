using System.Windows;
using Autofac;
using CommandLine;
using FikaForecast.Infrastructure.Persistence;
using FikaForecast.Wpf.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NLog;

namespace FikaForecast.Wpf;

/// <summary>
/// Application entry point. Builds Autofac container from modules and creates the main window.
/// </summary>
public partial class App : System.Windows.Application
{
    private IContainer? _container;
    private ILifetimeScope? _appScope;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Configures DI container, initializes database, and shows the main window.
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize NLog from NLog.config (before DI container)
        LogManager.Setup().LoadConfigurationFromFile("NLog.config");
        Logger.Info("Application starting...");

        // Global exception handlers — safety net to prevent crashes
        DispatcherUnhandledException += (_, args) =>
        {
            Logger.Error(args.Exception, "Unhandled UI exception");
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{args.Exception.Message}",
                "FikaForecast Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                Logger.Fatal(ex, "Unhandled domain exception");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Logger.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };

        // Parse command-line arguments
        var cliOptions = Parser.Default.ParseArguments<CliOptions>(e.Args).Value ?? new CliOptions();

        if (cliOptions.AutoSchedule)
        {
            Logger.Info("Launched with --auto-schedule: batch scheduler will start automatically");
        }

        // Build configuration from appsettings.json and User Secrets
        var configuration = BuildConfiguration();

        // Configure Autofac container
        var builder = new ContainerBuilder();

        // Register configuration and CLI options
        builder.RegisterInstance(configuration).As<IConfiguration>();
        builder.RegisterInstance(cliOptions).SingleInstance();

        // Auto-inject NLog.ILogger into all components (must be registered before other modules)
        builder.RegisterModule<NLogModule>();

        // Register modules
        builder.RegisterModule(new InfrastructureModule(configuration));
        builder.RegisterModule<ApplicationModule>();
        builder.RegisterModule(new PresentationModule(configuration));

        // Build container and create app-level lifetime scope
        _container = builder.Build();
        _appScope = _container.BeginLifetimeScope();

        // Initialize database
        await InitializeDatabaseAsync();

        Logger.Info("DI container configured");

        // Resolve and show main window
        var mainWindow = _appScope.Resolve<MainWindow>();
        mainWindow.Show();

        Logger.Info("Application started successfully");
    }

    /// <summary>
    /// Disposes the DI container and shuts down NLog.
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Info("Application exiting...");

        _appScope?.Dispose();
        _container?.Dispose();

        LogManager.Shutdown();

        base.OnExit(e);
    }

    /// <summary>
    /// Builds configuration from appsettings.json and User Secrets.
    /// </summary>
    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddUserSecrets<App>(optional: true)
            .Build();
    }

    /// <summary>
    /// Applies EF Core migrations to create or update the SQLite database schema.
    /// </summary>
    private async Task InitializeDatabaseAsync()
    {
        try
        {
            var db = _appScope!.Resolve<FikaDbContext>();
            await db.Database.MigrateAsync();
            Logger.Info("Database initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize database");
            throw;
        }
    }
}
