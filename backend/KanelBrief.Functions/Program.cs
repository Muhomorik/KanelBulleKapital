using Azure.AI.Projects;
using Azure.Data.Tables;
using Azure.Identity;
using KanelBrief.Core.Repositories;
using KanelBrief.Functions.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

// Azure Tables setup — Managed Identity in Azure, Azurite locally
var storageConnectionString = builder.Configuration["ConnectionStrings:AzureWebJobsStorage"]
    ?? builder.Configuration["AzureWebJobsStorage"];

builder.Services.AddSingleton(sp =>
{
    var isLocalDev = string.Equals(storageConnectionString, "UseDevelopmentStorage=true",
        StringComparison.OrdinalIgnoreCase);

    var tableServiceClient = isLocalDev
        ? new TableServiceClient(storageConnectionString)
        : new TableServiceClient(
            new Uri(builder.Configuration["TableStorageUri"]
                ?? throw new InvalidOperationException("TableStorageUri not configured")),
            new DefaultAzureCredential());

    // Ensure tables exist (creates if not present)
    tableServiceClient.GetTableClient("NewsBriefRuns").CreateIfNotExistsAsync().GetAwaiter().GetResult();
    tableServiceClient.GetTableClient("WeeklySummaryRuns").CreateIfNotExistsAsync().GetAwaiter().GetResult();
    tableServiceClient.GetTableClient("SubstitutionChainRuns").CreateIfNotExistsAsync().GetAwaiter().GetResult();
    tableServiceClient.GetTableClient("OpportunityScanRuns").CreateIfNotExistsAsync().GetAwaiter().GetResult();

    return tableServiceClient;
});

// Register individual table clients
builder.Services.AddSingleton(sp => sp.GetRequiredService<TableServiceClient>().GetTableClient("NewsBriefRuns"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<TableServiceClient>().GetTableClient("WeeklySummaryRuns"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<TableServiceClient>().GetTableClient("SubstitutionChainRuns"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<TableServiceClient>().GetTableClient("OpportunityScanRuns"));

// Register repository (keyed to distinguish between multiple table clients)
builder.Services.AddScoped<IAgentRunRepository>(sp =>
{
    var tableServiceClient = sp.GetRequiredService<TableServiceClient>();
    return new AgentRunRepository(
        tableServiceClient.GetTableClient("NewsBriefRuns"),
        tableServiceClient.GetTableClient("WeeklySummaryRuns"),
        tableServiceClient.GetTableClient("SubstitutionChainRuns"),
        tableServiceClient.GetTableClient("OpportunityScanRuns")
    );
});

// Register AIProjectClient for Agent Framework
builder.Services.AddSingleton(sp =>
{
    var foundryEndpoint = builder.Configuration["FOUNDRY_PROJECT_ENDPOINT"]
        ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT not configured");

    return new AIProjectClient(
        new Uri(foundryEndpoint),
        new DefaultAzureCredential());
});

builder.Build().Run();
