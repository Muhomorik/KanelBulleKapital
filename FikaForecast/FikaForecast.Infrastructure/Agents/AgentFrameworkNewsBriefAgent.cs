using System.ClientModel;
using System.Diagnostics;
using Azure.AI.Extensions.OpenAI;
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
/// Implements <see cref="INewsBriefAgent"/> using Microsoft Foundry Agent Service (v2) with Bing Grounding.
/// Creates an ephemeral agent version, calls it via the Responses API, then cleans up.
/// </summary>
public class AgentFrameworkNewsBriefAgent : INewsBriefAgent
{
    private const string NewsBriefJsonSchema = """
        {
            "type": "object",
            "properties": {
                "mood": { "type": "string" },
                "summary": { "type": "string" },
                "categories": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "category": { "type": "string" },
                            "headline": { "type": "string" },
                            "summary": { "type": "string" },
                            "sentiment": { "type": "string" }
                        },
                        "required": ["category", "headline", "summary", "sentiment"],
                        "additionalProperties": false
                    }
                }
            },
            "required": ["mood", "summary", "categories"],
            "additionalProperties": false
        }
        """;

    private readonly ILogger _logger;
    private readonly AIProjectClient _projectClient;
    private readonly string? _bingConnectionName;

    public AgentFrameworkNewsBriefAgent(
        ILogger logger,
        AIProjectClient projectClient,
        string? bingConnectionName = null)
    {
        _logger = logger;
        _projectClient = projectClient;
        _bingConnectionName = bingConnectionName;
    }

    /// <summary>
    /// Creates a Foundry agent version with Bing Grounding, sends the prompt via the Responses API,
    /// and returns the response with token usage. Cleans up the agent version afterward.
    /// </summary>
    public async Task<Result<AgentResult>> RunAsync(
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

        // Configure agent with JSON structured output
        var agentDefinition = new PromptAgentDefinition(model: model.DeploymentName)
        {
            Instructions = systemPrompt,
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "NewsBriefOutput",
                    jsonSchema: BinaryData.FromString(NewsBriefJsonSchema),
                    jsonSchemaFormatDescription: "Structured news brief with mood and per-category assessments",
                    jsonSchemaIsStrict: true)
            }
        };

        if (!string.IsNullOrEmpty(_bingConnectionName))
        {
            try
            {
                var bingConnectionResult = await _projectClient.Connections.GetConnectionAsync(connectionName: _bingConnectionName, cancellationToken: cancellationToken);
                var bingTool = new BingGroundingTool(
                    new BingGroundingSearchToolOptions(
                        searchConfigurations: [new BingGroundingSearchConfiguration(projectConnectionId: bingConnectionResult.Value.Id)]));
                agentDefinition.Tools.Add(bingTool);
                _logger.Info("Bing Grounding tool enabled");
            }
            catch (ClientResultException ex)
            {
                _logger.Warn(ex, "Failed to resolve Bing connection '{0}' — continuing without Bing Grounding", _bingConnectionName);
            }
        }
        else
        {
            _logger.Warn("Bing Grounding not configured — agent will use training data only");
        }

        // Create ephemeral agent version
        AgentVersion? agentVersion = null;
        try
        {
            agentVersion = await _projectClient.Agents.CreateAgentVersionAsync(
                agentName: "fikaforecast-news-brief",
                options: new(agentDefinition),
                cancellationToken: cancellationToken);

            _logger.Info("Created agent {0} v{1}", agentVersion.Name, agentVersion.Version);

            // Call via Responses API
            var responsesClient = _projectClient.OpenAI.GetProjectResponsesClientForAgent(agentVersion.Name);
            var userMessage = $"Generate the market-moving news brief for today, {DateTime.UtcNow:MMMM dd, yyyy}.";

            var clientResult = await responsesClient.CreateResponseAsync(userMessage, cancellationToken: cancellationToken);
            var response = clientResult.Value;

            stopwatch.Stop();

            if (response.Status != ResponseStatus.Completed)
            {
                _logger.Error("Agent response failed — status: {0}", response.Status);
                return Result.Fail<AgentResult>($"Agent response failed with status: {response.Status}");
            }

            var output = response.GetOutputText();
            var inputTokens = (int)(response.Usage?.InputTokenCount ?? 0);
            var outputTokens = (int)(response.Usage?.OutputTokenCount ?? 0);

            _logger.Info(
                "News Brief Agent completed in {0}ms — input: {1}, output: {2} tokens",
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
            _logger.Error(ex, "Agent SDK error — status: {0}", ex.Status);
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
                    _logger.Info("Cleaned up agent {0} v{1}", agentVersion.Name, agentVersion.Version);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to clean up agent {0} v{1}", agentVersion.Name, agentVersion.Version);
                }
            }
        }
    }
}
