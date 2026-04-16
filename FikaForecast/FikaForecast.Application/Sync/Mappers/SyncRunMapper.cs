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
        var markdown = _weeklySummaryRenderer.Render(parseResult, dto.WeekStart, dto.WeekEnd);

        return WeeklySummaryRun.Rehydrate(
            runId: Guid.Parse(dto.RunId),
            weekStart: dto.WeekStart,
            weekEnd: dto.WeekEnd,
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
        // Renderer accepts weekStart/weekEnd but doesn't use them in current implementation.
        var markdown = _substitutionChainRenderer.Render(parseResult, dto.CreatedAt, dto.CreatedAt);

        return SubstitutionChainRun.Rehydrate(
            runId: Guid.Parse(dto.RunId),
            weeklySummaryRunId: Guid.Parse(dto.WeeklySummaryRunId),
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

    // ── Enum mapping helpers ──────────────────────────────────────────────
    // Backend and WPF enums have different orderings for some types.
    // Backend serializes enums as integers (no JsonStringEnumConverter).

    /// <summary>RunStatus: same ordering on both sides — direct cast is safe.</summary>
    private static RunStatus MapStatus(int value) => (RunStatus)value;

    /// <summary>
    /// Backend MarketSentiment: RiskOn=0, RiskOff=1, Mixed=2.
    /// WPF MarketSentiment: RiskOff=0, RiskOn=1, Mixed=2.
    /// </summary>
    private static MarketSentiment MapSentiment(int value) => value switch
    {
        0 => MarketSentiment.RiskOn,
        1 => MarketSentiment.RiskOff,
        2 => MarketSentiment.Mixed,
        _ => MarketSentiment.Mixed
    };

    /// <summary>NewsBriefRun.Mood is stored as a string on the backend, not an enum.</summary>
    private static MarketSentiment MapMoodString(string mood) => mood switch
    {
        "RiskOn" => MarketSentiment.RiskOn,
        "RiskOff" => MarketSentiment.RiskOff,
        "Mixed" => MarketSentiment.Mixed,
        _ => MarketSentiment.Mixed
    };

    /// <summary>
    /// Backend ConfidenceLevel: High=0, Medium=1, Low=2.
    /// WPF ConfidenceLevel: High=0, Moderate=1, Dropped=2.
    /// </summary>
    private static ConfidenceLevel MapConfidence(int value) => value switch
    {
        0 => ConfidenceLevel.High,
        1 => ConfidenceLevel.Moderate,
        2 => ConfidenceLevel.Dropped,
        _ => ConfidenceLevel.Moderate
    };

    /// <summary>
    /// Backend SignalStrength: Strong=0, Moderate=1, Weak=2.
    /// WPF SignalStrength: Strong=0, Moderate=1 (no Weak).
    /// </summary>
    private static SignalStrength MapSignalStrength(int value) => value switch
    {
        0 => SignalStrength.Strong,
        1 => SignalStrength.Moderate,
        2 => SignalStrength.Moderate, // Weak has no WPF equivalent
        _ => SignalStrength.Moderate
    };
}
