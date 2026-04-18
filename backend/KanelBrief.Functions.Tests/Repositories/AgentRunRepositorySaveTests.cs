using Azure;
using Azure.Data.Tables;
using KanelBrief.Core.Models;
using KanelBrief.Functions.Repositories;
using Moq;
using static KanelBrief.Functions.Repositories.AgentRunRepository;

namespace KanelBrief.Functions.Tests.Repositories;

[TestFixture]
[TestOf(typeof(AgentRunRepository))]
public class AgentRunRepositorySaveTests
{
    private Mock<TableClient> _newsBriefTable = null!;
    private Mock<TableClient> _weeklyTable = null!;
    private Mock<TableClient> _substitutionTable = null!;
    private Mock<TableClient> _opportunityTable = null!;
    private AgentRunRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _newsBriefTable = new Mock<TableClient>();
        _weeklyTable = new Mock<TableClient>();
        _substitutionTable = new Mock<TableClient>();
        _opportunityTable = new Mock<TableClient>();

        _sut = new AgentRunRepository(
            _newsBriefTable.Object,
            _weeklyTable.Object,
            _substitutionTable.Object,
            _opportunityTable.Object);
    }

    [Test]
    [Category("WeeklySummaryRun")]
    public async Task SaveWeeklySummaryRunAsync_RunWithCreatedAt_PersistsCreatedAtColumn()
    {
        var createdAt = new DateTimeOffset(2026, 4, 16, 19, 0, 0, TimeSpan.Zero);
        var run = new WeeklySummaryRun
        {
            RunDate = "2026-04-16",
            RunId = "weekly-001",
            CreatedAt = createdAt,
            ModelId = "gpt-5.4-mini",
            Status = RunStatus.Success,
            WeekStart = DateTimeOffset.MinValue,
            WeekEnd = DateTimeOffset.MinValue,
            NetMood = MarketSentiment.Mixed,
            MoodSummary = "",
            Themes = []
        };

        TableEntity? captured = null;
        _weeklyTable
            .Setup(t => t.UpsertEntityAsync(It.IsAny<TableEntity>(), It.IsAny<TableUpdateMode>(), It.IsAny<CancellationToken>()))
            .Callback<TableEntity, TableUpdateMode, CancellationToken>((e, _, _) => captured = e)
            .ReturnsAsync(Mock.Of<Response>());

        await _sut.SaveWeeklySummaryRunAsync(run);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.ContainsKey(BaseColumns.CreatedAt), Is.True, "CreatedAt column must be written to the table entity");
        Assert.That(captured.GetDateTimeOffset(BaseColumns.CreatedAt), Is.EqualTo(createdAt));
    }

    [Test]
    [Category("SubstitutionChainRun")]
    public async Task SaveSubstitutionChainRunAsync_RunWithCreatedAt_PersistsCreatedAtColumn()
    {
        var createdAt = new DateTimeOffset(2026, 4, 16, 19, 2, 0, TimeSpan.Zero);
        var run = new SubstitutionChainRun
        {
            RunDate = "2026-04-16",
            RunId = "chain-001",
            CreatedAt = createdAt,
            ModelId = "gpt-5.4-mini",
            Status = RunStatus.Success,
            WeeklySummaryRunId = "weekly-001",
            Chains = []
        };

        TableEntity? captured = null;
        _substitutionTable
            .Setup(t => t.UpsertEntityAsync(It.IsAny<TableEntity>(), It.IsAny<TableUpdateMode>(), It.IsAny<CancellationToken>()))
            .Callback<TableEntity, TableUpdateMode, CancellationToken>((e, _, _) => captured = e)
            .ReturnsAsync(Mock.Of<Response>());

        await _sut.SaveSubstitutionChainRunAsync(run);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.ContainsKey(BaseColumns.CreatedAt), Is.True, "CreatedAt column must be written to the table entity");
        Assert.That(captured.GetDateTimeOffset(BaseColumns.CreatedAt), Is.EqualTo(createdAt));
    }

    [Test]
    [Category("OpportunityScanRun")]
    public async Task SaveOpportunityScanRunAsync_RunWithCreatedAt_PersistsCreatedAtColumn()
    {
        var createdAt = new DateTimeOffset(2026, 4, 16, 19, 4, 0, TimeSpan.Zero);
        var run = new OpportunityScanRun
        {
            RunDate = "2026-04-16",
            RunId = "opp-001",
            CreatedAt = createdAt,
            ModelId = "gpt-5.4-mini",
            Status = RunStatus.Success,
            SubstitutionChainRunId = "chain-001",
            Targets = []
        };

        TableEntity? captured = null;
        _opportunityTable
            .Setup(t => t.UpsertEntityAsync(It.IsAny<TableEntity>(), It.IsAny<TableUpdateMode>(), It.IsAny<CancellationToken>()))
            .Callback<TableEntity, TableUpdateMode, CancellationToken>((e, _, _) => captured = e)
            .ReturnsAsync(Mock.Of<Response>());

        await _sut.SaveOpportunityScanRunAsync(run);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.ContainsKey(BaseColumns.CreatedAt), Is.True, "CreatedAt column must be written to the table entity");
        Assert.That(captured.GetDateTimeOffset(BaseColumns.CreatedAt), Is.EqualTo(createdAt));
    }
}
