using System.Windows;
using Autofac;
using FikaForecast.Infrastructure.Persistence;
using FikaForecast.Wpf.Modules;
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

        // Build configuration from appsettings.json and User Secrets
        var configuration = BuildConfiguration();

        // Configure Autofac container
        var builder = new ContainerBuilder();

        // Register configuration
        builder.RegisterInstance(configuration).As<IConfiguration>();

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
    /// Ensures the SQLite database and tables exist.
    /// </summary>
    private async Task InitializeDatabaseAsync()
    {
        try
        {
            var db = _appScope!.Resolve<FikaDbContext>();
            await db.Database.EnsureCreatedAsync();
            Logger.Info("Database initialized successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to initialize database");
            throw;
        }
    }
}
