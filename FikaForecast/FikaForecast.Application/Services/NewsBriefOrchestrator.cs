using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.ValueObjects;
using FluentResults;
using NLog;

namespace FikaForecast.Application.Services;

/// <summary>
/// Runs the News Brief Agent for a single model, parses the JSON output into structured
/// domain objects, renders display markdown, and persists everything to the repository.
/// </summary>
public class NewsBriefOrchestrator
{
    private readonly ILogger _logger;
    private readonly INewsBriefAgent _agent;
    private readonly INewsBriefRunRepository _repository;
    private readonly INewsBriefParser _parser;
    private readonly NewsBriefMarkdownRenderer _renderer;

    public NewsBriefOrchestrator(
        ILogger logger,
        INewsBriefAgent agent,
        INewsBriefRunRepository repository,
        INewsBriefParser parser,
        NewsBriefMarkdownRenderer renderer)
    {
        _logger = logger;
        _agent = agent;
        _repository = repository;
        _parser = parser;
        _renderer = renderer;
    }

    /// <summary>
    /// Executes the agent, parses JSON into structured data, renders display markdown,
    /// and saves the run to the repository.
    /// Returns a failed Result with error details if the agent fails.
    /// </summary>
    public async Task<Result<NewsBriefRun>> RunBriefAsync(
        ModelConfig model,
        AgentPrompt prompt,
        CancellationToken cancellationToken = default)
    {
        var run = NewsBriefRun.Start(model, prompt);

        var agentResult = await _agent.RunAsync(model, prompt, cancellationToken);

        if (agentResult.IsFailed)
        {
            _logger.Error("Agent execution failed for model {0}: {1}",
                model.DisplayName, agentResult.Errors.First().Message);
            run.Fail(TimeSpan.Zero);
            await _repository.SaveAsync(run, cancellationToken);
            return Result.Fail<NewsBriefRun>(agentResult.Errors);
        }

        var result = agentResult.Value;
        run.Complete(
            result.RawOutput,
            result.Duration,
            result.InputTokens,
            result.OutputTokens);

        // Parse JSON into structured domain objects
        var parseResult = _parser.Parse(result.RawOutput);

        if (parseResult.Item is not null)
        {
            run.SetItem(parseResult.Item);

            // Render display markdown from structured data
            var displayMarkdown = _renderer.Render(parseResult);
            if (!string.IsNullOrEmpty(displayMarkdown))
                run.SetDisplayMarkdown(displayMarkdown);
        }

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
