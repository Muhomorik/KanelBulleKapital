using FikaForecast.Application.DTOs;
using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.ValueObjects;
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
    /// </summary>
    /// <param name="model">Model deployment to use.</param>
    /// <param name="prompt">System prompt for the agent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted <see cref="NewsBriefRun"/> with status and output.</returns>
    public async Task<NewsBriefRun> RunBriefAsync(
        ModelConfig model,
        AgentPrompt prompt,
        CancellationToken cancellationToken = default)
    {
        var run = NewsBriefRun.Start(model, prompt);

        try
        {
            var result = await _agent.RunAsync(model, prompt, cancellationToken);

            run.Complete(
                result.RawMarkdownOutput,
                result.Duration,
                result.InputTokens,
                result.OutputTokens);

            // TODO: Parse markdown into NewsItems and MarketMood
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Agent execution failed for model {0}", model.DisplayName);
            run.Fail(TimeSpan.Zero);
        }

        await _repository.SaveAsync(run, cancellationToken);
        return run;
    }
}
