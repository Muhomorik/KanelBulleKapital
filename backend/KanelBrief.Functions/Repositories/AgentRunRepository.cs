using System.Text.Json;
using Azure.Data.Tables;
using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;
using KanelBrief.Core.Serialization;

namespace KanelBrief.Functions.Repositories;

/// <summary>
/// Azure Tables implementation of the agent run repository.
/// Handles serialization of nested objects to JSON strings for Azure Tables storage.
/// </summary>
public class AgentRunRepository : IAgentRunRepository
{
    internal static class BaseColumns
    {
        public const string ModelId = "ModelId";
        public const string Status = "Status";
        public const string DurationSeconds = "DurationSeconds";
        public const string InputTokens = "InputTokens";
        public const string OutputTokens = "OutputTokens";
        public const string TotalTokens = "TotalTokens";
        public const string CreatedAt = "CreatedAt";
    }

    internal static class NewsBriefColumns
    {
        public const string DeploymentName = "DeploymentName";
        public const string Mood = "Mood";
        public const string Summary = "Summary";
        public const string Assessments = "Assessments";
    }

    internal static class WeeklySummaryColumns
    {
        public const string WeekStart = "WeekStart";
        public const string WeekEnd = "WeekEnd";
        public const string NetMood = "NetMood";
        public const string MoodSummary = "MoodSummary";
        public const string Themes = "Themes";
    }

    internal static class SubstitutionChainColumns
    {
        public const string WeeklySummaryRunId = "WeeklySummaryRunId";
        public const string Chains = "Chains";
    }

    internal static class OpportunityScanColumns
    {
        public const string SubstitutionChainRunId = "SubstitutionChainRunId";
        public const string Targets = "Targets";
    }

    private readonly TableClient _newsBriefRunsTable;
    private readonly TableClient _weeklySummaryRunsTable;
    private readonly TableClient _substitutionChainRunsTable;
    private readonly TableClient _opportunityScanRunsTable;

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
    }

    // News Brief Runs

    public async Task SaveNewsBriefRunAsync(NewsBriefRun run)
    {
        var entity = new TableEntity(run.RunDate, run.RunId)
        {
            { BaseColumns.ModelId, run.ModelId },
            { BaseColumns.Status, run.Status.ToString() },
            { BaseColumns.DurationSeconds, run.DurationSeconds },
            { BaseColumns.InputTokens, run.InputTokens },
            { BaseColumns.OutputTokens, run.OutputTokens },
            { BaseColumns.TotalTokens, run.TotalTokens },
            { BaseColumns.CreatedAt, run.CreatedAt },
            { NewsBriefColumns.DeploymentName, run.DeploymentName },
            { NewsBriefColumns.Mood, run.Mood },
            { NewsBriefColumns.Summary, run.Summary },
            { NewsBriefColumns.Assessments, JsonSerializer.Serialize(run.Assessments, KanelJsonOptions.CamelCase) }
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

    /// <summary>
    /// Returns all news brief runs for a given date, sorted newest-first by <see cref="NewsBriefRun.CreatedAt"/>.
    /// Callers (dashboard API, weekly aggregator, frontend) rely on this ordering contract:
    /// <c>result[0]</c> is the latest brief of the day.
    /// </summary>
    public async Task<List<NewsBriefRun>> GetNewsBriefRunsByDateAsync(string runDate)
    {
        var query = _newsBriefRunsTable.QueryAsync<TableEntity>(e => e.PartitionKey == runDate);
        var results = new List<NewsBriefRun>();

        await foreach (var entity in query)
        {
            results.Add(MapToNewsBriefRun(entity));
        }

        results.Sort(static (a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
        return results;
    }

    // Weekly Summary Runs

    public async Task SaveWeeklySummaryRunAsync(WeeklySummaryRun run)
    {
        var entity = new TableEntity(run.RunDate, run.RunId)
        {
            { BaseColumns.ModelId, run.ModelId },
            { BaseColumns.Status, run.Status.ToString() },
            { BaseColumns.DurationSeconds, run.DurationSeconds },
            { BaseColumns.InputTokens, run.InputTokens },
            { BaseColumns.OutputTokens, run.OutputTokens },
            { BaseColumns.TotalTokens, run.TotalTokens },
            { WeeklySummaryColumns.WeekStart, run.WeekStart },
            { WeeklySummaryColumns.WeekEnd, run.WeekEnd },
            { WeeklySummaryColumns.NetMood, run.NetMood.ToString() },
            { WeeklySummaryColumns.MoodSummary, run.MoodSummary },
            { WeeklySummaryColumns.Themes, JsonSerializer.Serialize(run.Themes, KanelJsonOptions.CamelCase) }
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
            { BaseColumns.ModelId, run.ModelId },
            { BaseColumns.Status, run.Status.ToString() },
            { BaseColumns.DurationSeconds, run.DurationSeconds },
            { BaseColumns.InputTokens, run.InputTokens },
            { BaseColumns.OutputTokens, run.OutputTokens },
            { BaseColumns.TotalTokens, run.TotalTokens },
            { SubstitutionChainColumns.WeeklySummaryRunId, run.WeeklySummaryRunId },
            { SubstitutionChainColumns.Chains, JsonSerializer.Serialize(run.Chains, KanelJsonOptions.CamelCase) }
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
            { BaseColumns.ModelId, run.ModelId },
            { BaseColumns.Status, run.Status.ToString() },
            { BaseColumns.DurationSeconds, run.DurationSeconds },
            { BaseColumns.InputTokens, run.InputTokens },
            { BaseColumns.OutputTokens, run.OutputTokens },
            { BaseColumns.TotalTokens, run.TotalTokens },
            { OpportunityScanColumns.SubstitutionChainRunId, run.SubstitutionChainRunId },
            { OpportunityScanColumns.Targets, JsonSerializer.Serialize(run.Targets, KanelJsonOptions.CamelCase) }
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

    internal NewsBriefRun MapToNewsBriefRun(TableEntity entity)
    {
        var assessments = string.IsNullOrEmpty(entity[NewsBriefColumns.Assessments]?.ToString())
            ? []
            : JsonSerializer.Deserialize<List<CategoryAssessment>>(entity[NewsBriefColumns.Assessments]!.ToString()!, KanelJsonOptions.CamelCase) ?? [];

        return new NewsBriefRun
        {
            RunDate = entity.PartitionKey,
            RunId = entity.RowKey,
            CreatedAt = entity.GetDateTimeOffset(BaseColumns.CreatedAt) ?? entity.Timestamp ?? DateTimeOffset.MinValue,
            ModelId = entity[BaseColumns.ModelId]?.ToString() ?? string.Empty,
            Status = Enum.Parse<RunStatus>(entity[BaseColumns.Status]?.ToString() ?? "Failed"),
            DurationSeconds = (double)(entity[BaseColumns.DurationSeconds] ?? 0.0),
            InputTokens = (int)(entity[BaseColumns.InputTokens] ?? 0),
            OutputTokens = (int)(entity[BaseColumns.OutputTokens] ?? 0),
            TotalTokens = (int)(entity[BaseColumns.TotalTokens] ?? 0),
            DeploymentName = entity[NewsBriefColumns.DeploymentName]?.ToString() ?? string.Empty,
            Mood = entity[NewsBriefColumns.Mood]?.ToString() ?? string.Empty,
            Summary = entity[NewsBriefColumns.Summary]?.ToString() ?? string.Empty,
            Assessments = assessments
        };
    }

    internal WeeklySummaryRun MapToWeeklySummaryRun(TableEntity entity)
    {
        var themes = string.IsNullOrEmpty(entity[WeeklySummaryColumns.Themes]?.ToString())
            ? []
            : JsonSerializer.Deserialize<List<WeeklySummaryTheme>>(entity[WeeklySummaryColumns.Themes]!.ToString()!, KanelJsonOptions.CamelCase) ?? [];

        return new WeeklySummaryRun
        {
            RunDate = entity.PartitionKey,
            RunId = entity.RowKey,
            ModelId = entity[BaseColumns.ModelId]?.ToString() ?? string.Empty,
            Status = Enum.Parse<RunStatus>(entity[BaseColumns.Status]?.ToString() ?? "Failed"),
            DurationSeconds = (double)(entity[BaseColumns.DurationSeconds] ?? 0.0),
            InputTokens = (int)(entity[BaseColumns.InputTokens] ?? 0),
            OutputTokens = (int)(entity[BaseColumns.OutputTokens] ?? 0),
            TotalTokens = (int)(entity[BaseColumns.TotalTokens] ?? 0),
            WeekStart = (DateTimeOffset)(entity[WeeklySummaryColumns.WeekStart] ?? DateTimeOffset.MinValue),
            WeekEnd = (DateTimeOffset)(entity[WeeklySummaryColumns.WeekEnd] ?? DateTimeOffset.MinValue),
            NetMood = Enum.Parse<MarketSentiment>(entity[WeeklySummaryColumns.NetMood]?.ToString() ?? "Mixed"),
            MoodSummary = entity[WeeklySummaryColumns.MoodSummary]?.ToString() ?? string.Empty,
            Themes = themes
        };
    }

    internal SubstitutionChainRun MapToSubstitutionChainRun(TableEntity entity)
    {
        var chains = string.IsNullOrEmpty(entity[SubstitutionChainColumns.Chains]?.ToString())
            ? []
            : JsonSerializer.Deserialize<List<RotationChain>>(entity[SubstitutionChainColumns.Chains]!.ToString()!, KanelJsonOptions.CamelCase) ?? [];

        return new SubstitutionChainRun
        {
            RunDate = entity.PartitionKey,
            RunId = entity.RowKey,
            ModelId = entity[BaseColumns.ModelId]?.ToString() ?? string.Empty,
            Status = Enum.Parse<RunStatus>(entity[BaseColumns.Status]?.ToString() ?? "Failed"),
            DurationSeconds = (double)(entity[BaseColumns.DurationSeconds] ?? 0.0),
            InputTokens = (int)(entity[BaseColumns.InputTokens] ?? 0),
            OutputTokens = (int)(entity[BaseColumns.OutputTokens] ?? 0),
            TotalTokens = (int)(entity[BaseColumns.TotalTokens] ?? 0),
            WeeklySummaryRunId = entity[SubstitutionChainColumns.WeeklySummaryRunId]?.ToString() ?? string.Empty,
            Chains = chains
        };
    }

    internal OpportunityScanRun MapToOpportunityScanRun(TableEntity entity)
    {
        var targets = string.IsNullOrEmpty(entity[OpportunityScanColumns.Targets]?.ToString())
            ? []
            : JsonSerializer.Deserialize<List<RotationTarget>>(entity[OpportunityScanColumns.Targets]!.ToString()!, KanelJsonOptions.CamelCase) ?? [];

        return new OpportunityScanRun
        {
            RunDate = entity.PartitionKey,
            RunId = entity.RowKey,
            ModelId = entity[BaseColumns.ModelId]?.ToString() ?? string.Empty,
            Status = Enum.Parse<RunStatus>(entity[BaseColumns.Status]?.ToString() ?? "Failed"),
            DurationSeconds = (double)(entity[BaseColumns.DurationSeconds] ?? 0.0),
            InputTokens = (int)(entity[BaseColumns.InputTokens] ?? 0),
            OutputTokens = (int)(entity[BaseColumns.OutputTokens] ?? 0),
            TotalTokens = (int)(entity[BaseColumns.TotalTokens] ?? 0),
            SubstitutionChainRunId = entity[OpportunityScanColumns.SubstitutionChainRunId]?.ToString() ?? string.Empty,
            Targets = targets
        };
    }
}
