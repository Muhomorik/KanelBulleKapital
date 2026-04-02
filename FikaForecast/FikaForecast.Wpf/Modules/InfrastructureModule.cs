using System.IO;
using Autofac;
using Azure.AI.Projects;
using Azure.Identity;
using FikaForecast.Application.Interfaces;
using FikaForecast.Infrastructure.Agents;
using FikaForecast.Infrastructure.Persistence;
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

    public InfrastructureModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        RegisterDatabase(builder);
        RegisterAgentClient(builder);
        RegisterAgent(builder);
        RegisterEvaluationAgent(builder);
    }

    private static void RegisterDatabase(ContainerBuilder builder)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FikaForecast",
            "fikaforecast.db");
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
}
