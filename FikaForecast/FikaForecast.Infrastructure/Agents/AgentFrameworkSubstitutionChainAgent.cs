using System.ClientModel;
using System.Diagnostics;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using FikaForecast.Application.DTOs;
using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.ValueObjects;
using FluentResults;
using NLog;
using OpenAI.Responses;

#pragma warning disable OPENAI001 // ResponseStatus is marked experimental

namespace FikaForecast.Infrastructure.Agents;

/// <summary>
/// Implements <see cref="ISubstitutionChainAgent"/> using Foundry Agent Service (v2).
/// Creates an ephemeral agent with the substitution chain prompt, sends the formatted
/// weekly summary, then cleans up. No Bing Grounding — works on pre-built input text only.
/// </summary>
public class AgentFrameworkSubstitutionChainAgent : ISubstitutionChainAgent
{
    private readonly ILogger _logger;
    private readonly AIProjectClient _projectClient;

    public AgentFrameworkSubstitutionChainAgent(ILogger logger, AIProjectClient projectClient)
    {
        _logger = logger;
        _projectClient = projectClient;
    }

    /// <inheritdoc />
    public async Task<Result<AgentResult>> RunAsync(
        ModelConfig model,
        AgentPrompt prompt,
        string inputText,
        CancellationToken cancellationToken = default)
    {
        _logger.Info("Running Substitution Chain Agent with model {0} ({1})",
            model.DisplayName, model.DeploymentName);

        var stopwatch = Stopwatch.StartNew();

        var agentDefinition = new PromptAgentDefinition(model: model.DeploymentName)
        {
            Instructions = prompt.SystemPrompt
        };

        AgentVersion? agentVersion = null;
        try
        {
            agentVersion = await _projectClient.Agents.CreateAgentVersionAsync(
                agentName: "fikaforecast-substitution-chain",
                options: new(agentDefinition),
                cancellationToken: cancellationToken);

            _logger.Info("Created substitution chain agent {0} v{1}",
                agentVersion.Name, agentVersion.Version);

            var responsesClient = _projectClient.OpenAI
                .GetProjectResponsesClientForAgent(agentVersion.Name);

            var clientResult = await responsesClient.CreateResponseAsync(
                inputText, cancellationToken: cancellationToken);
            var response = clientResult.Value;

            stopwatch.Stop();

            if (response.Status != ResponseStatus.Completed)
            {
                _logger.Error("Substitution Chain Agent response failed — status: {0}", response.Status);
                return Result.Fail<AgentResult>($"Substitution Chain failed with status: {response.Status}");
            }

            var output = response.GetOutputText();
            var inputTokens = (int)(response.Usage?.InputTokenCount ?? 0);
            var outputTokens = (int)(response.Usage?.OutputTokenCount ?? 0);

            _logger.Info(
                "Substitution Chain Agent completed in {0}ms — input: {1}, output: {2} tokens",
                stopwatch.ElapsedMilliseconds,
                inputTokens,
                outputTokens);

            return Result.Ok(new AgentResult(
                RawOutput: output,
                InputTokens: inputTokens,
                OutputTokens: outputTokens,
                Duration: stopwatch.Elapsed));
        }
        catch (ClientResultException ex)
        {
            stopwatch.Stop();
            _logger.Error(ex, "Substitution Chain Agent SDK error — status: {0}", ex.Status);
            return Result.Fail<AgentResult>($"Azure AI Foundry error ({ex.Status}): {ex.Message}");
        }
        finally
        {
            if (agentVersion != null)
            {
                try
                {
                    await _projectClient.Agents.DeleteAgentVersionAsync(
                        agentVersion.Name, agentVersion.Version, cancellationToken);
                    _logger.Info("Cleaned up substitution chain agent {0} v{1}",
                        agentVersion.Name, agentVersion.Version);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to clean up substitution chain agent {0} v{1}",
                        agentVersion.Name, agentVersion.Version);
                }
            }
        }
    }
}
