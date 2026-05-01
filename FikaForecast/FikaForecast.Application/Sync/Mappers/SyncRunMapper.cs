using FikaForecast.Application.DTOs;
using FikaForecast.Application.Services;
using FikaForecast.Application.Sync.Dtos;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Application.Sync.Mappers;

/// <summary>
/// Translates sync wire DTOs into domain aggregates, regenerating markdown client-side.
/// </summary>
public class SyncRunMapper
{
    private readonly NewsBriefMarkdownRenderer _newsBriefRenderer;
    private readonly WeeklySummaryMarkdownRenderer _weeklySummaryRenderer;
    private readonly SubstitutionChainMarkdownRenderer _substitutionChainRenderer;
    private readonly OpportunityScanMarkdownRenderer _opportunityScanRenderer;

    public SyncRunMapper(
        NewsBriefMarkdownRenderer newsBriefRenderer,
        WeeklySummaryMarkdownRenderer weeklySummaryRenderer,
        SubstitutionChainMarkdownRenderer substitutionChainRenderer,
        OpportunityScanMarkdownRenderer opportunityScanRenderer)
    {
        _newsBriefRenderer = newsBriefRenderer;
        _weeklySummaryRenderer = weeklySummaryRenderer;
        _substitutionChainRenderer = substitutionChainRenderer;
        _opportunityScanRenderer = opportunityScanRenderer;
    }

    public NewsBriefRun ToDomain(SyncNewsBriefRun dto)
    {
        var mood = MapMoodString(dto.Mood);
        var assessments = dto.Assessments
            .Select(a => new CategoryAssessment(a.Category, a.Headline, a.Summary, MapSentiment(a.Sentiment)))
            .ToList();

        var item = new NewsItem(mood, dto.Summary);
        foreach (var assessment in assessments)
            item.AddAssessment(assessment);

        var parseResult = new NewsBriefParseResult(item, IsComplete: true, Warnings: []);
        var markdown = _newsBriefRenderer.Render(parseResult);

        return NewsBriefRun.Rehydrate(
            runId: Guid.Parse(dto.RunId),
            timestamp: dto.CreatedAt,
            modelId: dto.ModelId,
            deploymentName: dto.DeploymentName,
            promptName: string.Empty,
            duration: TimeSpan.FromSeconds(dto.DurationSeconds),
            inputTokens: dto.InputTokens,
            outputTokens: dto.OutputTokens,
            totalTokens: dto.TotalTokens,
            status: MapStatus(dto.Status),
            rawAgentOutput: string.Empty,
            rawMarkdownOutput: markdown,
            item: item);
    }

    public WeeklySummaryRun ToDomain(SyncWeeklySummaryRun dto)
    {
        var themes = dto.Themes
            .Select(t => new WeeklySummaryTheme(
                t.Category, t.Summary, MapConfidence(t.Confidence), MapSentiment(t.Sentiment)))
            .ToList();

        var netMood = MapSentiment(dto.NetMood);
        var parseResult = new WeeklySummaryParseResult(netMood, dto.MoodSummary, themes, IsComplete: true, Warnings: []);
        var markdown = _weeklySummaryRenderer.Render(parseResult, dto.PeriodStart, dto.PeriodEnd);

        return WeeklySummaryRun.Rehydrate(
            runId: Guid.Parse(dto.RunId),
            periodStart: dto.PeriodStart,
            periodEnd: dto.PeriodEnd,
            periodIsoWeek: dto.PeriodIsoWeek,
            timestamp: dto.CreatedAt,
            modelId: dto.ModelId,
            duration: TimeSpan.FromSeconds(dto.DurationSeconds),
            inputTokens: dto.InputTokens,
            outputTokens: dto.OutputTokens,
            totalTokens: dto.TotalTokens,
            status: MapStatus(dto.Status),
            rawAgentOutput: string.Empty,
            rawMarkdownOutput: markdown,
            netMood: netMood,
            moodSummary: dto.MoodSummary,
            themes: themes);
    }

    public SubstitutionChainRun ToDomain(SyncSubstitutionChainRun dto)
    {
        var chains = dto.Chains
            .Select(c => new RotationChain(c.CapitalFleeing, c.FlowsToward, c.Mechanism))
            .ToList();

        var parseResult = new SubstitutionChainParseResult(chains, IsComplete: true, Warnings: []);
        // Renderer takes period for header rendering — backend now ships these via lazy-fill.
        var markdown = _substitutionChainRenderer.Render(parseResult, dto.PeriodStart, dto.PeriodEnd);

        return SubstitutionChainRun.Rehydrate(
            runId: Guid.Parse(dto.RunId),
            weeklySummaryRunId: Guid.Parse(dto.WeeklySummaryRunId),
            periodStart: dto.PeriodStart,
            periodEnd: dto.PeriodEnd,
            periodIsoWeek: dto.PeriodIsoWeek,
            timestamp: dto.CreatedAt,
            modelId: dto.ModelId,
            duration: TimeSpan.FromSeconds(dto.DurationSeconds),
            inputTokens: dto.InputTokens,
            outputTokens: dto.OutputTokens,
            totalTokens: dto.TotalTokens,
            status: MapStatus(dto.Status),
            rawAgentOutput: string.Empty,
            rawMarkdownOutput: markdown,
            chains: chains);
    }

    public OpportunityScanRun ToDomain(SyncOpportunityScanRun dto)
    {
        var targets = dto.Targets
            .Select(t => new RotationTarget(t.Category, MapSignalStrength(t.SignalStrength), t.Rationale, t.RiskCaveat))
            .ToList();

        var parseResult = new OpportunityScanParseResult(targets, IsComplete: true, Warnings: []);
        var markdown = _opportunityScanRenderer.Render(parseResult);

        return OpportunityScanRun.Rehydrate(
            runId: Guid.Parse(dto.RunId),
            substitutionChainRunId: Guid.Parse(dto.SubstitutionChainRunId),
            periodStart: dto.PeriodStart,
            periodEnd: dto.PeriodEnd,
            periodIsoWeek: dto.PeriodIsoWeek,
            timestamp: dto.CreatedAt,
            modelId: dto.ModelId,
            duration: TimeSpan.FromSeconds(dto.DurationSeconds),
            inputTokens: dto.InputTokens,
            outputTokens: dto.OutputTokens,
            totalTokens: dto.TotalTokens,
            status: MapStatus(dto.Status),
            rawAgentOutput: string.Empty,
            rawMarkdownOutput: markdown,
            targets: targets);
    }

    #region Enum mapping helpers
    // Backend enums use [JsonStringEnumConverter] and emit their member names.
    // WPF-side enum members don't always match — these switches bridge the gap.

    public static RunStatus MapStatus(string s) => s switch
    {
        "Success" => RunStatus.Success,
        "Failed" => RunStatus.Failed,
        _ => RunStatus.Failed  // includes "Partial" — obsolete on backend, no WPF equivalent
    };

    public static MarketSentiment MapSentiment(string s) => s switch
    {
        "RiskOn" => MarketSentiment.RiskOn,
        "RiskOff" => MarketSentiment.RiskOff,
        "Mixed" => MarketSentiment.Mixed,
        _ => MarketSentiment.Mixed
    };

    /// <summary>NewsBriefRun.Mood is stored as a string on the backend (not the enum).</summary>
    public static MarketSentiment MapMoodString(string mood) => MapSentiment(mood);

    /// <summary>Backend <c>Medium</c> → WPF <c>Moderate</c>, <c>Low</c> → WPF <c>Dropped</c>.</summary>
    public static ConfidenceLevel MapConfidence(string s) => s switch
    {
        "High" => ConfidenceLevel.High,
        "Medium" => ConfidenceLevel.Moderate,
        "Low" => ConfidenceLevel.Dropped,
        _ => ConfidenceLevel.Moderate
    };

    /// <summary>Backend has <c>Weak</c>; WPF doesn't — folds to <c>Moderate</c>.</summary>
    public static SignalStrength MapSignalStrength(string s) => s switch
    {
        "Strong" => SignalStrength.Strong,
        "Moderate" => SignalStrength.Moderate,
        "Weak" => SignalStrength.Moderate,
        _ => SignalStrength.Moderate
    };

    #endregion
}
