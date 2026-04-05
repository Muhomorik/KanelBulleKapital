using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.ValueObjects;
using FluentResults;
using NLog;

namespace FikaForecast.Application.Services;

/// <summary>
/// Runs the Substitution Chain Agent for a weekly summary: formats input,
/// calls the LLM, parses JSON output, renders display markdown, and persists everything.
/// </summary>
public class SubstitutionChainOrchestrator
{
    private readonly ILogger _logger;
    private readonly ISubstitutionChainAgent _agent;
    private readonly ISubstitutionChainRunRepository _repository;
    private readonly ISubstitutionChainParser _parser;
    private readonly SubstitutionChainMarkdownRenderer _renderer;
    private readonly SubstitutionChainInputFormatter _inputFormatter;

    public SubstitutionChainOrchestrator(
        ILogger logger,
        ISubstitutionChainAgent agent,
        ISubstitutionChainRunRepository repository,
        ISubstitutionChainParser parser,
        SubstitutionChainMarkdownRenderer renderer,
        SubstitutionChainInputFormatter inputFormatter)
    {
        _logger = logger;
        _agent = agent;
        _repository = repository;
        _parser = parser;
        _renderer = renderer;
        _inputFormatter = inputFormatter;
    }

    /// <summary>
    /// Formats the weekly summary as input, executes the agent, parses JSON into structured
    /// rotation chains, renders display markdown, and saves the run to the repository.
    /// </summary>
    public async Task<Result<SubstitutionChainRun>> RunAsync(
        ModelConfig model,
        AgentPrompt prompt,
        WeeklySummaryRun weeklySummaryRun,
        CancellationToken cancellationToken = default)
    {
        if (weeklySummaryRun.Themes.Count == 0)
            return Result.Fail<SubstitutionChainRun>("Weekly summary has no themes to analyze");

        var run = SubstitutionChainRun.Start(model, weeklySummaryRun.RunId);

        // Format weekly summary into text input
        var inputText = _inputFormatter.Format(weeklySummaryRun);
        if (string.IsNullOrWhiteSpace(inputText))
        {
            _logger.Warn("Input formatter produced empty text — no valid themes to analyze");
            run.Fail(TimeSpan.Zero);
            await _repository.SaveAsync(run, cancellationToken);
            return Result.Fail<SubstitutionChainRun>("No valid weekly summary data to analyze");
        }

        _logger.Info("Running Substitution Chain Agent with model {0} — {1} themes from summary {2}",
            model.DisplayName, weeklySummaryRun.Themes.Count, weeklySummaryRun.RunId);

        var agentResult = await _agent.RunAsync(model, prompt, inputText, cancellationToken);

        if (agentResult.IsFailed)
        {
            _logger.Error("Substitution Chain Agent failed for model {0}: {1}",
                model.DisplayName, agentResult.Errors.First().Message);
            run.Fail(TimeSpan.Zero);
            await _repository.SaveAsync(run, cancellationToken);
            return Result.Fail<SubstitutionChainRun>(agentResult.Errors);
        }

        var result = agentResult.Value;
        run.Complete(
            result.RawOutput,
            result.Duration,
            result.InputTokens,
            result.OutputTokens);

        // Parse JSON into structured domain objects
        var parseResult = _parser.Parse(result.RawOutput);

        foreach (var chain in parseResult.Chains)
            run.AddChain(chain);

        // Render display markdown from structured data
        var displayMarkdown = _renderer.Render(
            parseResult, weeklySummaryRun.WeekStart, weeklySummaryRun.WeekEnd);
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
