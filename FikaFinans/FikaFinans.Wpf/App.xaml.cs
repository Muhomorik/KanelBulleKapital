using System.Windows;
using Autofac;
using FikaFinans.Wpf.Modules;
using Microsoft.Extensions.Configuration;
using NLog;

namespace FikaFinans.Wpf;

/// <summary>
/// Application entry point. Builds Autofac container from modules and creates the main window.
/// </summary>
public partial class App : System.Windows.Application
{
    private IContainer? _container;
    private ILifetimeScope? _appScope;
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        LogManager.Setup().LoadConfigurationFromFile("NLog.config");
        Logger.Info("Application starting...");

        WireUpExceptionHandlers();

        var configuration = BuildConfiguration();

        var builder = new ContainerBuilder();
        builder.RegisterInstance(configuration).As<IConfiguration>();
        builder.RegisterModule<NLogModule>();
        builder.RegisterModule<ApplicationModule>();
        builder.RegisterModule(new PresentationModule(configuration));

        _container = builder.Build();
        _appScope = _container.BeginLifetimeScope();

        Logger.Info("DI container configured");

        var mainWindow = _appScope.Resolve<MainWindow>();
        mainWindow.Show();

        Logger.Info("Application started successfully");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Info("Application exiting...");

        _appScope?.Dispose();
        _container?.Dispose();

        LogManager.Shutdown();

        base.OnExit(e);
    }

    private void WireUpExceptionHandlers()
    {
        DispatcherUnhandledException += (_, args) =>
        {
            Logger.Error(args.Exception, "Unhandled UI exception");
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{args.Exception.Message}",
                "FikaFinans Error",
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
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddUserSecrets<App>(optional: true)
            .Build();
    }
}
