using System.Reactive.Concurrency;
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

        builder.Register(_ => new SynchronizationContextScheduler(SynchronizationContext.Current!))
            .As<IScheduler>()
            .InstancePerDependency();

        builder.RegisterType<BatchSchedulingService>()
            .As<IBatchSchedulingService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<NewsBriefJsonParser>()
            .As<INewsBriefParser>()
            .InstancePerLifetimeScope();

        builder.RegisterType<NewsBriefMarkdownRenderer>()
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterType<PromptProvider>()
            .As<IPromptProvider>()
            .SingleInstance();

        // Step 2: Weekly Summary
        builder.RegisterType<WeeklySummaryOrchestrator>()
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterType<WeeklySummaryJsonParser>()
            .As<IWeeklySummaryParser>()
            .InstancePerLifetimeScope();

        builder.RegisterType<WeeklySummaryMarkdownRenderer>()
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterType<WeeklySummaryInputFormatter>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Step 3: Substitution Chain
        builder.RegisterType<SubstitutionChainOrchestrator>()
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterType<SubstitutionChainJsonParser>()
            .As<ISubstitutionChainParser>()
            .InstancePerLifetimeScope();

        builder.RegisterType<SubstitutionChainMarkdownRenderer>()
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterType<SubstitutionChainInputFormatter>()
            .AsSelf()
            .InstancePerLifetimeScope();

        // Step 4: Opportunity Scan
        builder.RegisterType<OpportunityScanOrchestrator>()
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterType<OpportunityScanJsonParser>()
            .As<IOpportunityScanParser>()
            .InstancePerLifetimeScope();

        builder.RegisterType<OpportunityScanMarkdownRenderer>()
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterType<OpportunityScanInputFormatter>()
            .AsSelf()
            .InstancePerLifetimeScope();
    }
}
