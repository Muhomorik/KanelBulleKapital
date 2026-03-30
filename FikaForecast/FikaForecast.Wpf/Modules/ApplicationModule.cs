using Autofac;
using FikaForecast.Application.Services;

namespace FikaForecast.Wpf.Modules;

/// <summary>
/// Registers Application layer services: orchestrator and comparison service.
/// </summary>
public class ApplicationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<NewsBriefOrchestrator>()
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterType<BriefComparisonService>()
            .AsSelf()
            .InstancePerLifetimeScope();
    }
}
