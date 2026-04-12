using System.Text.Json;
using Azure.Data.Tables;
using KanelBrief.Core.Models;
using KanelBrief.Functions.Repositories;
using static KanelBrief.Functions.Repositories.AgentRunRepository;

// RunStatus.Partial is [Obsolete] but still read from historical rows — these parser
// tests intentionally exercise that path. Suppress the deprecation warning for the file.
#pragma warning disable CS0618

namespace KanelBrief.Functions.Tests.Repositories;

[TestFixture]
[TestOf(typeof(AgentRunRepository))]
public class AgentRunRepositoryMapperTests
{
    private AgentRunRepository _sut = null!;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    [SetUp]
    public void SetUp()
    {
        // Mappers don't use table clients, safe to pass null
        _sut = new AgentRunRepository(null!, null!, null!, null!);
    }

    [Test]
    [Category("NewsBriefRun")]
    public void MapToNewsBriefRun_ValidEntity_MapsAllBaseFields()
    {
        var entity = CreateBaseEntity("2026-04-08", "run-123");
        entity[NewsBriefColumns.DeploymentName] = "gpt-5.4-mini";
        entity[NewsBriefColumns.Mood] = "RiskOn";
        entity[NewsBriefColumns.Summary] = "Bull market continues";
        entity[NewsBriefColumns.Assessments] = "[]";

        var run = _sut.MapToNewsBriefRun(entity);

        Assert.That(run.RunDate, Is.EqualTo("2026-04-08"));
        Assert.That(run.RunId, Is.EqualTo("run-123"));
        Assert.That(run.ModelId, Is.EqualTo("gpt-5.4-mini"));
        Assert.That(run.Status, Is.EqualTo(RunStatus.Success));
        Assert.That(run.DurationSeconds, Is.EqualTo(11.5));
        Assert.That(run.InputTokens, Is.EqualTo(100));
        Assert.That(run.OutputTokens, Is.EqualTo(200));
        Assert.That(run.TotalTokens, Is.EqualTo(300));
    }

    [Test]
    [Category("NewsBriefRun")]
    public void MapToNewsBriefRun_ValidEntity_MapsSpecificFields()
    {
        var entity = CreateBaseEntity("2026-04-08", "run-123");
        entity[NewsBriefColumns.DeploymentName] = "gpt-5.4-mini";
        entity[NewsBriefColumns.Mood] = "RiskOff";
        entity[NewsBriefColumns.Summary] = "Markets retreat";
        entity[NewsBriefColumns.Assessments] = "[]";

        var run = _sut.MapToNewsBriefRun(entity);

        Assert.That(run.DeploymentName, Is.EqualTo("gpt-5.4-mini"));
        Assert.That(run.Mood, Is.EqualTo("RiskOff"));
        Assert.That(run.Summary, Is.EqualTo("Markets retreat"));
    }

    [Test]
    [Category("NewsBriefRun")]
    public void MapToNewsBriefRun_AssessmentsJson_DeserializesCorrectly()
    {
        var assessments = new List<CategoryAssessment>
        {
            new() { Category = "Tech", Headline = "AI boom", Summary = "Cloud up", Sentiment = MarketSentiment.RiskOn }
        };

        var entity = CreateBaseEntity("2026-04-08", "run-123");
        entity[NewsBriefColumns.DeploymentName] = "gpt-5.4-mini";
        entity[NewsBriefColumns.Mood] = "Mixed";
        entity[NewsBriefColumns.Summary] = "test";
        entity[NewsBriefColumns.Assessments] = JsonSerializer.Serialize(assessments, _jsonOptions);

        var run = _sut.MapToNewsBriefRun(entity);

        Assert.That(run.Assessments, Has.Count.EqualTo(1));
        Assert.That(run.Assessments[0].Category, Is.EqualTo("Tech"));
        Assert.That(run.Assessments[0].Sentiment, Is.EqualTo(MarketSentiment.RiskOn));
    }

    [Test]
    [Category("NewsBriefRun")]
    public void MapToNewsBriefRun_EmptyAssessmentsJson_ReturnsEmptyList()
    {
        var entity = CreateBaseEntity("2026-04-08", "run-123");
        entity[NewsBriefColumns.DeploymentName] = "gpt-5.4-mini";
        entity[NewsBriefColumns.Mood] = "Mixed";
        entity[NewsBriefColumns.Summary] = "test";
        entity[NewsBriefColumns.Assessments] = "";

        var run = _sut.MapToNewsBriefRun(entity);

        Assert.That(run.Assessments, Is.Not.Null);
        Assert.That(run.Assessments, Is.Empty);
    }

    [TestCase("Success", RunStatus.Success)]
    [TestCase("Failed", RunStatus.Failed)]
    [TestCase("Partial", RunStatus.Partial)]
    [Category("NewsBriefRun")]
    public void MapToNewsBriefRun_StatusParsing_AllValues_ParseCorrectly(string statusString, RunStatus expected)
    {
        var entity = CreateBaseEntity("2026-04-08", "run-123");
        entity[BaseColumns.Status] = statusString;
        entity[NewsBriefColumns.DeploymentName] = "";
        entity[NewsBriefColumns.Mood] = "";
        entity[NewsBriefColumns.Summary] = "";
        entity[NewsBriefColumns.Assessments] = "[]";

        var run = _sut.MapToNewsBriefRun(entity);

        Assert.That(run.Status, Is.EqualTo(expected));
    }

    [Test]
    [Category("WeeklySummaryRun")]
    public void MapToWeeklySummaryRun_ValidEntity_MapsWeekDatesAndMood()
    {
        var weekStart = new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.Zero);
        var weekEnd = new DateTimeOffset(2026, 4, 6, 0, 0, 0, TimeSpan.Zero);

        var entity = CreateBaseEntity("2026-04-07", "weekly-001");
        entity[WeeklySummaryColumns.WeekStart] = weekStart;
        entity[WeeklySummaryColumns.WeekEnd] = weekEnd;
        entity[WeeklySummaryColumns.NetMood] = "RiskOff";
        entity[WeeklySummaryColumns.MoodSummary] = "Cautious week";
        entity[WeeklySummaryColumns.Themes] = "[]";

        var run = _sut.MapToWeeklySummaryRun(entity);

        Assert.That(run.WeekStart, Is.EqualTo(weekStart));
        Assert.That(run.WeekEnd, Is.EqualTo(weekEnd));
        Assert.That(run.NetMood, Is.EqualTo(MarketSentiment.RiskOff));
        Assert.That(run.MoodSummary, Is.EqualTo("Cautious week"));
    }

    [Test]
    [Category("WeeklySummaryRun")]
    public void MapToWeeklySummaryRun_ThemesJson_DeserializesWithNestedEnums()
    {
        var themes = new List<WeeklySummaryTheme>
        {
            new() { Category = "Trade War", Summary = "Tariffs up", Confidence = ConfidenceLevel.High, Sentiment = MarketSentiment.RiskOff }
        };

        var entity = CreateBaseEntity("2026-04-07", "weekly-001");
        entity[WeeklySummaryColumns.WeekStart] = DateTimeOffset.MinValue;
        entity[WeeklySummaryColumns.WeekEnd] = DateTimeOffset.MinValue;
        entity[WeeklySummaryColumns.NetMood] = "Mixed";
        entity[WeeklySummaryColumns.MoodSummary] = "";
        entity[WeeklySummaryColumns.Themes] = JsonSerializer.Serialize(themes, _jsonOptions);

        var run = _sut.MapToWeeklySummaryRun(entity);

        Assert.That(run.Themes, Has.Count.EqualTo(1));
        Assert.That(run.Themes[0].Confidence, Is.EqualTo(ConfidenceLevel.High));
        Assert.That(run.Themes[0].Sentiment, Is.EqualTo(MarketSentiment.RiskOff));
    }

    [Test]
    [Category("WeeklySummaryRun")]
    public void MapToWeeklySummaryRun_EmptyThemesJson_ReturnsEmptyList()
    {
        var entity = CreateBaseEntity("2026-04-07", "weekly-001");
        entity[WeeklySummaryColumns.WeekStart] = DateTimeOffset.MinValue;
        entity[WeeklySummaryColumns.WeekEnd] = DateTimeOffset.MinValue;
        entity[WeeklySummaryColumns.NetMood] = "Mixed";
        entity[WeeklySummaryColumns.MoodSummary] = "";
        entity[WeeklySummaryColumns.Themes] = "";

        var run = _sut.MapToWeeklySummaryRun(entity);

        Assert.That(run.Themes, Is.Not.Null);
        Assert.That(run.Themes, Is.Empty);
    }

    [Test]
    [Category("SubstitutionChainRun")]
    public void MapToSubstitutionChainRun_ValidEntity_MapsChainsFromJson()
    {
        var chains = new List<RotationChain>
        {
            new() { CapitalFleeing = "Energy", FlowsToward = "Tech", Mechanism = "ESG" }
        };

        var entity = CreateBaseEntity("2026-04-07", "chain-001");
        entity[SubstitutionChainColumns.WeeklySummaryRunId] = "weekly-001";
        entity[SubstitutionChainColumns.Chains] = JsonSerializer.Serialize(chains, _jsonOptions);

        var run = _sut.MapToSubstitutionChainRun(entity);

        Assert.That(run.Chains, Has.Count.EqualTo(1));
        Assert.That(run.Chains[0].CapitalFleeing, Is.EqualTo("Energy"));
        Assert.That(run.Chains[0].FlowsToward, Is.EqualTo("Tech"));
    }

    [Test]
    [Category("SubstitutionChainRun")]
    public void MapToSubstitutionChainRun_PreservesWeeklySummaryRunId()
    {
        var entity = CreateBaseEntity("2026-04-07", "chain-001");
        entity[SubstitutionChainColumns.WeeklySummaryRunId] = "weekly-ref-123";
        entity[SubstitutionChainColumns.Chains] = "[]";

        var run = _sut.MapToSubstitutionChainRun(entity);

        Assert.That(run.WeeklySummaryRunId, Is.EqualTo("weekly-ref-123"));
    }

    [Test]
    [Category("OpportunityScanRun")]
    public void MapToOpportunityScanRun_ValidEntity_MapsTargetsFromJson()
    {
        var targets = new List<RotationTarget>
        {
            new() { Category = "Cloud", SignalStrength = SignalStrength.Strong, Rationale = "Growth", RiskCaveat = "Valuation" }
        };

        var entity = CreateBaseEntity("2026-04-07", "opp-001");
        entity[OpportunityScanColumns.SubstitutionChainRunId] = "chain-001";
        entity[OpportunityScanColumns.Targets] = JsonSerializer.Serialize(targets, _jsonOptions);

        var run = _sut.MapToOpportunityScanRun(entity);

        Assert.That(run.Targets, Has.Count.EqualTo(1));
        Assert.That(run.Targets[0].SignalStrength, Is.EqualTo(SignalStrength.Strong));
    }

    [Test]
    [Category("OpportunityScanRun")]
    public void MapToOpportunityScanRun_PreservesSubstitutionChainRunId()
    {
        var entity = CreateBaseEntity("2026-04-07", "opp-001");
        entity[OpportunityScanColumns.SubstitutionChainRunId] = "chain-ref-456";
        entity[OpportunityScanColumns.Targets] = "[]";

        var run = _sut.MapToOpportunityScanRun(entity);

        Assert.That(run.SubstitutionChainRunId, Is.EqualTo("chain-ref-456"));
    }

    private static TableEntity CreateBaseEntity(string partitionKey, string rowKey)
    {
        return new TableEntity(partitionKey, rowKey)
        {
            { BaseColumns.ModelId, "gpt-5.4-mini" },
            { BaseColumns.Status, "Success" },
            { BaseColumns.DurationSeconds, 11.5 },
            { BaseColumns.InputTokens, 100 },
            { BaseColumns.OutputTokens, 200 },
            { BaseColumns.TotalTokens, 300 }
        };
    }
}
