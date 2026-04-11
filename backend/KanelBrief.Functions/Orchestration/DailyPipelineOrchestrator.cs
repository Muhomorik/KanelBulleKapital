using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using KanelBrief.Core.Models;
using KanelBrief.Core.Parsers;
using KanelBrief.Core.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Orchestration;

/// <summary>
/// Daily pipeline orchestrator: coordinates the sequence of agent runs.
/// Triggers News Brief daily; triggers aggregations weekly.
/// </summary>
public class DailyPipelineOrchestrator
{
    /// <summary>Cron schedule: every day at 8 UTC (5 fields: minute hour day month day-of-week).</summary>
    public const string DAILY_BRIEF_SCHEDULE = "0 8 * * *";

    /// <summary>Cron schedule: every Monday at 9 UTC.</summary>
    public const string WEEKLY_AGGREGATION_SCHEDULE = "0 9 * * 1";

    private readonly ILogger<DailyPipelineOrchestrator> _logger;
    private readonly IAgentRunRepository _repository;
    private readonly AIProjectClient _aiProjectClient;
    private readonly AgentAdministrationClient _agentAdmin;
    private readonly Uri _foundryEndpoint;
    private readonly DefaultAzureCredential _credential;
    private readonly string? _bingConnectionName;
    private readonly JsonSerializerOptions _jsonOptions;

    public DailyPipelineOrchestrator(
        ILogger<DailyPipelineOrchestrator> logger,
        IAgentRunRepository repository,
        AIProjectClient aiProjectClient,
        AgentAdministrationClient agentAdmin,
        Uri foundryEndpoint,
        DefaultAzureCredential credential,
        string? bingConnectionName = null)
    {
        _logger = logger;
        _repository = repository;
        _aiProjectClient = aiProjectClient;
        _agentAdmin = agentAdmin;
        _foundryEndpoint = foundryEndpoint;
        _credential = credential;
        _bingConnectionName = bingConnectionName;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <summary>Daily timer trigger: executes the News Brief agent every morning at 8 UTC.</summary>
    [Function("DailyNewsBriefTimer")]
    public async Task RunDailyNewsBrief(
        [TimerTrigger(DAILY_BRIEF_SCHEDULE)] TimerInfo timer)
    {
        try
        {
            _logger.LogInformation("Daily News Brief pipeline starting at {Time}", timer.ScheduleStatus?.Last);

            if (timer.IsPastDue)
                _logger.LogWarning("Daily News Brief execution is behind schedule");

            var startTime = DateTimeOffset.UtcNow;
            var run = new NewsBriefRun
            {
                RunDate = startTime.ToString("yyyy-MM-dd"),
                RunId = Guid.NewGuid().ToString(),
                CreatedAt = startTime,
                ModelId = "gpt-5.4-mini",
                DeploymentName = "gpt-5.4-mini",
                Status = RunStatus.Success,
                Mood = MarketSentiment.Mixed.ToString(),
                Summary = string.Empty,
                Assessments = []
            };

            try
            {
                var agentDefinition = new DeclarativeAgentDefinition(model: "gpt-5.4-mini")
                {
                    Instructions = @"You are a financial market analyst. Analyze today's global financial market conditions and produce a morning market brief.

Cover these sectors: Technology, Energy, Financials, Healthcare, Consumer Discretionary, Industrials.
Focus on the most significant market-moving events and trends.

1. Determine the overall market mood (RiskOn, RiskOff, or Mixed)
2. Write a brief 1-2 sentence market summary
3. For each significant sector (at least 3-4), provide a sentiment assessment

Return ONLY a JSON object with this exact structure:
{
  ""mood"": ""RiskOn|RiskOff|Mixed"",
  ""summary"": ""Your market summary"",
  ""assessments"": [
    {
      ""category"": ""Sector name"",
      ""headline"": ""Key development"",
      ""summary"": ""Brief analysis"",
      ""sentiment"": ""RiskOn|RiskOff|Mixed""
    }
  ]
}"
                };

                // Add Bing Grounding for real-time news search
                if (!string.IsNullOrEmpty(_bingConnectionName))
                {
                    try
                    {
                        var bingConnection = await _aiProjectClient.Connections
                            .GetConnectionAsync(connectionName: _bingConnectionName);
                        var bingTool = new BingGroundingTool(
                            new BingGroundingSearchToolOptions(
                                searchConfigurations: [new BingGroundingSearchConfiguration(
                                    projectConnectionId: bingConnection.Value.Id)]));
                        agentDefinition.Tools.Add(bingTool);
                        _logger.LogInformation("Bing Grounding tool enabled");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to resolve Bing connection '{Name}' — continuing without Bing Grounding",
                            _bingConnectionName);
                    }
                }
                else
                {
                    _logger.LogWarning("Bing Grounding not configured — agent will use training data only");
                }

                // Create ephemeral agent version
                var agentVersionResult = await _agentAdmin.CreateAgentVersionAsync(
                    agentName: "kanelbrief-news-brief",
                    options: new(agentDefinition));
                var agentVersionValue = agentVersionResult.Value;

                try
                {
                    var responsesClient = new ProjectResponsesClient(
                        _foundryEndpoint,
                        _credential,
                        new AgentReference(agentVersionValue.Name, agentVersionValue.Version));

                    var userMessage = $"Produce today's morning market brief for {startTime:yyyy-MM-dd}. Analyze the most significant global financial market developments and sector-level sentiment.";

                    var clientResult = await responsesClient.CreateResponseAsync(userMessage);
                    var responseText = clientResult.Value.GetOutputText();

                    var analysisJson = AgentResponseParser.ExtractJson(responseText);
                    var analysis = JsonSerializer.Deserialize<NewsBriefAnalysisResult>(analysisJson, _jsonOptions)
                        ?? throw new InvalidOperationException("Failed to parse agent response");

                    run.Mood = analysis.Mood;
                    run.Summary = analysis.Summary;
                    run.Assessments = analysis.Assessments;

                    _logger.LogInformation("Daily News Brief completed: Mood={Mood}, Assessments={Count}",
                        run.Mood, run.Assessments.Count);
                }
                finally
                {
                    // Clean up ephemeral agent version
                    await _agentAdmin.DeleteAgentVersionAsync(
                        agentName: agentVersionValue.Name, agentVersion: agentVersionValue.Version);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Daily News Brief agent failed, using fallback");
                run.Status = RunStatus.Partial;
                run.Summary = $"Market briefing for {startTime:yyyy-MM-dd} — analysis unavailable.";
            }

            run.DurationSeconds = (DateTimeOffset.UtcNow - startTime).TotalSeconds;
            await _repository.SaveNewsBriefRunAsync(run);
            _logger.LogInformation("Daily News Brief run saved: {RunId}", run.RunId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Daily News Brief pipeline failed");
            throw;
        }
    }

    /// <summary>Weekly timer trigger: aggregates the week's briefs into themes (runs Monday morning).</summary>
    [Function("WeeklyAggregationTimer")]
    public async Task RunWeeklyAggregation(
        [TimerTrigger(WEEKLY_AGGREGATION_SCHEDULE)] TimerInfo timer)
    {
        try
        {
            _logger.LogInformation("Weekly aggregation pipeline starting at {Time}", timer.ScheduleStatus?.Last);

            if (timer.IsPastDue)
                _logger.LogWarning("Weekly aggregation execution is behind schedule");

            // Calculate previous week boundaries (Monday to Sunday)
            var (prevMonday, prevSunday) = CalculateWeekBoundaries(DateTimeOffset.UtcNow);

            _logger.LogInformation("Processing week from {WeekStart} to {WeekEnd}",
                prevMonday.ToString("yyyy-MM-dd"), prevSunday.ToString("yyyy-MM-dd"));

            // Fetch all news briefs from the previous week
            var dailyBriefs = new List<NewsBriefRun>();
            for (var date = prevMonday; date < prevSunday; date = date.AddDays(1))
            {
                var briefsForDate = await _repository.GetNewsBriefRunsByDateAsync(date.ToString("yyyy-MM-dd"));
                dailyBriefs.AddRange(briefsForDate);
            }

            _logger.LogInformation("Retrieved {BriefCount} news briefs from the week", dailyBriefs.Count);

            if (dailyBriefs.Count == 0)
            {
                _logger.LogWarning("No daily briefs found for the week, skipping aggregation");
                return;
            }

            // Step 1: Weekly Summary
            var weeklySummaryRun = await RunWeeklySummaryAsync(prevMonday, prevSunday, dailyBriefs);

            // Step 2: Substitution Chain (depends on weekly summary)
            var substitutionChainRun = await RunSubstitutionChainAsync(weeklySummaryRun);

            // Step 3: Opportunity Scan (depends on substitution chain)
            await RunOpportunityScanAsync(substitutionChainRun);

            _logger.LogInformation("Weekly aggregation pipeline completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Weekly aggregation pipeline failed");
            throw;
        }
    }

    private async Task<WeeklySummaryRun> RunWeeklySummaryAsync(
        DateTime weekStart, DateTime weekEnd, List<NewsBriefRun> dailyBriefs)
    {
        var startTime = DateTimeOffset.UtcNow;
        var run = new WeeklySummaryRun
        {
            RunDate = startTime.ToString("yyyy-MM-dd"),
            RunId = Guid.NewGuid().ToString(),
            CreatedAt = startTime,
            ModelId = "gpt-5.4-mini",
            Status = RunStatus.Success,
            WeekStart = new DateTimeOffset(weekStart, TimeSpan.Zero),
            WeekEnd = new DateTimeOffset(weekEnd, TimeSpan.Zero),
            NetMood = MarketSentiment.Mixed,
            MoodSummary = string.Empty,
            Themes = []
        };

        try
        {
            var briefsContext = string.Join("\n\n", dailyBriefs.Select(b =>
                $"[{b.RunDate}] Mood: {b.Mood}\nSummary: {b.Summary}\nAssessments: {JsonSerializer.Serialize(b.Assessments, _jsonOptions)}"));

            var agent = _aiProjectClient.AsAIAgent(
                model: "gpt-5.4-mini",
                name: "WeeklySummaryAnalyzer",
                instructions: @"You are a financial market analyst. Analyze a week of daily market briefs and:
1. Determine the net market mood for the week (RiskOn, RiskOff, or Mixed)
2. Write a 1-2 sentence weekly summary
3. Identify 2-3 key themes that emerged

Return ONLY a JSON object with this exact structure:
{
  ""mood"": ""RiskOn|RiskOff|Mixed"",
  ""summary"": ""Weekly assessment"",
  ""themes"": [
    {
      ""category"": ""Theme name"",
      ""summary"": ""Description"",
      ""confidence"": ""High|Medium|Low"",
      ""sentiment"": ""RiskOn|RiskOff|Mixed""
    }
  ]
}");

            var prompt = $"Analyze this week's market briefs ({weekStart:yyyy-MM-dd} to {weekEnd:yyyy-MM-dd}):\n\n{briefsContext}";
            var response = await agent.RunAsync(prompt);
            var json = AgentResponseParser.ExtractJson(response.ToString() ?? string.Empty);
            var analysis = JsonSerializer.Deserialize<WeeklySummaryAnalysisResult>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to parse weekly summary");

            run.NetMood = AgentResponseParser.ParseSentiment(analysis.Mood);
            run.MoodSummary = analysis.Summary;
            run.Themes = analysis.Themes;

            _logger.LogInformation("Weekly Summary completed: Mood={Mood}, Themes={Count}",
                run.NetMood, run.Themes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Weekly Summary agent failed, using fallback");
            run.Status = RunStatus.Partial;
            run.MoodSummary = $"Weekly summary for {weekStart:yyyy-MM-dd} to {weekEnd:yyyy-MM-dd} — analysis unavailable.";
        }

        run.DurationSeconds = (DateTimeOffset.UtcNow - startTime).TotalSeconds;
        await _repository.SaveWeeklySummaryRunAsync(run);
        _logger.LogInformation("Weekly Summary saved: {RunId}", run.RunId);
        return run;
    }

    private async Task<SubstitutionChainRun> RunSubstitutionChainAsync(WeeklySummaryRun weeklySummary)
    {
        var startTime = DateTimeOffset.UtcNow;
        var run = new SubstitutionChainRun
        {
            RunDate = startTime.ToString("yyyy-MM-dd"),
            RunId = Guid.NewGuid().ToString(),
            CreatedAt = startTime,
            ModelId = "gpt-5.4-mini",
            Status = RunStatus.Success,
            WeeklySummaryRunId = weeklySummary.RunId,
            Chains = []
        };

        try
        {
            var summaryContext = $"Net Mood: {weeklySummary.NetMood}\nSummary: {weeklySummary.MoodSummary}\nThemes:\n{JsonSerializer.Serialize(weeklySummary.Themes, _jsonOptions)}";

            var agent = _aiProjectClient.AsAIAgent(
                model: "gpt-5.4-mini",
                name: "SubstitutionChainAnalyzer",
                instructions: @"You are a capital rotation analyst. Based on weekly market themes, identify where capital is flowing from and to.
Identify 2-4 rotation chains based on the sentiment data.

Return ONLY a JSON object with this exact structure:
{
  ""chains"": [
    {
      ""capitalFleeing"": ""Sector losing capital"",
      ""flowsToward"": ""Sector gaining capital"",
      ""mechanism"": ""Why capital is rotating""
    }
  ]
}");

            var prompt = $"Based on this weekly summary, identify capital rotation chains:\n\n{summaryContext}";
            var response = await agent.RunAsync(prompt);
            var json = AgentResponseParser.ExtractJson(response.ToString() ?? string.Empty);
            var analysis = JsonSerializer.Deserialize<SubstitutionChainAnalysisResult>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to parse substitution chains");

            run.Chains = analysis.Chains;

            _logger.LogInformation("Substitution Chain completed: {ChainCount} chains", run.Chains.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Substitution Chain agent failed, using fallback");
            run.Status = RunStatus.Partial;
        }

        run.DurationSeconds = (DateTimeOffset.UtcNow - startTime).TotalSeconds;
        await _repository.SaveSubstitutionChainRunAsync(run);
        _logger.LogInformation("Substitution Chain saved: {RunId}", run.RunId);
        return run;
    }

    private async Task RunOpportunityScanAsync(SubstitutionChainRun substitutionChain)
    {
        var startTime = DateTimeOffset.UtcNow;
        var run = new OpportunityScanRun
        {
            RunDate = startTime.ToString("yyyy-MM-dd"),
            RunId = Guid.NewGuid().ToString(),
            CreatedAt = startTime,
            ModelId = "gpt-5.4-mini",
            Status = RunStatus.Success,
            SubstitutionChainRunId = substitutionChain.RunId,
            Targets = []
        };

        try
        {
            var chainsContext = JsonSerializer.Serialize(substitutionChain.Chains, _jsonOptions);

            var agent = _aiProjectClient.AsAIAgent(
                model: "gpt-5.4-mini",
                name: "OpportunityScanAnalyzer",
                instructions: @"You are an investment analyst. Evaluate capital rotation opportunities and identify actionable targets.
Identify 2-4 opportunities with varying signal strengths.

Return ONLY a JSON object with this exact structure:
{
  ""targets"": [
    {
      ""category"": ""Asset or sector"",
      ""signalStrength"": ""Strong|Moderate|Weak"",
      ""rationale"": ""Why this is an opportunity"",
      ""riskCaveat"": ""Key risks to watch""
    }
  ]
}");

            var prompt = $"Based on these capital rotation chains, identify investment opportunities:\n\n{chainsContext}";
            var response = await agent.RunAsync(prompt);
            var json = AgentResponseParser.ExtractJson(response.ToString() ?? string.Empty);
            var analysis = JsonSerializer.Deserialize<OpportunityScanAnalysisResult>(json, _jsonOptions)
                ?? throw new InvalidOperationException("Failed to parse opportunities");

            run.Targets = analysis.Targets;

            _logger.LogInformation("Opportunity Scan completed: {TargetCount} targets", run.Targets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Opportunity Scan agent failed, using fallback");
            run.Status = RunStatus.Partial;
        }

        run.DurationSeconds = (DateTimeOffset.UtcNow - startTime).TotalSeconds;
        await _repository.SaveOpportunityScanRunAsync(run);
        _logger.LogInformation("Opportunity Scan saved: {RunId}", run.RunId);
    }

    /// <summary>
    /// Calculates the previous week's Monday-to-Sunday boundaries relative to the given timestamp.
    /// </summary>
    internal static (DateTime prevMonday, DateTime prevSunday) CalculateWeekBoundaries(DateTimeOffset now)
    {
        var daysFromMonday = ((int)now.DayOfWeek + 6) % 7; // Monday = 0
        var thisMonday = now.AddDays(-daysFromMonday).Date;
        var prevMonday = thisMonday.AddDays(-7);
        var prevSunday = thisMonday;
        return (prevMonday, prevSunday);
    }
}
