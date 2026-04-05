using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.ValueObjects;
using FluentResults;
using NLog;

namespace FikaForecast.Application.Services;

/// <summary>
/// Runs the Opportunity Scan Agent for a substitution chain run: formats input,
/// calls the LLM, parses JSON output, renders display markdown, and persists everything.
/// </summary>
public class OpportunityScanOrchestrator
{
    private readonly ILogger _logger;
    private readonly IOpportunityScanAgent _agent;
    private readonly IOpportunityScanRunRepository _repository;
    private readonly IOpportunityScanParser _parser;
    private readonly OpportunityScanMarkdownRenderer _renderer;
    private readonly OpportunityScanInputFormatter _inputFormatter;

    public OpportunityScanOrchestrator(
        ILogger logger,
        IOpportunityScanAgent agent,
        IOpportunityScanRunRepository repository,
        IOpportunityScanParser parser,
        OpportunityScanMarkdownRenderer renderer,
        OpportunityScanInputFormatter inputFormatter)
    {
        _logger = logger;
        _agent = agent;
        _repository = repository;
        _parser = parser;
        _renderer = renderer;
        _inputFormatter = inputFormatter;
    }

    /// <summary>
    /// Formats the substitution chain data as input, executes the agent, parses JSON into structured
    /// rotation targets, renders display markdown, and saves the run to the repository.
    /// </summary>
    public async Task<Result<OpportunityScanRun>> RunAsync(
        ModelConfig model,
        AgentPrompt prompt,
        SubstitutionChainRun substitutionChainRun,
        CancellationToken cancellationToken = default)
    {
        if (substitutionChainRun.Chains.Count == 0)
            return Result.Fail<OpportunityScanRun>("Substitution chain run has no chains to analyze");

        var run = OpportunityScanRun.Start(model, substitutionChainRun.RunId);

        // Format substitution chain data into text input
        var inputText = _inputFormatter.Format(substitutionChainRun);
        if (string.IsNullOrWhiteSpace(inputText))
        {
            _logger.Warn("Input formatter produced empty text — no valid chains to analyze");
            run.Fail(TimeSpan.Zero);
            await _repository.SaveAsync(run, cancellationToken);
            return Result.Fail<OpportunityScanRun>("No valid substitution chain data to analyze");
        }

        _logger.Info("Running Opportunity Scan Agent with model {0} — {1} chains from run {2}",
            model.DisplayName, substitutionChainRun.Chains.Count, substitutionChainRun.RunId);

        var agentResult = await _agent.RunAsync(model, prompt, inputText, cancellationToken);

        if (agentResult.IsFailed)
        {
            _logger.Error("Opportunity Scan Agent failed for model {0}: {1}",
                model.DisplayName, agentResult.Errors.First().Message);
            run.Fail(TimeSpan.Zero);
            await _repository.SaveAsync(run, cancellationToken);
            return Result.Fail<OpportunityScanRun>(agentResult.Errors);
        }

        var result = agentResult.Value;
        run.Complete(
            result.RawOutput,
            result.Duration,
            result.InputTokens,
            result.OutputTokens);

        // Parse JSON into structured domain objects
        var parseResult = _parser.Parse(result.RawOutput);

        foreach (var target in parseResult.Targets)
            run.AddTarget(target);

        // Render display markdown from structured data
        var displayMarkdown = _renderer.Render(parseResult);
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
