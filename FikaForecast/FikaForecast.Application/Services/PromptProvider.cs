using System.Collections.Concurrent;
using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.ValueObjects;
using NLog;

namespace FikaForecast.Application.Services;

/// <summary>
/// Loads agent prompts from external files in AppData with embedded resource fallback.
/// Caches prompts in memory for the lifetime of the application.
/// </summary>
public class PromptProvider : IPromptProvider
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private const string NewsBriefKey = "newsbrief";
    private const string ComparisonKey = "comparison";
    private const string EvaluationKey = "evaluation";
    private const string WeeklySummaryKey = "weeklysummary";

    private readonly IPromptFileService _promptFileService;
    private readonly ConcurrentDictionary<string, AgentPrompt> _cache = new();

    public PromptProvider(IPromptFileService promptFileService)
    {
        _promptFileService = promptFileService;
    }

    /// <inheritdoc />
    public AgentPrompt GetNewsBriefPrompt() => GetOrLoadPrompt(NewsBriefKey);

    /// <inheritdoc />
    public AgentPrompt GetComparisonPrompt() => GetOrLoadPrompt(ComparisonKey);

    /// <inheritdoc />
    public AgentPrompt GetEvaluationPrompt() => GetOrLoadPrompt(EvaluationKey);

    /// <inheritdoc />
    public AgentPrompt GetWeeklySummaryPrompt() => GetOrLoadPrompt(WeeklySummaryKey);

    /// <inheritdoc />
    public void InvalidateCache()
    {
        _cache.Clear();
        Logger.Info("Prompt cache invalidated — prompts will be re-read from disk on next access");
    }

    private AgentPrompt GetOrLoadPrompt(string promptKey)
    {
        return _cache.GetOrAdd(promptKey, key =>
        {
            Logger.Debug("Loading prompt '{PromptKey}' from file", key);

            var content = _promptFileService.ReadPromptFile(key);
            var (name, systemPrompt) = PromptFileParser.Parse(content, fallbackName: key);

            return new AgentPrompt(name, systemPrompt);
        });
    }
}
