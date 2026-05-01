using System.Text.Json;
using KanelBrief.Core.Models;
using KanelBrief.Core.Serialization;

namespace KanelBrief.Core.Tests.Models;

/// <summary>
/// The frontend expects camelCase JSON (runDate, hasData, newsBrief, ...).
/// Azure Functions' WriteAsJsonAsync uses the default .NET serializer which outputs PascalCase.
/// These tests ensure the dashboard response is always serialized as camelCase using
/// the shared <see cref="KanelJsonOptions.CamelCase"/> contract.
/// </summary>
[TestFixture]
public class DashboardSerializationTests
{
    private static readonly JsonSerializerOptions CamelCase = KanelJsonOptions.CamelCase;

    [Test]
    public void DashboardResponse_SerializedWithCamelCase_HasCorrectPropertyNames()
    {
        // Arrange
        var dashboard = new DashboardResponse
        {
            RunDate = "2026-04-10",
            HasData = true,
            NewsBrief = new NewsBriefRun
            {
                RunDate = "2026-04-10",
                RunId = "run-1",
                ModelId = "gpt-5.4-mini",
                Mood = "Mixed",
                Summary = "Test summary",
                Assessments = []
            }
        };

        // Act
        var json = JsonSerializer.Serialize(dashboard, CamelCase);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.That(root.TryGetProperty("runDate", out _), Is.True);
        Assert.That(root.TryGetProperty("hasData", out _), Is.True);
        Assert.That(root.TryGetProperty("newsBrief", out _), Is.True);
        Assert.That(root.TryGetProperty("weeklySummary", out _), Is.True);
        Assert.That(root.TryGetProperty("substitutionChain", out _), Is.True);
        Assert.That(root.TryGetProperty("opportunityScan", out _), Is.True);

        Assert.That(root.TryGetProperty("RunDate", out _), Is.False);
        Assert.That(root.TryGetProperty("HasData", out _), Is.False);
        Assert.That(root.TryGetProperty("NewsBrief", out _), Is.False);
    }

    [Test]
    public void DashboardResponse_NestedRunProperties_AreCamelCase()
    {
        // Arrange
        var dashboard = new DashboardResponse
        {
            RunDate = "2026-04-10",
            HasData = true,
            NewsBrief = new NewsBriefRun
            {
                RunDate = "2026-04-10",
                RunId = "run-1",
                ModelId = "gpt-5.4-mini",
                DurationSeconds = 11.2,
                InputTokens = 1840,
                OutputTokens = 920,
                TotalTokens = 2760,
                Mood = "Mixed",
                Summary = "Test summary",
                Assessments =
                [
                    new CategoryAssessment
                    {
                        Category = "Technology",
                        Headline = "AI drives growth",
                        Summary = "Tech is up",
                        Sentiment = MarketSentiment.RiskOn
                    }
                ]
            }
        };

        // Act
        var json = JsonSerializer.Serialize(dashboard, CamelCase);
        using var doc = JsonDocument.Parse(json);
        var brief = doc.RootElement.GetProperty("newsBrief");

        // Assert
        Assert.That(brief.TryGetProperty("runDate", out _), Is.True);
        Assert.That(brief.TryGetProperty("runId", out _), Is.True);
        Assert.That(brief.TryGetProperty("modelId", out _), Is.True);
        Assert.That(brief.TryGetProperty("durationSeconds", out _), Is.True);
        Assert.That(brief.TryGetProperty("totalTokens", out _), Is.True);

        var assessment = brief.GetProperty("assessments")[0];
        Assert.That(assessment.TryGetProperty("category", out _), Is.True);
        Assert.That(assessment.TryGetProperty("headline", out _), Is.True);
        Assert.That(assessment.TryGetProperty("sentiment", out _), Is.True);
    }

    [Test]
    public void DashboardResponse_WeeklySummary_SerializesPeriodFieldsWithCamelCase()
    {
        // Arrange
        var dashboard = new DashboardResponse
        {
            HasData = true,
            WeeklySummary = new WeeklySummaryRun
            {
                RunDate = "2026-04-27",
                RunId = "weekly-002",
                ModelId = "gpt-5.4-mini",
                Status = RunStatus.Success,
                PeriodStart = new DateTimeOffset(2026, 4, 20, 0, 0, 0, TimeSpan.Zero),
                PeriodEnd = new DateTimeOffset(2026, 4, 26, 0, 0, 0, TimeSpan.Zero),
                PeriodIsoWeek = "2026-W17",
                NetMood = MarketSentiment.Mixed,
                MoodSummary = "Mixed week"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(dashboard, CamelCase);
        using var doc = JsonDocument.Parse(json);
        var weekly = doc.RootElement.GetProperty("weeklySummary");

        // Assert
        Assert.That(weekly.TryGetProperty("reportType", out var reportType), Is.True);
        Assert.That(reportType.GetString(), Is.EqualTo("weekly-summary"));
        Assert.That(weekly.TryGetProperty("periodStart", out _), Is.True);
        Assert.That(weekly.TryGetProperty("periodEnd", out _), Is.True);
        Assert.That(weekly.TryGetProperty("periodIsoWeek", out var isoWeek), Is.True);
        Assert.That(isoWeek.GetString(), Is.EqualTo("2026-W17"));
        // Old keys must not appear on the wire.
        Assert.That(weekly.TryGetProperty("weekStart", out _), Is.False);
        Assert.That(weekly.TryGetProperty("weekEnd", out _), Is.False);
    }

    [Test]
    public void DashboardResponse_SubstitutionChainAndScan_CarryReportTypeAndPeriodFields()
    {
        // Even though period* on chain/scan is lazy-filled by the repository, the wire shape
        // must always include the fields. This test pins them.
        var dashboard = new DashboardResponse
        {
            HasData = true,
            SubstitutionChain = new SubstitutionChainRun
            {
                RunDate = "2026-04-27",
                RunId = "chain-002",
                ModelId = "gpt-5.4-mini",
                Status = RunStatus.Success,
                WeeklySummaryRunId = "weekly-002",
                PeriodStart = new DateTimeOffset(2026, 4, 20, 0, 0, 0, TimeSpan.Zero),
                PeriodEnd = new DateTimeOffset(2026, 4, 26, 0, 0, 0, TimeSpan.Zero),
                PeriodIsoWeek = "2026-W17"
            },
            OpportunityScan = new OpportunityScanRun
            {
                RunDate = "2026-04-27",
                RunId = "opp-002",
                ModelId = "gpt-5.4-mini",
                Status = RunStatus.Success,
                SubstitutionChainRunId = "chain-002",
                PeriodStart = new DateTimeOffset(2026, 4, 20, 0, 0, 0, TimeSpan.Zero),
                PeriodEnd = new DateTimeOffset(2026, 4, 26, 0, 0, 0, TimeSpan.Zero),
                PeriodIsoWeek = "2026-W17"
            }
        };

        var json = JsonSerializer.Serialize(dashboard, CamelCase);
        using var doc = JsonDocument.Parse(json);
        var chain = doc.RootElement.GetProperty("substitutionChain");
        var scan = doc.RootElement.GetProperty("opportunityScan");

        Assert.That(chain.GetProperty("reportType").GetString(), Is.EqualTo("substitution-chain"));
        Assert.That(chain.TryGetProperty("periodStart", out _), Is.True);
        Assert.That(chain.GetProperty("periodIsoWeek").GetString(), Is.EqualTo("2026-W17"));

        Assert.That(scan.GetProperty("reportType").GetString(), Is.EqualTo("rotation-targets"));
        Assert.That(scan.TryGetProperty("periodStart", out _), Is.True);
        Assert.That(scan.GetProperty("periodIsoWeek").GetString(), Is.EqualTo("2026-W17"));
    }

    [Test]
    public void DashboardResponse_DefaultSerializer_ProducesPascalCase()
    {
        // Arrange
        var dashboard = new DashboardResponse
        {
            RunDate = "2026-04-10",
            HasData = true
        };

        // Act
        // This is what WriteAsJsonAsync uses — proves the bug
        var json = JsonSerializer.Serialize(dashboard);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.That(root.TryGetProperty("RunDate", out _), Is.True);
        Assert.That(root.TryGetProperty("runDate", out _), Is.False);
    }
}
