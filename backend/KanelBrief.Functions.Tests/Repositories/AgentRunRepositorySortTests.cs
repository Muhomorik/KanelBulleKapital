using System.Linq.Expressions;
using Azure;
using Azure.Data.Tables;
using KanelBrief.Functions.Repositories;
using Moq;
using static KanelBrief.Functions.Repositories.AgentRunRepository;

namespace KanelBrief.Functions.Tests.Repositories;

[TestFixture]
[TestOf(typeof(AgentRunRepository))]
public class AgentRunRepositorySortTests
{
    private Mock<TableClient> _weeklyTable = null!;
    private Mock<TableClient> _substitutionTable = null!;
    private Mock<TableClient> _opportunityTable = null!;
    private AgentRunRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _weeklyTable = new Mock<TableClient>();
        _substitutionTable = new Mock<TableClient>();
        _opportunityTable = new Mock<TableClient>();

        // Default lazy-fill lookups: parent not found. Tests that exercise lazy-fill
        // override these setups; sort-only tests don't care about the period values.
        _weeklyTable
            .Setup(t => t.GetEntityAsync<TableEntity>(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Not Found"));
        _weeklyTable
            .Setup(t => t.QueryAsync(
                It.IsAny<Expression<Func<TableEntity, bool>>>(),
                It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncPageableFrom(Array.Empty<TableEntity>()));
        _substitutionTable
            .Setup(t => t.GetEntityAsync<TableEntity>(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Not Found"));
        _substitutionTable
            .Setup(t => t.QueryAsync(
                It.IsAny<Expression<Func<TableEntity, bool>>>(),
                It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncPageableFrom(Array.Empty<TableEntity>()));

        _sut = new AgentRunRepository(
            new Mock<TableClient>().Object,
            _weeklyTable.Object,
            _substitutionTable.Object,
            _opportunityTable.Object);
    }

    [Test]
    [Category("WeeklySummaryRun")]
    public async Task GetWeeklySummaryRunsByDateAsync_MultipleRuns_ReturnsSortedByCreatedAtDescending()
    {
        var earliest = new DateTimeOffset(2026, 4, 16, 10, 0, 0, TimeSpan.Zero);
        var middle = new DateTimeOffset(2026, 4, 16, 14, 0, 0, TimeSpan.Zero);
        var latest = new DateTimeOffset(2026, 4, 16, 19, 0, 0, TimeSpan.Zero);

        var entities = new[]
        {
            WeeklyEntity("2026-04-16", "run-early", earliest),
            WeeklyEntity("2026-04-16", "run-late", latest),
            WeeklyEntity("2026-04-16", "run-middle", middle)
        };

        _weeklyTable
            .Setup(t => t.QueryAsync(It.IsAny<Expression<Func<TableEntity, bool>>>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncPageableFrom(entities));

        var result = await _sut.GetWeeklySummaryRunsByDateAsync("2026-04-16");

        Assert.That(result.Select(r => r.RunId), Is.EqualTo(new[] { "run-late", "run-middle", "run-early" }));
    }

    [Test]
    [Category("SubstitutionChainRun")]
    public async Task GetSubstitutionChainRunsByDateAsync_MultipleRuns_ReturnsSortedByCreatedAtDescending()
    {
        var earliest = new DateTimeOffset(2026, 4, 16, 10, 0, 0, TimeSpan.Zero);
        var latest = new DateTimeOffset(2026, 4, 16, 19, 2, 0, TimeSpan.Zero);

        var entities = new[]
        {
            SubstitutionEntity("2026-04-16", "chain-early", earliest),
            SubstitutionEntity("2026-04-16", "chain-late", latest)
        };

        _substitutionTable
            .Setup(t => t.QueryAsync(It.IsAny<Expression<Func<TableEntity, bool>>>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncPageableFrom(entities));

        var result = await _sut.GetSubstitutionChainRunsByDateAsync("2026-04-16");

        Assert.That(result.Select(r => r.RunId), Is.EqualTo(new[] { "chain-late", "chain-early" }));
    }

    [Test]
    [Category("OpportunityScanRun")]
    public async Task GetOpportunityScanRunsByDateAsync_MultipleRuns_ReturnsSortedByCreatedAtDescending()
    {
        var earliest = new DateTimeOffset(2026, 4, 16, 10, 0, 0, TimeSpan.Zero);
        var latest = new DateTimeOffset(2026, 4, 16, 19, 4, 0, TimeSpan.Zero);

        var entities = new[]
        {
            OpportunityEntity("2026-04-16", "opp-early", earliest),
            OpportunityEntity("2026-04-16", "opp-late", latest)
        };

        _opportunityTable
            .Setup(t => t.QueryAsync(It.IsAny<Expression<Func<TableEntity, bool>>>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncPageableFrom(entities));

        var result = await _sut.GetOpportunityScanRunsByDateAsync("2026-04-16");

        Assert.That(result.Select(r => r.RunId), Is.EqualTo(new[] { "opp-late", "opp-early" }));
    }

    [Test]
    [Category("LazyFill")]
    public async Task GetSubstitutionChainRunsByDateAsync_LazyFillFromParent_PopulatesPeriodFields()
    {
        // Arrange — chain in the table, parent weekly summary on the same partition.
        var chainCreatedAt = new DateTimeOffset(2026, 4, 16, 19, 2, 0, TimeSpan.Zero);
        var chainEntities = new[] { SubstitutionEntity("2026-04-16", "chain-001", chainCreatedAt) };
        _substitutionTable
            .Setup(t => t.QueryAsync(It.IsAny<Expression<Func<TableEntity, bool>>>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncPageableFrom(chainEntities));

        var parentEntity = WeeklyEntity("2026-04-16", "weekly-001", chainCreatedAt);
        parentEntity[WeeklySummaryColumns.PeriodStart] = new DateTimeOffset(2026, 4, 6, 0, 0, 0, TimeSpan.Zero);
        parentEntity[WeeklySummaryColumns.PeriodEnd] = new DateTimeOffset(2026, 4, 12, 0, 0, 0, TimeSpan.Zero);
        parentEntity[WeeklySummaryColumns.PeriodIsoWeek] = "2026-W15";

        // Override default 404 — preferred partition lookup hits this.
        _weeklyTable.Reset();
        _weeklyTable
            .Setup(t => t.GetEntityAsync<TableEntity>("2026-04-16", "weekly-001",
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(parentEntity, Mock.Of<Response>()));

        // Act
        var result = await _sut.GetSubstitutionChainRunsByDateAsync("2026-04-16");

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].PeriodStart, Is.EqualTo(new DateTimeOffset(2026, 4, 6, 0, 0, 0, TimeSpan.Zero)));
        Assert.That(result[0].PeriodEnd, Is.EqualTo(new DateTimeOffset(2026, 4, 12, 0, 0, 0, TimeSpan.Zero)));
        Assert.That(result[0].PeriodIsoWeek, Is.EqualTo("2026-W15"));
    }

    [Test]
    [Category("LazyFill")]
    public async Task GetSubstitutionChainRunsByDateAsync_WhenParentMissing_LeavesPeriodAtDefault()
    {
        // Default SetUp throws 404 + returns empty query — parent never found.
        var chainEntities = new[] { SubstitutionEntity("2026-04-16", "chain-orphan",
            new DateTimeOffset(2026, 4, 16, 19, 0, 0, TimeSpan.Zero)) };
        _substitutionTable
            .Setup(t => t.QueryAsync(It.IsAny<Expression<Func<TableEntity, bool>>>(), It.IsAny<int?>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncPageableFrom(chainEntities));

        var result = await _sut.GetSubstitutionChainRunsByDateAsync("2026-04-16");

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].PeriodStart, Is.EqualTo(default(DateTimeOffset)));
        Assert.That(result[0].PeriodIsoWeek, Is.Empty);
    }

    private static TableEntity WeeklyEntity(string runDate, string runId, DateTimeOffset createdAt) =>
        new TableEntity(runDate, runId)
        {
            { BaseColumns.ModelId, "gpt-5.4-mini" },
            { BaseColumns.Status, "Success" },
            { BaseColumns.DurationSeconds, 11.5 },
            { BaseColumns.InputTokens, 100 },
            { BaseColumns.OutputTokens, 200 },
            { BaseColumns.TotalTokens, 300 },
            { BaseColumns.CreatedAt, createdAt },
            { WeeklySummaryColumns.PeriodStart, DateTimeOffset.MinValue },
            { WeeklySummaryColumns.PeriodEnd, DateTimeOffset.MinValue },
            { WeeklySummaryColumns.NetMood, "Mixed" },
            { WeeklySummaryColumns.MoodSummary, "" },
            { WeeklySummaryColumns.Themes, "[]" }
        };

    private static TableEntity SubstitutionEntity(string runDate, string runId, DateTimeOffset createdAt) =>
        new TableEntity(runDate, runId)
        {
            { BaseColumns.ModelId, "gpt-5.4-mini" },
            { BaseColumns.Status, "Success" },
            { BaseColumns.DurationSeconds, 11.5 },
            { BaseColumns.InputTokens, 100 },
            { BaseColumns.OutputTokens, 200 },
            { BaseColumns.TotalTokens, 300 },
            { BaseColumns.CreatedAt, createdAt },
            { SubstitutionChainColumns.WeeklySummaryRunId, "weekly-001" },
            { SubstitutionChainColumns.Chains, "[]" }
        };

    private static TableEntity OpportunityEntity(string runDate, string runId, DateTimeOffset createdAt) =>
        new TableEntity(runDate, runId)
        {
            { BaseColumns.ModelId, "gpt-5.4-mini" },
            { BaseColumns.Status, "Success" },
            { BaseColumns.DurationSeconds, 11.5 },
            { BaseColumns.InputTokens, 100 },
            { BaseColumns.OutputTokens, 200 },
            { BaseColumns.TotalTokens, 300 },
            { BaseColumns.CreatedAt, createdAt },
            { OpportunityScanColumns.SubstitutionChainRunId, "chain-001" },
            { OpportunityScanColumns.Targets, "[]" }
        };

    private static AsyncPageable<TableEntity> AsyncPageableFrom(IEnumerable<TableEntity> entities)
    {
        var page = Page<TableEntity>.FromValues(entities.ToList(), continuationToken: null, response: Mock.Of<Response>());
        return AsyncPageable<TableEntity>.FromPages(new[] { page });
    }
}
