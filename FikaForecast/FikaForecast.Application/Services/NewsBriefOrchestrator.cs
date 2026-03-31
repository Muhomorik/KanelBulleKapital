using FikaForecast.Application.DTOs;
using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.ValueObjects;
using FluentResults;
using NLog;

namespace FikaForecast.Application.Services;

/// <summary>
/// Runs the News Brief Agent for a single model, persists the result, and returns the completed run.
/// </summary>
public class NewsBriefOrchestrator
{
    private readonly ILogger _logger;
    private readonly INewsBriefAgent _agent;
    private readonly INewsBriefRunRepository _repository;

    public NewsBriefOrchestrator(ILogger logger, INewsBriefAgent agent, INewsBriefRunRepository repository)
    {
        _logger = logger;
        _agent = agent;
        _repository = repository;
    }

    /// <summary>
    /// Executes the agent, records token usage, and saves the run to the repository.
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
            result.RawMarkdownOutput,
            result.Duration,
            result.InputTokens,
            result.OutputTokens);

        await _repository.SaveAsync(run, cancellationToken);
        return Result.Ok(run);
    }
}
