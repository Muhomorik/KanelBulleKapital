using KanelBrief.Functions.Infrastructure;

namespace KanelBrief.Functions.Tests.Infrastructure;

[TestFixture]
[TestOf(typeof(EmbeddedPromptProvider))]
public class EmbeddedPromptProviderTests
{
    private EmbeddedPromptProvider _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new EmbeddedPromptProvider();
    }

    [Test]
    public void GetWeeklySummaryPrompt_ReturnsPopulatedPrompt()
    {
        var prompt = _sut.GetWeeklySummaryPrompt();

        Assert.That(prompt, Is.Not.Null);
        Assert.That(prompt.Name, Is.Not.Empty);
        Assert.That(prompt.SystemPrompt, Is.Not.Empty);
    }

    [Test]
    public void GetSubstitutionChainPrompt_ReturnsPopulatedPrompt()
    {
        var prompt = _sut.GetSubstitutionChainPrompt();

        Assert.That(prompt, Is.Not.Null);
        Assert.That(prompt.Name, Is.Not.Empty);
        Assert.That(prompt.SystemPrompt, Is.Not.Empty);
    }

    [Test]
    public void GetOpportunityScanPrompt_ReturnsPopulatedPrompt()
    {
        var prompt = _sut.GetOpportunityScanPrompt();

        Assert.That(prompt, Is.Not.Null);
        Assert.That(prompt.Name, Is.Not.Empty);
        Assert.That(prompt.SystemPrompt, Is.Not.Empty);
    }

    [Test]
    public void GetWeeklySummaryPrompt_LoadsExpectedFrontmatterName()
    {
        var prompt = _sut.GetWeeklySummaryPrompt();

        Assert.That(prompt.Name, Is.EqualTo("Weekly Summary - Default"));
    }

    [Test]
    public void GetSubstitutionChainPrompt_LoadsExpectedFrontmatterName()
    {
        var prompt = _sut.GetSubstitutionChainPrompt();

        Assert.That(prompt.Name, Is.EqualTo("Substitution Chain - Default"));
    }

    [Test]
    public void GetOpportunityScanPrompt_LoadsExpectedFrontmatterName()
    {
        var prompt = _sut.GetOpportunityScanPrompt();

        Assert.That(prompt.Name, Is.EqualTo("Opportunity Scan - Default"));
    }

    [Test]
    public void GetWeeklySummaryPrompt_BodyContainsKeyDomainRules()
    {
        var prompt = _sut.GetWeeklySummaryPrompt();

        Assert.That(prompt.SystemPrompt, Does.Contain("at least 2 daily briefs"));
        Assert.That(prompt.SystemPrompt, Does.Contain("\"mood\""));
        Assert.That(prompt.SystemPrompt, Does.Contain("\"themes\""));
    }

    [Test]
    public void GetSubstitutionChainPrompt_BodyContainsKeyDomainRules()
    {
        var prompt = _sut.GetSubstitutionChainPrompt();

        Assert.That(prompt.SystemPrompt, Does.Contain("specific theme by name"));
        Assert.That(prompt.SystemPrompt, Does.Contain("\"capitalFleeing\""));
        Assert.That(prompt.SystemPrompt, Does.Contain("\"flowsToward\""));
    }

    [Test]
    public void GetOpportunityScanPrompt_BodyContainsKeyDomainRules()
    {
        var prompt = _sut.GetOpportunityScanPrompt();

        Assert.That(prompt.SystemPrompt, Does.Contain("reference a specific chain"));
        Assert.That(prompt.SystemPrompt, Does.Contain("\"signalStrength\""));
        Assert.That(prompt.SystemPrompt, Does.Contain("\"riskCaveat\""));
    }

    [Test]
    public void GetNewsBriefArticlesPrompt_ReturnsPopulatedPrompt()
    {
        var prompt = _sut.GetNewsBriefArticlesPrompt();

        Assert.That(prompt, Is.Not.Null);
        Assert.That(prompt.Name, Is.Not.Empty);
        Assert.That(prompt.SystemPrompt, Is.Not.Empty);
    }

    [Test]
    public void GetNewsBriefArticlesPrompt_LoadsExpectedFrontmatterName()
    {
        var prompt = _sut.GetNewsBriefArticlesPrompt();

        Assert.That(prompt.Name, Is.EqualTo("News Brief - Articles"));
    }

    [Test]
    public void GetNewsBriefArticlesPrompt_BodyContainsKeyDomainRules()
    {
        var prompt = _sut.GetNewsBriefArticlesPrompt();

        Assert.That(prompt.SystemPrompt, Does.Contain("\"mood\""));
        Assert.That(prompt.SystemPrompt, Does.Contain("\"assessments\""));
        Assert.That(prompt.SystemPrompt, Does.Contain("\"sentiment\""));
    }

    [Test]
    public void GetWeeklySummaryPrompt_RepeatedCalls_ReturnsCachedInstance()
    {
        var first = _sut.GetWeeklySummaryPrompt();
        var second = _sut.GetWeeklySummaryPrompt();

        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void GetAllPrompts_AreDistinctInstances()
    {
        var weekly = _sut.GetWeeklySummaryPrompt();
        var chain = _sut.GetSubstitutionChainPrompt();
        var opportunity = _sut.GetOpportunityScanPrompt();
        var newsArticles = _sut.GetNewsBriefArticlesPrompt();

        Assert.That(weekly, Is.Not.SameAs(chain));
        Assert.That(chain, Is.Not.SameAs(opportunity));
        Assert.That(weekly, Is.Not.SameAs(opportunity));
        Assert.That(newsArticles, Is.Not.SameAs(weekly));
        Assert.That(newsArticles, Is.Not.SameAs(chain));
        Assert.That(newsArticles, Is.Not.SameAs(opportunity));
    }
}
