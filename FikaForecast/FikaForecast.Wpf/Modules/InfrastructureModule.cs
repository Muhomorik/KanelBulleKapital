using System.IO;
using System.Net.Http;
using Autofac;
using Azure.AI.Projects;
using Azure.Identity;
using FikaForecast.Application.Interfaces;
using FikaForecast.Application.Sync;
using FikaForecast.Infrastructure.Agents;
using FikaForecast.Infrastructure.Persistence;
using FikaForecast.Infrastructure.Services;
using FikaForecast.Infrastructure.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FikaForecast.Wpf.Modules;

/// <summary>
/// Registers Infrastructure layer services: SQLite persistence, EF Core,
/// Foundry Agent Service client, and the agent implementation.
/// </summary>
public class InfrastructureModule : Autofac.Module
{
    private readonly IConfiguration _configuration;
    private readonly string _databasePath;

    public InfrastructureModule(IConfiguration configuration, string databasePath)
    {
        _configuration = configuration;
        _databasePath = databasePath;
    }

    protected override void Load(ContainerBuilder builder)
    {
        RegisterDatabase(builder);
        RegisterPromptFileService(builder);
        RegisterSyncServices(builder);
        RegisterAgentClient(builder);
        RegisterAgent(builder);
        RegisterEvaluationAgent(builder);
        RegisterWeeklySummaryAgent(builder);
        RegisterSubstitutionChainAgent(builder);
        RegisterOpportunityScanAgent(builder);
        RegisterExporter(builder);
    }

    private static void RegisterExporter(ContainerBuilder builder)
    {
        builder.RegisterType<WeeklyReportExporter>()
            .As<IWeeklyReportExporter>()
            .InstancePerLifetimeScope();
    }

    private static void RegisterSyncServices(ContainerBuilder builder)
    {
        builder.Register(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
            .SingleInstance();

        builder.RegisterType<AgentRunSyncClient>()
            .As<IAgentRunSyncClient>()
            .SingleInstance();

        builder.RegisterType<SyncService>()
            .As<ISyncService>()
            .InstancePerLifetimeScope();
    }

    private void RegisterDatabase(ContainerBuilder builder)
    {
        var dbPath = _databasePath;
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        builder.Register(_ =>
        {
            var options = new DbContextOptionsBuilder<FikaDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;
            return new FikaDbContext(options);
        }).AsSelf().InstancePerLifetimeScope();

        builder.RegisterType<NewsBriefRunRepository>()
            .As<INewsBriefRunRepository>()
            .InstancePerLifetimeScope();

        builder.RegisterType<WeeklySummaryRunRepository>()
            .As<IWeeklySummaryRunRepository>()
            .InstancePerLifetimeScope();
    }

    private static void RegisterPromptFileService(ContainerBuilder builder)
    {
        builder.RegisterType<PromptFileService>()
            .As<IPromptFileService>()
            .SingleInstance();
    }

    /// <summary>
    /// Creates <see cref="AIProjectClient"/> using the Foundry project endpoint
    /// and <see cref="DefaultAzureCredential"/> (picks up VS / Windows / az CLI login).
    /// </summary>
    private void RegisterAgentClient(ContainerBuilder builder)
    {
        var endpoint = _configuration["AzureAIFoundry:ProjectEndpoint"];

        if (!string.IsNullOrEmpty(endpoint))
        {
            var client = new AIProjectClient(
                endpoint: new Uri(endpoint),
                tokenProvider: new DefaultAzureCredential());
            builder.RegisterInstance(client).SingleInstance();
        }
    }

    /// <summary>
    /// Registers the agent implementation with optional Bing Grounding connection name.
    /// </summary>
    private void RegisterAgent(ContainerBuilder builder)
    {
        var bingConnectionName = _configuration["AzureAIFoundry:BingConnectionName"];

        builder.RegisterType<AgentFrameworkNewsBriefAgent>()
            .WithParameter("bingConnectionName", bingConnectionName)
            .As<INewsBriefAgent>()
            .SingleInstance();
    }

    /// <summary>
    /// Registers the evaluation agent (no Bing Grounding — evaluates existing content only).
    /// </summary>
    private static void RegisterEvaluationAgent(ContainerBuilder builder)
    {
        builder.RegisterType<AgentFrameworkEvaluationAgent>()
            .As<IEvaluationAgent>()
            .SingleInstance();
    }

    /// <summary>
    /// Registers the weekly summary agent (no Bing Grounding — works on DB data only).
    /// </summary>
    private static void RegisterWeeklySummaryAgent(ContainerBuilder builder)
    {
        builder.RegisterType<AgentFrameworkWeeklySummaryAgent>()
            .As<IWeeklySummaryAgent>()
            .SingleInstance();
    }

    /// <summary>
    /// Registers the substitution chain agent (no Bing Grounding — works on DB data only).
    /// </summary>
    private static void RegisterSubstitutionChainAgent(ContainerBuilder builder)
    {
        builder.RegisterType<AgentFrameworkSubstitutionChainAgent>()
            .As<ISubstitutionChainAgent>()
            .SingleInstance();

        builder.RegisterType<SubstitutionChainRunRepository>()
            .As<ISubstitutionChainRunRepository>()
            .InstancePerLifetimeScope();
    }

    /// <summary>
    /// Registers the opportunity scan agent (no Bing Grounding — works on DB data only).
    /// </summary>
    private static void RegisterOpportunityScanAgent(ContainerBuilder builder)
    {
        builder.RegisterType<AgentFrameworkOpportunityScanAgent>()
            .As<IOpportunityScanAgent>()
            .SingleInstance();

        builder.RegisterType<OpportunityScanRunRepository>()
            .As<IOpportunityScanRunRepository>()
            .InstancePerLifetimeScope();
    }
}
