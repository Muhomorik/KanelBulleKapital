using System.Reflection;
using Autofac;
using FikaForecast.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;

namespace FikaForecast.Wpf.Modules;

/// <summary>
/// Registers Presentation layer components: ViewModels (by convention), Views, and MainWindow.
/// Also loads model configuration from appsettings or defaults.
/// </summary>
public class PresentationModule : Autofac.Module
{
    private readonly IConfiguration _configuration;

    public PresentationModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        RegisterModels(builder);
        RegisterViewModels(builder);
        RegisterViews(builder);
    }

    /// <summary>
    /// Loads <see cref="ModelConfig"/> list from configuration.
    /// Falls back to default models if none are configured.
    /// </summary>
    private void RegisterModels(ContainerBuilder builder)
    {
        var section = _configuration.GetSection("FikaForecast:Models");
        var models = section.GetChildren()
            .Select(s => new ModelConfig(
                s["ModelId"] ?? "",
                s["DeploymentName"] ?? "",
                s["DisplayName"] ?? ""))
            .Where(m => !string.IsNullOrEmpty(m.ModelId))
            .ToArray();

        if (models.Length == 0)
        {
            models =
            [
                new ModelConfig("gpt-4.1", "gpt-4.1", "GPT-4.1"),
                new ModelConfig("gpt-5.4-mini", "gpt-5.4-mini", "GPT-5.4 Mini"),
            ];
        }

        builder.RegisterInstance<IEnumerable<ModelConfig>>(models);
    }

    /// <summary>
    /// Auto-discovers and registers all *ViewModel classes in this assembly.
    /// </summary>
    private static void RegisterViewModels(ContainerBuilder builder)
    {
        var assembly = Assembly.GetExecutingAssembly();

        builder.RegisterAssemblyTypes(assembly)
            .Where(t => t.Name.EndsWith("ViewModel"))
            .AsSelf()
            .InstancePerDependency();
    }

    /// <summary>
    /// Registers MainWindow explicitly and auto-discovers other *View and *Window classes.
    /// </summary>
    private static void RegisterViews(ContainerBuilder builder)
    {
        builder.RegisterType<MainWindow>().AsSelf().InstancePerDependency();

        var assembly = Assembly.GetExecutingAssembly();

        builder.RegisterAssemblyTypes(assembly)
            .Where(t => t != typeof(MainWindow)
                        && (t.Name.EndsWith("View") || t.Name.EndsWith("Window")))
            .AsSelf()
            .InstancePerDependency();
    }
}
