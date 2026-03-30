using System.Diagnostics;
using Azure.AI.Agents.Persistent;
using FikaForecast.Application.DTOs;
using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.ValueObjects;
using NLog;

namespace FikaForecast.Infrastructure.Agents;

/// <summary>
/// Implements <see cref="INewsBriefAgent"/> using Microsoft Foundry Agent Service with Bing Grounding.
/// Creates a stateful agent with Bing search tool, runs it, collects the response, then cleans up.
/// </summary>
public class AgentFrameworkNewsBriefAgent : INewsBriefAgent
{
    private readonly ILogger _logger;
    private readonly PersistentAgentsClient _agentClient;
    private readonly string? _bingConnectionId;

    public AgentFrameworkNewsBriefAgent(
        ILogger logger,
        PersistentAgentsClient agentClient,
        string? bingConnectionId = null)
    {
        _logger = logger;
        _agentClient = agentClient;
        _bingConnectionId = bingConnectionId;
    }

    /// <summary>
    /// Creates a Foundry agent with Bing Grounding, sends the prompt, waits for completion,
    /// and returns the response with token usage. Cleans up the agent and thread afterward.
    /// </summary>
    public async Task<AgentResult> RunAsync(
        ModelConfig model,
        AgentPrompt prompt,
        CancellationToken cancellationToken = default)
    {
        _logger.Info("Running News Brief Agent with model {0} ({1})", model.DisplayName, model.DeploymentName);

        var stopwatch = Stopwatch.StartNew();

        // Replace {current_date} placeholder in the prompt
        var systemPrompt = prompt.SystemPrompt.Replace(
            "{current_date}",
            DateTime.UtcNow.ToString("yyyy-MM-dd"));

        // Configure tools
        var tools = new List<ToolDefinition>();
        if (!string.IsNullOrEmpty(_bingConnectionId))
        {
            var bingTool = new BingGroundingToolDefinition(
                new BingGroundingSearchToolParameters(
                    [new BingGroundingSearchConfiguration(_bingConnectionId)]));
            tools.Add(bingTool);
            _logger.Info("Bing Grounding tool enabled");
        }
        else
        {
            _logger.Warn("Bing Grounding not configured — agent will use training data only");
        }

        // Create agent
        var agentResponse = await _agentClient.Administration.CreateAgentAsync(
            model: model.DeploymentName,
            name: "fikaforecast-news-brief",
            instructions: systemPrompt,
            tools: tools,
            cancellationToken: cancellationToken);
        var agent = agentResponse.Value;

        _logger.Info("Created agent {0}", agent.Id);

        try
        {
            // Create thread and send message
            var threadResponse = await _agentClient.Threads.CreateThreadAsync(cancellationToken: cancellationToken);
            var thread = threadResponse.Value;
            var userMessage = $"Generate the market-moving news brief for today, {DateTime.UtcNow:MMMM dd, yyyy}.";

            await _agentClient.Messages.CreateMessageAsync(
                thread.Id,
                MessageRole.User,
                userMessage,
                cancellationToken: cancellationToken);

            // Create run and poll until complete
            var runResponse = await _agentClient.Runs.CreateRunAsync(
                thread.Id,
                agent.Id,
                cancellationToken: cancellationToken);
            var run = runResponse.Value;

            while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress)
            {
                await Task.Delay(500, cancellationToken);
                run = (await _agentClient.Runs.GetRunAsync(thread.Id, run.Id, cancellationToken: cancellationToken)).Value;
            }

            stopwatch.Stop();

            if (run.Status != RunStatus.Completed)
            {
                var errorCode = run.LastError?.Code ?? "no_code";
                var errorMsg = run.LastError?.Message ?? "Unknown error";
                _logger.Error("Agent run failed — status: {0}, code: {1}, message: {2}", run.Status, errorCode, errorMsg);
                _logger.Error("  Run ID: {0}, Thread ID: {1}, Agent ID: {2}", run.Id, thread.Id, agent.Id);
                _logger.Error("  Model: {0}, Tools: {1}", model.DeploymentName, string.Join(", ", tools.Select(t => t.GetType().Name)));
                _logger.Error("  Bing connection configured: {0}", !string.IsNullOrEmpty(_bingConnectionId));
                _logger.Error("  Elapsed: {0}ms", stopwatch.ElapsedMilliseconds);

                // Log run steps for debugging
                try
                {
                    var failedSteps = _agentClient.Runs.GetRunSteps(run);
                    var stepCount = 0;
                    foreach (var step in failedSteps)
                    {
                        stepCount++;
                        _logger.Error("  Step {0}: id={1}, status={2}, type={3}",
                            stepCount, step.Id, step.Status, step.Type);
                        if (step.LastError != null)
                            _logger.Error("    Step error: {0} — {1}", step.LastError.Code, step.LastError.Message);
                        if (step.StepDetails is RunStepToolCallDetails toolCallDetails)
                        {
                            foreach (var toolCall in toolCallDetails.ToolCalls)
                                _logger.Error("    Tool call: {0} ({1})", toolCall.GetType().Name, toolCall.Id);
                        }
                    }
                    if (stepCount == 0)
                        _logger.Error("  No run steps found — agent failed before executing any steps");
                }
                catch (Exception ex)
                {
                    _logger.Error("  Failed to retrieve run steps: {0}", ex.Message);
                }

                throw new InvalidOperationException($"Agent run failed: {errorCode} — {errorMsg}");
            }

            // Collect response
            var messages = _agentClient.Messages.GetMessages(thread.Id);
            var output = string.Empty;

            foreach (var msg in messages)
            {
                if (msg.Role == MessageRole.Agent)
                {
                    foreach (var content in msg.ContentItems)
                    {
                        if (content is MessageTextContent textContent)
                        {
                            output = textContent.Text;
                        }
                    }

                    break;
                }
            }

            // Get token usage from run steps
            long totalInputTokens = 0;
            long totalOutputTokens = 0;
            var runSteps = _agentClient.Runs.GetRunSteps(run);
            foreach (var step in runSteps)
            {
                if (step.Usage != null)
                {
                    totalInputTokens += step.Usage.PromptTokens;
                    totalOutputTokens += step.Usage.CompletionTokens;
                }
            }

            _logger.Info(
                "News Brief Agent completed in {0}ms — input: {1}, output: {2} tokens",
                stopwatch.ElapsedMilliseconds,
                totalInputTokens,
                totalOutputTokens);

            // Cleanup thread
            await _agentClient.Threads.DeleteThreadAsync(thread.Id, cancellationToken: cancellationToken);

            return new AgentResult(
                RawMarkdownOutput: output,
                InputTokens: (int)totalInputTokens,
                OutputTokens: (int)totalOutputTokens,
                Duration: stopwatch.Elapsed);
        }
        finally
        {
            // Always cleanup agent
            await _agentClient.Administration.DeleteAgentAsync(agent.Id, cancellationToken: cancellationToken);
            _logger.Info("Cleaned up agent {0}", agent.Id);
        }
    }
}
