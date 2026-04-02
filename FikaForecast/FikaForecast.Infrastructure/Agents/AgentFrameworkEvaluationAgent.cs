using System.ClientModel;
using System.Diagnostics;
using System.Text;
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
/// Implements <see cref="IEvaluationAgent"/> using Microsoft Foundry Agent Service (v2).
/// Creates an ephemeral agent version with the evaluation prompt, sends the report for assessment,
/// then cleans up. No Bing Grounding — evaluation works on existing content only.
/// </summary>
public class AgentFrameworkEvaluationAgent : IEvaluationAgent
{
    private readonly ILogger _logger;
    private readonly AIProjectClient _projectClient;

    public AgentFrameworkEvaluationAgent(ILogger logger, AIProjectClient projectClient)
    {
        _logger = logger;
        _projectClient = projectClient;
    }

    /// <inheritdoc />
    public async Task<Result<AgentResult>> EvaluateAsync(
        ModelConfig model,
        AgentPrompt evaluationPrompt,
        string reportMarkdown,
        AgentPrompt originalPrompt,
        CancellationToken cancellationToken = default)
    {
        _logger.Info("Running Evaluation Agent with model {0} ({1})", model.DisplayName, model.DeploymentName);

        var stopwatch = Stopwatch.StartNew();

        var agentDefinition = new PromptAgentDefinition(model: model.DeploymentName)
        {
            Instructions = evaluationPrompt.SystemPrompt
        };

        var userMessage = $"""
            ## ORIGINAL PROMPT (rules the report must follow)

            {originalPrompt.SystemPrompt}

            ---

            ## REPORT OUTPUT (to evaluate)

            {reportMarkdown}
            """;

        AgentVersion? agentVersion = null;
        try
        {
            agentVersion = await _projectClient.Agents.CreateAgentVersionAsync(
                agentName: "fikaforecast-evaluation",
                options: new(agentDefinition),
                cancellationToken: cancellationToken);

            _logger.Info("Created evaluation agent {0} v{1}", agentVersion.Name, agentVersion.Version);

            var responsesClient = _projectClient.OpenAI.GetProjectResponsesClientForAgent(agentVersion.Name);

            var clientResult = await responsesClient.CreateResponseAsync(userMessage, cancellationToken: cancellationToken);
            var response = clientResult.Value;

            stopwatch.Stop();

            if (response.Status != ResponseStatus.Completed)
            {
                _logger.Error("Evaluation agent response failed — status: {0}", response.Status);
                return Result.Fail<AgentResult>($"Evaluation failed with status: {response.Status}");
            }

            var output = response.GetOutputText();
            var inputTokens = (int)(response.Usage?.InputTokenCount ?? 0);
            var outputTokens = (int)(response.Usage?.OutputTokenCount ?? 0);

            _logger.Info(
                "Evaluation Agent completed in {0}ms — input: {1}, output: {2} tokens",
                stopwatch.ElapsedMilliseconds,
                inputTokens,
                outputTokens);

            return Result.Ok(new AgentResult(
                RawMarkdownOutput: output,
                InputTokens: inputTokens,
                OutputTokens: outputTokens,
                Duration: stopwatch.Elapsed));
        }
        catch (ClientResultException ex)
        {
            stopwatch.Stop();
            _logger.Error(ex, "Evaluation agent SDK error — status: {0}", ex.Status);
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
                    _logger.Info("Cleaned up evaluation agent {0} v{1}", agentVersion.Name, agentVersion.Version);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to clean up evaluation agent {0} v{1}", agentVersion.Name, agentVersion.Version);
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<Result<AgentResult>> CompareAsync(
        ModelConfig model,
        AgentPrompt comparisonPrompt,
        IReadOnlyList<(string ModelName, string ReportMarkdown)> reports,
        AgentPrompt originalPrompt,
        CancellationToken cancellationToken = default)
    {
        _logger.Info(
            "Running Comparison Agent with model {0} ({1}) — comparing {2} reports",
            model.DisplayName, model.DeploymentName, reports.Count);

        var stopwatch = Stopwatch.StartNew();

        var agentDefinition = new PromptAgentDefinition(model: model.DeploymentName)
        {
            Instructions = comparisonPrompt.SystemPrompt
        };

        var sb = new StringBuilder();
        sb.AppendLine("## ORIGINAL PROMPT (rules all reports must follow)");
        sb.AppendLine();
        sb.AppendLine(originalPrompt.SystemPrompt);
        sb.AppendLine();
        sb.AppendLine("---");

        for (var i = 0; i < reports.Count; i++)
        {
            var (modelName, reportMarkdown) = reports[i];
            sb.AppendLine();
            sb.AppendLine($"## REPORT {i + 1}: {modelName}");
            sb.AppendLine();
            sb.AppendLine(reportMarkdown);
            sb.AppendLine();
            sb.AppendLine("---");
        }

        var userMessage = sb.ToString();

        AgentVersion? agentVersion = null;
        try
        {
            agentVersion = await _projectClient.Agents.CreateAgentVersionAsync(
                agentName: "fikaforecast-evaluation",
                options: new(agentDefinition),
                cancellationToken: cancellationToken);

            _logger.Info("Created comparison agent {0} v{1}", agentVersion.Name, agentVersion.Version);

            var responsesClient = _projectClient.OpenAI.GetProjectResponsesClientForAgent(agentVersion.Name);

            var clientResult = await responsesClient.CreateResponseAsync(userMessage, cancellationToken: cancellationToken);
            var response = clientResult.Value;

            stopwatch.Stop();

            if (response.Status != ResponseStatus.Completed)
            {
                _logger.Error("Comparison agent response failed — status: {0}", response.Status);
                return Result.Fail<AgentResult>($"Comparison failed with status: {response.Status}");
            }

            var output = response.GetOutputText();
            var inputTokens = (int)(response.Usage?.InputTokenCount ?? 0);
            var outputTokens = (int)(response.Usage?.OutputTokenCount ?? 0);

            _logger.Info(
                "Comparison Agent completed in {0}ms — input: {1}, output: {2} tokens",
                stopwatch.ElapsedMilliseconds,
                inputTokens,
                outputTokens);

            return Result.Ok(new AgentResult(
                RawMarkdownOutput: output,
                InputTokens: inputTokens,
                OutputTokens: outputTokens,
                Duration: stopwatch.Elapsed));
        }
        catch (ClientResultException ex)
        {
            stopwatch.Stop();
            _logger.Error(ex, "Comparison agent SDK error — status: {0}", ex.Status);
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
                    _logger.Info("Cleaned up comparison agent {0} v{1}", agentVersion.Name, agentVersion.Version);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to clean up comparison agent {0} v{1}", agentVersion.Name, agentVersion.Version);
                }
            }
        }
    }
}
