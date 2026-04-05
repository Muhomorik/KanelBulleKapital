using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.ValueObjects;
using FluentResults;
using NLog;

namespace FikaForecast.Application.Services;

/// <summary>
/// Runs the Weekly Summary Agent for a set of daily briefs: formats input,
/// calls the LLM, parses JSON output, renders display markdown, and persists everything.
/// </summary>
public class WeeklySummaryOrchestrator
{
    private readonly ILogger _logger;
    private readonly IWeeklySummaryAgent _agent;
    private readonly IWeeklySummaryRunRepository _repository;
    private readonly IWeeklySummaryParser _parser;
    private readonly WeeklySummaryMarkdownRenderer _renderer;
    private readonly WeeklySummaryInputFormatter _inputFormatter;

    public WeeklySummaryOrchestrator(
        ILogger logger,
        IWeeklySummaryAgent agent,
        IWeeklySummaryRunRepository repository,
        IWeeklySummaryParser parser,
        WeeklySummaryMarkdownRenderer renderer,
        WeeklySummaryInputFormatter inputFormatter)
    {
        _logger = logger;
        _agent = agent;
        _repository = repository;
        _parser = parser;
        _renderer = renderer;
        _inputFormatter = inputFormatter;
    }

    /// <summary>
    /// Formats daily briefs as input, executes the agent, parses JSON into structured themes,
    /// renders display markdown, and saves the run to the repository.
    /// </summary>
    public async Task<Result<WeeklySummaryRun>> RunSummaryAsync(
        ModelConfig model,
        AgentPrompt prompt,
        IReadOnlyList<NewsBriefRun> dailyRuns,
        CancellationToken cancellationToken = default)
    {
        if (dailyRuns.Count == 0)
            return Result.Fail<WeeklySummaryRun>("No daily runs to summarize");

        var orderedRuns = dailyRuns.OrderBy(r => r.Timestamp).ToList();
        var weekStart = orderedRuns.First().Timestamp;
        var weekEnd = orderedRuns.Last().Timestamp;

        var run = WeeklySummaryRun.Start(model, weekStart, weekEnd);

        // Format daily briefs into text input
        var inputText = _inputFormatter.Format(dailyRuns);
        if (string.IsNullOrWhiteSpace(inputText))
        {
            _logger.Warn("Input formatter produced empty text — no valid briefs to summarize");
            run.Fail(TimeSpan.Zero);
            await _repository.SaveAsync(run, cancellationToken);
            return Result.Fail<WeeklySummaryRun>("No valid daily briefs to summarize");
        }

        _logger.Info("Running Weekly Summary Agent with model {0} — {1} daily runs ({2} to {3})",
            model.DisplayName, dailyRuns.Count,
            weekStart.ToString("yyyy-MM-dd"), weekEnd.ToString("yyyy-MM-dd"));

        var agentResult = await _agent.RunAsync(model, prompt, inputText, cancellationToken);

        if (agentResult.IsFailed)
        {
            _logger.Error("Weekly Summary Agent failed for model {0}: {1}",
                model.DisplayName, agentResult.Errors.First().Message);
            run.Fail(TimeSpan.Zero);
            await _repository.SaveAsync(run, cancellationToken);
            return Result.Fail<WeeklySummaryRun>(agentResult.Errors);
        }

        var result = agentResult.Value;
        run.Complete(
            result.RawOutput,
            result.Duration,
            result.InputTokens,
            result.OutputTokens);

        // Parse JSON into structured domain objects
        var parseResult = _parser.Parse(result.RawOutput);

        run.SetMood(parseResult.NetMood, parseResult.MoodSummary);

        foreach (var theme in parseResult.Themes)
            run.AddTheme(theme);

        // Render display markdown from structured data
        var displayMarkdown = _renderer.Render(parseResult, weekStart, weekEnd);
        if (!string.IsNullOrEmpty(displayMarkdown))
            run.SetDisplayMarkdown(displayMarkdown);

        if (!parseResult.IsComplete)
        {
            _logger.Warn("Parsing incomplete for run {0}: {1} warnings",
                run.RunId, parseResult.Warnings.Count);
            run.MarkPartial();
        }

        await _repository.SaveAsync(run, cancellationToken);
        return Result.Ok(run);
    }
}
