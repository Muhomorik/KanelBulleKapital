using AutoFixture;
using AutoFixture.AutoMoq;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.Enums;
using FikaForecast.Domain.ValueObjects;

namespace FikaForecast.Domain.Tests.Entities;

[TestFixture]
[TestOf(typeof(NewsBriefRun))]
public class NewsBriefRunTests
{
    private IFixture _fixture = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
    }

    #region Start

    [Test]
    public void Start_CreatesRunWithPartialStatus()
    {
        // Arrange
        var model = _fixture.Create<ModelConfig>();
        var prompt = _fixture.Create<AgentPrompt>();

        // Act
        var run = NewsBriefRun.Start(model, prompt);

        // Assert
        Assert.That(run.Status, Is.EqualTo(RunStatus.Partial));
        Assert.That(run.RunId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(run.ModelId, Is.EqualTo(model.ModelId));
        Assert.That(run.DeploymentName, Is.EqualTo(model.DeploymentName));
        Assert.That(run.PromptName, Is.EqualTo(prompt.Name));
        Assert.That(run.RawMarkdownOutput, Is.Empty);
        Assert.That(run.Item, Is.Null);
    }

    #endregion

    #region Complete

    [Test]
    public void Complete_SetsSuccessStatusAndTokens()
    {
        // Arrange
        var run = NewsBriefRun.Start(_fixture.Create<ModelConfig>(), _fixture.Create<AgentPrompt>());
        var rawJson = """{"mood":"RiskOff","summary":"Test","categories":[]}""";
        var duration = TimeSpan.FromSeconds(5);

        // Act
        run.Complete(rawJson, duration, inputTokens: 100, outputTokens: 200);

        // Assert
        Assert.That(run.Status, Is.EqualTo(RunStatus.Success));
        Assert.That(run.RawAgentOutput, Is.EqualTo(rawJson));
        Assert.That(run.Duration, Is.EqualTo(duration));
        Assert.That(run.InputTokens, Is.EqualTo(100));
        Assert.That(run.OutputTokens, Is.EqualTo(200));
        Assert.That(run.TotalTokens, Is.EqualTo(300));
    }

    [Test]
    public void SetDisplayMarkdown_SetsRawMarkdownOutput()
    {
        // Arrange
        var run = NewsBriefRun.Start(_fixture.Create<ModelConfig>(), _fixture.Create<AgentPrompt>());
        run.Complete("{}", TimeSpan.FromSeconds(1), 10, 20);

        // Act
        run.SetDisplayMarkdown("🔴 GEOPOLITICS\n- **War** — Impact");

        // Assert
        Assert.That(run.RawMarkdownOutput, Is.EqualTo("🔴 GEOPOLITICS\n- **War** — Impact"));
    }

    #endregion

    #region Fail

    [Test]
    public void Fail_SetsFailedStatus()
    {
        // Arrange
        var run = NewsBriefRun.Start(_fixture.Create<ModelConfig>(), _fixture.Create<AgentPrompt>());
        var duration = TimeSpan.FromSeconds(2);

        // Act
        run.Fail(duration);

        // Assert
        Assert.That(run.Status, Is.EqualTo(RunStatus.Failed));
        Assert.That(run.Duration, Is.EqualTo(duration));
    }

    #endregion

    #region MarkPartial

    [Test]
    public void MarkPartial_DowngradesSuccessToPartial()
    {
        // Arrange
        var run = NewsBriefRun.Start(_fixture.Create<ModelConfig>(), _fixture.Create<AgentPrompt>());
        run.Complete("output", TimeSpan.FromSeconds(1), 10, 20);

        // Act
        run.MarkPartial();

        // Assert
        Assert.That(run.Status, Is.EqualTo(RunStatus.Partial));
    }

    [Test]
    public void MarkPartial_DoesNothingIfFailed()
    {
        // Arrange
        var run = NewsBriefRun.Start(_fixture.Create<ModelConfig>(), _fixture.Create<AgentPrompt>());
        run.Fail(TimeSpan.FromSeconds(1));

        // Act
        run.MarkPartial();

        // Assert
        Assert.That(run.Status, Is.EqualTo(RunStatus.Failed));
    }

    #endregion

    #region SetItem

    [Test]
    public void SetItem_AssignsNewsItem()
    {
        // Arrange
        var run = NewsBriefRun.Start(_fixture.Create<ModelConfig>(), _fixture.Create<AgentPrompt>());
        var item = new NewsItem(MarketSentiment.RiskOff, "Test mood summary");

        // Act
        run.SetItem(item);

        // Assert
        Assert.That(run.Item, Is.SameAs(item));
    }

    #endregion
}
