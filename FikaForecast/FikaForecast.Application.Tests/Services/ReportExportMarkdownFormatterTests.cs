using FikaForecast.Application.DTOs;
using FikaForecast.Application.Services;

namespace FikaForecast.Application.Tests.Services;

[TestFixture]
[TestOf(typeof(ReportExportMarkdownFormatter))]
public class ReportExportMarkdownFormatterTests
{
    private ReportExportMarkdownFormatter _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new ReportExportMarkdownFormatter();
    }

    private static ExportReportContext Context(
        ReportType type = ReportType.SubstitutionChain,
        string body = "🔴 **Fleeing:** EM debt",
        string modelId = "gpt-5.4-mini",
        Guid? runId = null)
    {
        var start = new DateTimeOffset(2026, 4, 7, 0, 0, 0, TimeSpan.FromHours(2));
        var end = new DateTimeOffset(2026, 4, 13, 23, 59, 59, TimeSpan.FromHours(2));
        var generated = new DateTimeOffset(2026, 4, 16, 22, 10, 0, TimeSpan.FromHours(2));
        return new ExportReportContext(type, start, end, generated, modelId, runId ?? Guid.Parse("3f2cffff-ffff-ffff-ffff-ffffffffffff"), body);
    }

    #region Header & metadata

    [Test]
    public void Format_H1_UsesReportTypeLabelAndIsoWeekAndRange()
    {
        // Arrange
        var ctx = Context(ReportType.SubstitutionChain);

        // Act
        var md = _sut.Format(ctx);

        // Assert
        Assert.That(md, Does.StartWith("# Substitution Chain — Week 15 (Apr 7 – Apr 13, 2026)"));
    }

    [TestCase(ReportType.WeeklySummary, "Weekly Summary")]
    [TestCase(ReportType.SubstitutionChain, "Substitution Chain")]
    [TestCase(ReportType.RotationTargets, "Rotation Targets")]
    public void Format_H1_MatchesReportTypeLabel(ReportType type, string expectedLabel)
    {
        // Arrange
        var ctx = Context(type);

        // Act
        var md = _sut.Format(ctx);

        // Assert
        Assert.That(md, Does.Contain($"# {expectedLabel} — Week 15"));
    }

    [Test]
    public void Format_MetadataBlock_ContainsAllFieldsAsYaml()
    {
        // Arrange
        var runId = Guid.Parse("3f2cffff-ffff-ffff-ffff-ffffffffffff");
        var ctx = Context(ReportType.RotationTargets, runId: runId);

        // Act
        var md = _sut.Format(ctx);

        // Assert — YAML keys are stable, machine-parseable, snake_case.
        Assert.That(md, Does.Contain("report_type: rotation-targets"));
        Assert.That(md, Does.Contain("iso_week: 2026-W15"));
        Assert.That(md, Does.Contain("period_start: 2026-04-07"));
        Assert.That(md, Does.Contain("period_end: 2026-04-13"));
        Assert.That(md, Does.Contain("generated_at: 2026-04-16T22:10:00+02:00"));
        Assert.That(md, Does.Contain("model: gpt-5.4-mini"));
        Assert.That(md, Does.Contain($"run_id: {runId}"));
    }

    [Test]
    public void Format_MetadataBlock_IsFencedAndUsesYamlCodeBlock()
    {
        // Arrange
        var ctx = Context(ReportType.RotationTargets);

        // Act
        var md = _sut.Format(ctx);

        // Assert — metadata is wrapped in BEGIN/END METADATA comments around a ```yaml block,
        // so agents can split reports and parse metadata deterministically.
        var beginMeta = md.IndexOf("<!-- BEGIN METADATA -->", StringComparison.Ordinal);
        var yamlOpen = md.IndexOf("```yaml", StringComparison.Ordinal);
        var yamlClose = md.IndexOf("```", yamlOpen + "```yaml".Length, StringComparison.Ordinal);
        var endMeta = md.IndexOf("<!-- END METADATA -->", StringComparison.Ordinal);

        Assert.That(beginMeta, Is.GreaterThan(0));
        Assert.That(yamlOpen, Is.GreaterThan(beginMeta));
        Assert.That(yamlClose, Is.GreaterThan(yamlOpen));
        Assert.That(endMeta, Is.GreaterThan(yamlClose));
    }

    [Test]
    public void Format_MetadataFences_OrderedBeforeReportFences()
    {
        // Arrange
        var ctx = Context(ReportType.RotationTargets);

        // Act
        var md = _sut.Format(ctx);

        // Assert
        var endMeta = md.IndexOf("<!-- END METADATA -->", StringComparison.Ordinal);
        var beginReport = md.IndexOf("<!-- BEGIN REPORT -->", StringComparison.Ordinal);
        Assert.That(endMeta, Is.GreaterThan(0));
        Assert.That(beginReport, Is.GreaterThan(endMeta));
    }

    [Test]
    public void Format_WeekRange_CrossYear_ShowsBothYears()
    {
        // Arrange — week crossing 2025→2026.
        var start = new DateTimeOffset(2025, 12, 29, 0, 0, 0, TimeSpan.FromHours(1));
        var end = new DateTimeOffset(2026, 1, 4, 0, 0, 0, TimeSpan.FromHours(1));
        var ctx = new ExportReportContext(
            ReportType.WeeklySummary, start, end, start, "m", Guid.NewGuid(), "body");

        // Act
        var md = _sut.Format(ctx);

        // Assert
        Assert.That(md, Does.Contain("Dec 29, 2025 – Jan 4, 2026"));
    }

    #endregion

    #region Fences & body

    [Test]
    public void Format_BodyEmbeddedBetweenBeginAndEndFences()
    {
        // Arrange
        var body = "custom report body with\n\nmultiple paragraphs";
        var ctx = Context(body: body);

        // Act
        var md = _sut.Format(ctx);

        // Assert
        var beginIdx = md.IndexOf("<!-- BEGIN REPORT -->", StringComparison.Ordinal);
        var endIdx = md.IndexOf("<!-- END REPORT -->", StringComparison.Ordinal);
        var bodyIdx = md.IndexOf(body, StringComparison.Ordinal);

        Assert.That(beginIdx, Is.GreaterThan(0));
        Assert.That(endIdx, Is.GreaterThan(beginIdx));
        Assert.That(bodyIdx, Is.InRange(beginIdx, endIdx));
    }

    [Test]
    public void Format_FencesExactlyOnce_ForAiAgentSplitting()
    {
        // Arrange — body itself contains the fence-looking text. It should still appear
        // exactly once each, but the occurrences inside the body must survive as plain text.
        var body = "Text containing <!-- BEGIN REPORT --> inside body";
        var ctx = Context(body: body);

        // Act
        var md = _sut.Format(ctx);

        // Assert
        var beginCount = CountOccurrences(md, "<!-- BEGIN REPORT -->");
        var endCount = CountOccurrences(md, "<!-- END REPORT -->");
        Assert.That(beginCount, Is.EqualTo(2), "Body text is preserved so the fence string appears twice; the fence itself is still present.");
        Assert.That(endCount, Is.EqualTo(1));
    }

    [Test]
    public void Format_Body_TrailingWhitespaceTrimmed_BeforeEndFence()
    {
        // Arrange
        var body = "body text\n\n\n\n";
        var ctx = Context(body: body);

        // Act
        var md = _sut.Format(ctx);

        // Assert — the end fence should directly follow the body with exactly one blank line.
        Assert.That(md, Does.Contain("body text\r\n\r\n<!-- END REPORT -->")
            .Or.Contain("body text\n\n<!-- END REPORT -->"));
    }

    #endregion

    #region Slugs & labels

    [TestCase(ReportType.WeeklySummary, "weekly-summary")]
    [TestCase(ReportType.SubstitutionChain, "substitution-chain")]
    [TestCase(ReportType.RotationTargets, "rotation-targets")]
    public void FileSlug_MatchesPlanSpec(ReportType type, string expected)
    {
        // Act
        var slug = ReportExportMarkdownFormatter.FileSlug(type);

        // Assert
        Assert.That(slug, Is.EqualTo(expected));
    }

    #endregion

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
}
