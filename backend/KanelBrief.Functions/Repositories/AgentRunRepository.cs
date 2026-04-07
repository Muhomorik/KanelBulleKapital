using System.Text.Json;
using Azure.Data.Tables;
using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;

namespace KanelBrief.Functions.Repositories;

/// <summary>
/// Azure Tables implementation of the agent run repository.
/// Handles serialization of nested objects to JSON strings for Azure Tables storage.
/// </summary>
public class AgentRunRepository : IAgentRunRepository
{
    private readonly TableClient _newsBriefRunsTable;
    private readonly TableClient _weeklySummaryRunsTable;
    private readonly TableClient _substitutionChainRunsTable;
    private readonly TableClient _opportunityScanRunsTable;
    private readonly JsonSerializerOptions _jsonOptions;

    public AgentRunRepository(
        TableClient newsBriefRunsTable,
        TableClient weeklySummaryRunsTable,
        TableClient substitutionChainRunsTable,
        TableClient opportunityScanRunsTable)
    {
        _newsBriefRunsTable = newsBriefRunsTable;
        _weeklySummaryRunsTable = weeklySummaryRunsTable;
        _substitutionChainRunsTable = substitutionChainRunsTable;
        _opportunityScanRunsTable = opportunityScanRunsTable;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    // News Brief Runs

    public async Task SaveNewsBriefRunAsync(NewsBriefRun run)
    {
        var entity = new TableEntity(run.RunDate, run.RunId)
        {
            { "ModelId", run.ModelId },
            { "Status", run.Status.ToString() },
            { "DurationSeconds", run.DurationSeconds },
            { "InputTokens", run.InputTokens },
            { "OutputTokens", run.OutputTokens },
            { "TotalTokens", run.TotalTokens },
            { "DeploymentName", run.DeploymentName },
            { "Mood", run.Mood },
            { "Summary", run.Summary },
            { "Assessments", JsonSerializer.Serialize(run.Assessments, _jsonOptions) }
        };

        await _newsBriefRunsTable.UpsertEntityAsync(entity);
    }

    public async Task<NewsBriefRun?> GetNewsBriefRunAsync(string runDate, string runId)
    {
        try
        {
            var entity = await _newsBriefRunsTable.GetEntityAsync<TableEntity>(runDate, runId);
            return MapToNewsBriefRun(entity.Value);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<List<NewsBriefRun>> GetNewsBriefRunsByDateAsync(string runDate)
    {
        var query = _newsBriefRunsTable.QueryAsync<TableEntity>(e => e.PartitionKey == runDate);
        var results = new List<NewsBriefRun>();

        await foreach (var entity in query)
        {
            results.Add(MapToNewsBriefRun(entity));
        }

        return results;
    }

    // Weekly Summary Runs

    public async Task SaveWeeklySummaryRunAsync(WeeklySummaryRun run)
    {
        var entity = new TableEntity(run.RunDate, run.RunId)
        {
            { "ModelId", run.ModelId },
            { "Status", run.Status.ToString() },
            { "DurationSeconds", run.DurationSeconds },
            { "InputTokens", run.InputTokens },
            { "OutputTokens", run.OutputTokens },
            { "TotalTokens", run.TotalTokens },
            { "WeekStart", run.WeekStart },
            { "WeekEnd", run.WeekEnd },
            { "NetMood", run.NetMood.ToString() },
            { "MoodSummary", run.MoodSummary },
            { "Themes", JsonSerializer.Serialize(run.Themes, _jsonOptions) }
        };

        await _weeklySummaryRunsTable.UpsertEntityAsync(entity);
    }

    public async Task<WeeklySummaryRun?> GetWeeklySummaryRunAsync(string runDate, string runId)
    {
        try
        {
            var entity = await _weeklySummaryRunsTable.GetEntityAsync<TableEntity>(runDate, runId);
            return MapToWeeklySummaryRun(entity.Value);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<List<WeeklySummaryRun>> GetWeeklySummaryRunsByDateAsync(string runDate)
    {
        var query = _weeklySummaryRunsTable.QueryAsync<TableEntity>(e => e.PartitionKey == runDate);
        var results = new List<WeeklySummaryRun>();

        await foreach (var entity in query)
        {
            results.Add(MapToWeeklySummaryRun(entity));
        }

        return results;
    }

    // Substitution Chain Runs

    public async Task SaveSubstitutionChainRunAsync(SubstitutionChainRun run)
    {
        var entity = new TableEntity(run.RunDate, run.RunId)
        {
            { "ModelId", run.ModelId },
            { "Status", run.Status.ToString() },
            { "DurationSeconds", run.DurationSeconds },
            { "InputTokens", run.InputTokens },
            { "OutputTokens", run.OutputTokens },
            { "TotalTokens", run.TotalTokens },
            { "WeeklySummaryRunId", run.WeeklySummaryRunId },
            { "Chains", JsonSerializer.Serialize(run.Chains, _jsonOptions) }
        };

        await _substitutionChainRunsTable.UpsertEntityAsync(entity);
    }

    public async Task<SubstitutionChainRun?> GetSubstitutionChainRunAsync(string runDate, string runId)
    {
        try
        {
            var entity = await _substitutionChainRunsTable.GetEntityAsync<TableEntity>(runDate, runId);
            return MapToSubstitutionChainRun(entity.Value);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<List<SubstitutionChainRun>> GetSubstitutionChainRunsByDateAsync(string runDate)
    {
        var query = _substitutionChainRunsTable.QueryAsync<TableEntity>(e => e.PartitionKey == runDate);
        var results = new List<SubstitutionChainRun>();

        await foreach (var entity in query)
        {
            results.Add(MapToSubstitutionChainRun(entity));
        }

        return results;
    }

    // Opportunity Scan Runs

    public async Task SaveOpportunityScanRunAsync(OpportunityScanRun run)
    {
        var entity = new TableEntity(run.RunDate, run.RunId)
        {
            { "ModelId", run.ModelId },
            { "Status", run.Status.ToString() },
            { "DurationSeconds", run.DurationSeconds },
            { "InputTokens", run.InputTokens },
            { "OutputTokens", run.OutputTokens },
            { "TotalTokens", run.TotalTokens },
            { "SubstitutionChainRunId", run.SubstitutionChainRunId },
            { "Targets", JsonSerializer.Serialize(run.Targets, _jsonOptions) }
        };

        await _opportunityScanRunsTable.UpsertEntityAsync(entity);
    }

    public async Task<OpportunityScanRun?> GetOpportunityScanRunAsync(string runDate, string runId)
    {
        try
        {
            var entity = await _opportunityScanRunsTable.GetEntityAsync<TableEntity>(runDate, runId);
            return MapToOpportunityScanRun(entity.Value);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<List<OpportunityScanRun>> GetOpportunityScanRunsByDateAsync(string runDate)
    {
        var query = _opportunityScanRunsTable.QueryAsync<TableEntity>(e => e.PartitionKey == runDate);
        var results = new List<OpportunityScanRun>();

        await foreach (var entity in query)
        {
            results.Add(MapToOpportunityScanRun(entity));
        }

        return results;
    }

    // Mapping Helpers

    private NewsBriefRun MapToNewsBriefRun(TableEntity entity)
    {
        var assessments = string.IsNullOrEmpty(entity["Assessments"]?.ToString())
            ? []
            : JsonSerializer.Deserialize<List<CategoryAssessment>>(entity["Assessments"]!.ToString()!, _jsonOptions) ?? [];

        return new NewsBriefRun
        {
            RunDate = entity.PartitionKey,
            RunId = entity.RowKey,
            ModelId = entity["ModelId"]?.ToString() ?? string.Empty,
            Status = Enum.Parse<RunStatus>(entity["Status"]?.ToString() ?? "Failed"),
            DurationSeconds = (double)(entity["DurationSeconds"] ?? 0.0),
            InputTokens = (int)(entity["InputTokens"] ?? 0),
            OutputTokens = (int)(entity["OutputTokens"] ?? 0),
            TotalTokens = (int)(entity["TotalTokens"] ?? 0),
            DeploymentName = entity["DeploymentName"]?.ToString() ?? string.Empty,
            Mood = entity["Mood"]?.ToString() ?? string.Empty,
            Summary = entity["Summary"]?.ToString() ?? string.Empty,
            Assessments = assessments
        };
    }

    private WeeklySummaryRun MapToWeeklySummaryRun(TableEntity entity)
    {
        var themes = string.IsNullOrEmpty(entity["Themes"]?.ToString())
            ? []
            : JsonSerializer.Deserialize<List<WeeklySummaryTheme>>(entity["Themes"]!.ToString()!, _jsonOptions) ?? [];

        return new WeeklySummaryRun
        {
            RunDate = entity.PartitionKey,
            RunId = entity.RowKey,
            ModelId = entity["ModelId"]?.ToString() ?? string.Empty,
            Status = Enum.Parse<RunStatus>(entity["Status"]?.ToString() ?? "Failed"),
            DurationSeconds = (double)(entity["DurationSeconds"] ?? 0.0),
            InputTokens = (int)(entity["InputTokens"] ?? 0),
            OutputTokens = (int)(entity["OutputTokens"] ?? 0),
            TotalTokens = (int)(entity["TotalTokens"] ?? 0),
            WeekStart = (DateTimeOffset)(entity["WeekStart"] ?? DateTimeOffset.MinValue),
            WeekEnd = (DateTimeOffset)(entity["WeekEnd"] ?? DateTimeOffset.MinValue),
            NetMood = Enum.Parse<MarketSentiment>(entity["NetMood"]?.ToString() ?? "Mixed"),
            MoodSummary = entity["MoodSummary"]?.ToString() ?? string.Empty,
            Themes = themes
        };
    }

    private SubstitutionChainRun MapToSubstitutionChainRun(TableEntity entity)
    {
        var chains = string.IsNullOrEmpty(entity["Chains"]?.ToString())
            ? []
            : JsonSerializer.Deserialize<List<RotationChain>>(entity["Chains"]!.ToString()!, _jsonOptions) ?? [];

        return new SubstitutionChainRun
        {
            RunDate = entity.PartitionKey,
            RunId = entity.RowKey,
            ModelId = entity["ModelId"]?.ToString() ?? string.Empty,
            Status = Enum.Parse<RunStatus>(entity["Status"]?.ToString() ?? "Failed"),
            DurationSeconds = (double)(entity["DurationSeconds"] ?? 0.0),
            InputTokens = (int)(entity["InputTokens"] ?? 0),
            OutputTokens = (int)(entity["OutputTokens"] ?? 0),
            TotalTokens = (int)(entity["TotalTokens"] ?? 0),
            WeeklySummaryRunId = entity["WeeklySummaryRunId"]?.ToString() ?? string.Empty,
            Chains = chains
        };
    }

    private OpportunityScanRun MapToOpportunityScanRun(TableEntity entity)
    {
        var targets = string.IsNullOrEmpty(entity["Targets"]?.ToString())
            ? []
            : JsonSerializer.Deserialize<List<RotationTarget>>(entity["Targets"]!.ToString()!, _jsonOptions) ?? [];

        return new OpportunityScanRun
        {
            RunDate = entity.PartitionKey,
            RunId = entity.RowKey,
            ModelId = entity["ModelId"]?.ToString() ?? string.Empty,
            Status = Enum.Parse<RunStatus>(entity["Status"]?.ToString() ?? "Failed"),
            DurationSeconds = (double)(entity["DurationSeconds"] ?? 0.0),
            InputTokens = (int)(entity["InputTokens"] ?? 0),
            OutputTokens = (int)(entity["OutputTokens"] ?? 0),
            TotalTokens = (int)(entity["TotalTokens"] ?? 0),
            SubstitutionChainRunId = entity["SubstitutionChainRunId"]?.ToString() ?? string.Empty,
            Targets = targets
        };
    }
}
