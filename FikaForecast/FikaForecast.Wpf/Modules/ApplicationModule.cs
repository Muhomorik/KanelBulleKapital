using Autofac;
using FikaForecast.Application.Interfaces;
using FikaForecast.Application.Services;

namespace FikaForecast.Wpf.Modules;

/// <summary>
/// Registers Application layer services: orchestrator, comparison service, and prompt provider.
/// </summary>
public class ApplicationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<NewsBriefOrchestrator>()
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterType<BriefComparisonService>()
            .As<IBriefComparisonService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<PromptProvider>()
            .As<IPromptProvider>()
            .SingleInstance();
    }
}
