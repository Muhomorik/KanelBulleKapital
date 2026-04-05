using FikaForecast.Domain.Entities;
using FikaForecast.Domain.Enums;
using FikaForecast.Domain.ValueObjects;
using FikaForecast.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FikaForecast.Infrastructure.Tests.Persistence;

[TestFixture]
[TestOf(typeof(NewsBriefRunRepository))]
public class NewsBriefRunRepositoryTests
{
    private FikaDbContext _db = null!;
    private NewsBriefRunRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<FikaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new FikaDbContext(options);
        _sut = new NewsBriefRunRepository(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    #region SaveAsync

    [Test]
    public async Task SaveAsync_PersistsRunWithItemAndAssessments()
    {
        // Arrange
        var run = NewsBriefRun.Start(
            new ModelConfig("gpt-4.1", "gpt-4.1", "GPT-4.1"),
            new AgentPrompt("Test", "You are a test agent."));
        run.Complete("test output", TimeSpan.FromSeconds(1), 10, 20);

        var item = new NewsItem(MarketSentiment.RiskOff, "Risk-off mood");
        item.AddAssessment(new CategoryAssessment("Geopolitics", "War", "Impact", MarketSentiment.RiskOff));
        item.AddAssessment(new CategoryAssessment("Tech / AI", "Nvidia", "AI capex", MarketSentiment.RiskOn));
        run.SetItem(item);

        // Act
        await _sut.SaveAsync(run);

        // Assert
        var saved = await _db.NewsBriefRuns
            .Include(r => r.Item)
            .ThenInclude(i => i!.Assessments)
            .FirstAsync();

        Assert.That(saved.Item, Is.Not.Null);
        Assert.That(saved.Item!.Mood, Is.EqualTo(MarketSentiment.RiskOff));
        Assert.That(saved.Item.Assessments, Has.Count.EqualTo(2));
        Assert.That(saved.Item.Assessments[0].Category, Is.EqualTo("Geopolitics"));
        Assert.That(saved.Item.Assessments[1].Category, Is.EqualTo("Tech / AI"));
    }

    #endregion

    #region GetByIdAsync

    [Test]
    public async Task GetByIdAsync_IncludesItemAndAssessments()
    {
        // Arrange
        var run = NewsBriefRun.Start(
            new ModelConfig("gpt-4.1", "gpt-4.1", "GPT-4.1"),
            new AgentPrompt("Test", "Prompt"));
        run.Complete("output", TimeSpan.FromSeconds(1), 10, 20);

        var item = new NewsItem(MarketSentiment.Mixed, "Mixed signals");
        item.AddAssessment(new CategoryAssessment("Macro", "CPI", "Inflation", MarketSentiment.RiskOff));
        run.SetItem(item);
        await _sut.SaveAsync(run);

        // Act
        var loaded = await _sut.GetByIdAsync(run.RunId);

        // Assert
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Item, Is.Not.Null);
        Assert.That(loaded.Item!.Assessments, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region DeleteAsync

    [Test]
    public async Task DeleteAsync_CascadesDeleteToItemAndAssessments()
    {
        // Arrange
        var run = NewsBriefRun.Start(
            new ModelConfig("gpt-4.1", "gpt-4.1", "GPT-4.1"),
            new AgentPrompt("Test", "Prompt"));
        run.Complete("output", TimeSpan.FromSeconds(1), 10, 20);

        var item = new NewsItem(MarketSentiment.RiskOn, "Bull market");
        item.AddAssessment(new CategoryAssessment("Equities", "S&P", "All-time high", MarketSentiment.RiskOn));
        run.SetItem(item);
        await _sut.SaveAsync(run);

        // Act
        await _sut.DeleteAsync(run.RunId);

        // Assert
        Assert.That(await _db.NewsBriefRuns.CountAsync(), Is.EqualTo(0));
        Assert.That(await _db.NewsItems.CountAsync(), Is.EqualTo(0));
        Assert.That(await _db.CategoryAssessments.CountAsync(), Is.EqualTo(0));
    }

    #endregion
}
