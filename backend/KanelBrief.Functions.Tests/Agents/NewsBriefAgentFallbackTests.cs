using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;
using KanelBrief.Functions.Agents;
using Microsoft.Extensions.Logging;
using Moq;

namespace KanelBrief.Functions.Tests.Agents;

[TestFixture]
[TestOf(typeof(NewsBriefAgent))]
public class NewsBriefAgentFallbackTests
{
    private NewsBriefAgent _sut = null!;

    [SetUp]
    public void SetUp()
    {
        // AIProjectClient requires Azure credentials — pass null since fallback methods don't use it
        _sut = new NewsBriefAgent(
            Mock.Of<ILogger<NewsBriefAgent>>(),
            null!,
            Mock.Of<IAgentRunRepository>());
    }

    [Test]
    public void GenerateFallbackSummary_MultipleCategories_ListsDistinctCategories()
    {
        var articles = new List<NewsArticle>
        {
            new() { Title = "A", Content = "a", Category = "Tech" },
            new() { Title = "B", Content = "b", Category = "Energy" },
            new() { Title = "C", Content = "c", Category = "Tech" },
            new() { Title = "D", Content = "d", Category = "Financials" }
        };

        var summary = _sut.GenerateFallbackSummary(articles);

        Assert.That(summary, Does.Contain("3 sectors"));
        Assert.That(summary, Does.Contain("Tech"));
        Assert.That(summary, Does.Contain("Energy"));
        Assert.That(summary, Does.Contain("Financials"));
    }

    [Test]
    public void GenerateFallbackSummary_SingleCategory_ListsSingleCategory()
    {
        var articles = new List<NewsArticle>
        {
            new() { Title = "A", Content = "a", Category = "Tech" },
            new() { Title = "B", Content = "b", Category = "Tech" }
        };

        var summary = _sut.GenerateFallbackSummary(articles);

        Assert.That(summary, Does.Contain("1 sectors"));
        Assert.That(summary, Does.Contain("Tech"));
    }

    [Test]
    public void GenerateFallbackAssessments_GroupsByCategory_OnePerCategory()
    {
        var articles = new List<NewsArticle>
        {
            new() { Title = "A", Content = "content a", Category = "Tech" },
            new() { Title = "B", Content = "content b", Category = "Energy" },
            new() { Title = "C", Content = "content c", Category = "Tech" }
        };

        var assessments = _sut.GenerateFallbackAssessments(articles);

        Assert.That(assessments, Has.Count.EqualTo(2));
        Assert.That(assessments.Select(a => a.Category), Is.EquivalentTo(new[] { "Tech", "Energy" }));
    }

    [Test]
    public void GenerateFallbackAssessments_UsesFirstArticleTitle_AsHeadline()
    {
        var articles = new List<NewsArticle>
        {
            new() { Title = "First Tech", Content = "content", Category = "Tech" },
            new() { Title = "Second Tech", Content = "content", Category = "Tech" }
        };

        var assessments = _sut.GenerateFallbackAssessments(articles);

        Assert.That(assessments[0].Headline, Is.EqualTo("First Tech"));
    }

    [Test]
    public void GenerateFallbackAssessments_LongContent_TruncatesTo100Chars()
    {
        var longContent = new string('x', 200);
        var articles = new List<NewsArticle>
        {
            new() { Title = "A", Content = longContent, Category = "Tech" }
        };

        var assessments = _sut.GenerateFallbackAssessments(articles);

        Assert.That(assessments[0].Summary, Has.Length.EqualTo(100));
    }

    [Test]
    public void GenerateFallbackAssessments_ShortContent_PreservesFullContent()
    {
        var shortContent = "Short content here";
        var articles = new List<NewsArticle>
        {
            new() { Title = "A", Content = shortContent, Category = "Tech" }
        };

        var assessments = _sut.GenerateFallbackAssessments(articles);

        Assert.That(assessments[0].Summary, Is.EqualTo(shortContent));
    }

    [Test]
    public void GenerateFallbackAssessments_AllSentiments_DefaultToMixed()
    {
        var articles = new List<NewsArticle>
        {
            new() { Title = "A", Content = "content", Category = "Tech" },
            new() { Title = "B", Content = "content", Category = "Energy" }
        };

        var assessments = _sut.GenerateFallbackAssessments(articles);

        Assert.That(assessments, Has.All.Property(nameof(CategoryAssessment.Sentiment)).EqualTo(MarketSentiment.Mixed));
    }
}
