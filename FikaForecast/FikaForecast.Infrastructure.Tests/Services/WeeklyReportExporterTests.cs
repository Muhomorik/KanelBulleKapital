using System.IO;
using AutoFixture;
using AutoFixture.AutoMoq;
using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.Enums;
using FikaForecast.Domain.ValueObjects;
using FikaForecast.Infrastructure.Services;
using Moq;

namespace FikaForecast.Infrastructure.Tests.Services;

[TestFixture]
[TestOf(typeof(WeeklyReportExporter))]
public class WeeklyReportExporterTests
{
    private IFixture _fixture = null!;
    private Mock<IExportSettingsProvider> _settingsMock = null!;
    private Mock<IWeeklySummaryRunRepository> _weeklyRepoMock = null!;
    private Mock<ISubstitutionChainRunRepository> _chainRepoMock = null!;
    private Mock<IOpportunityScanRunRepository> _scanRepoMock = null!;
    private string _tempFolder = null!;
    private WeeklyReportExporter _sut = null!;

    private static readonly ModelConfig TestModel = new("gpt-5.4-mini", "gpt-5.4-mini", "GPT-5.4 Mini");
    private static readonly DateTimeOffset WeekStart = new(2026, 4, 7, 0, 0, 0, TimeSpan.FromHours(2));
    private static readonly DateTimeOffset WeekEnd = new(2026, 4, 13, 23, 59, 59, TimeSpan.FromHours(2));

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());

        _settingsMock = _fixture.Freeze<Mock<IExportSettingsProvider>>();
        _weeklyRepoMock = _fixture.Freeze<Mock<IWeeklySummaryRunRepository>>();
        _chainRepoMock = _fixture.Freeze<Mock<ISubstitutionChainRunRepository>>();
        _scanRepoMock = _fixture.Freeze<Mock<IOpportunityScanRunRepository>>();

        _tempFolder = Path.Combine(Path.GetTempPath(), "FikaForecast.Tests", Guid.NewGuid().ToString("N"));

        _sut = _fixture.Create<WeeklyReportExporter>();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempFolder))
            Directory.Delete(_tempFolder, recursive: true);
    }

    private void EnableAutoExport()
    {
        _settingsMock.SetupGet(s => s.AutoExportEnabled).Returns(true);
        _settingsMock.SetupGet(s => s.ExportFolderPath).Returns(_tempFolder);
    }

    private static WeeklySummaryRun SuccessfulWeeklyRun(string body = "weekly body")
    {
        var run = WeeklySummaryRun.Start(TestModel, WeekStart, WeekEnd);
        run.Complete("{}", TimeSpan.FromSeconds(1), 10, 20);
        run.SetDisplayMarkdown(body);
        return run;
    }

    private static SubstitutionChainRun SuccessfulChainRun(Guid weeklyId, string body = "chain body")
    {
        var run = SubstitutionChainRun.Start(TestModel, weeklyId);
        run.Complete("{}", TimeSpan.FromSeconds(1), 10, 20);
        run.SetDisplayMarkdown(body);
        return run;
    }

    private static OpportunityScanRun SuccessfulScanRun(Guid chainId, string body = "scan body")
    {
        var run = OpportunityScanRun.Start(TestModel, chainId);
        run.Complete("{}", TimeSpan.FromSeconds(1), 10, 20);
        run.SetDisplayMarkdown(body);
        return run;
    }

    #region Auto-export guard conditions

    [Test]
    public async Task ExportWeeklySummaryAsync_AutoExportDisabled_DoesNotWriteFile()
    {
        // Arrange
        _settingsMock.SetupGet(s => s.AutoExportEnabled).Returns(false);
        _settingsMock.SetupGet(s => s.ExportFolderPath).Returns(_tempFolder);
        var run = SuccessfulWeeklyRun();

        // Act
        await _sut.ExportWeeklySummaryAsync(run);

        // Assert
        Assert.That(Directory.Exists(_tempFolder), Is.False);
    }

    [Test]
    public async Task ExportWeeklySummaryAsync_AutoExportEnabledButPathEmpty_DoesNotWriteFile()
    {
        // Arrange
        _settingsMock.SetupGet(s => s.AutoExportEnabled).Returns(true);
        _settingsMock.SetupGet(s => s.ExportFolderPath).Returns((string?)null);
        var run = SuccessfulWeeklyRun();

        // Act
        await _sut.ExportWeeklySummaryAsync(run);

        // Assert
        Assert.That(Directory.Exists(_tempFolder), Is.False);
    }

    [Test]
    public async Task ExportWeeklySummaryAsync_RunFailed_SkipsExport()
    {
        // Arrange
        EnableAutoExport();
        var run = WeeklySummaryRun.Start(TestModel, WeekStart, WeekEnd);
        run.Fail(TimeSpan.FromSeconds(1));

        // Act
        await _sut.ExportWeeklySummaryAsync(run);

        // Assert
        Assert.That(Directory.Exists(_tempFolder) && Directory.GetFiles(_tempFolder).Length > 0, Is.False);
    }

    #endregion

    #region Auto-export happy path

    [Test]
    public async Task ExportWeeklySummaryAsync_WritesFileWithCorrectNameAndHeader()
    {
        // Arrange
        EnableAutoExport();
        var run = SuccessfulWeeklyRun("weekly body payload");

        // Act
        await _sut.ExportWeeklySummaryAsync(run);

        // Assert
        var expected = Path.Combine(_tempFolder, "2026-W15-weekly-summary.md");
        Assert.That(File.Exists(expected), Is.True, $"Expected file at {expected}");

        var content = await File.ReadAllTextAsync(expected);
        Assert.That(content, Does.StartWith("# Weekly Summary — Week 15"));
        Assert.That(content, Does.Contain("weekly body payload"));
        Assert.That(content, Does.Contain("<!-- BEGIN REPORT -->"));
        Assert.That(content, Does.Contain("<!-- END REPORT -->"));
    }

    [Test]
    public async Task ExportSubstitutionChainAsync_ResolvesParentWeekViaRepository_AndWritesChainFile()
    {
        // Arrange
        EnableAutoExport();
        var weekly = SuccessfulWeeklyRun();
        var chain = SuccessfulChainRun(weekly.RunId, "chain body payload");
        _weeklyRepoMock.Setup(r => r.GetByIdAsync(weekly.RunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(weekly);

        // Act
        await _sut.ExportSubstitutionChainAsync(chain);

        // Assert
        var expected = Path.Combine(_tempFolder, "2026-W15-substitution-chain.md");
        Assert.That(File.Exists(expected), Is.True);
        var content = await File.ReadAllTextAsync(expected);
        Assert.That(content, Does.StartWith("# Substitution Chain — Week 15"));
        Assert.That(content, Does.Contain("chain body payload"));
    }

    [Test]
    public async Task ExportOpportunityScanAsync_ResolvesGrandparentWeekViaFkChain_AndWritesTargetsFile()
    {
        // Arrange
        EnableAutoExport();
        var weekly = SuccessfulWeeklyRun();
        var chain = SuccessfulChainRun(weekly.RunId);
        var scan = SuccessfulScanRun(chain.RunId, "scan body payload");
        _weeklyRepoMock.Setup(r => r.GetByIdAsync(weekly.RunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(weekly);
        _chainRepoMock.Setup(r => r.GetByIdAsync(chain.RunId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chain);

        // Act
        await _sut.ExportOpportunityScanAsync(scan);

        // Assert
        var expected = Path.Combine(_tempFolder, "2026-W15-rotation-targets.md");
        Assert.That(File.Exists(expected), Is.True);
        var content = await File.ReadAllTextAsync(expected);
        Assert.That(content, Does.StartWith("# Rotation Targets — Week 15"));
        Assert.That(content, Does.Contain("scan body payload"));
    }

    [Test]
    public async Task ExportWeeklySummaryAsync_ExistingFile_IsOverwritten()
    {
        // Arrange
        EnableAutoExport();
        Directory.CreateDirectory(_tempFolder);
        var path = Path.Combine(_tempFolder, "2026-W15-weekly-summary.md");
        await File.WriteAllTextAsync(path, "stale content");
        var run = SuccessfulWeeklyRun("fresh content");

        // Act
        await _sut.ExportWeeklySummaryAsync(run);

        // Assert
        var content = await File.ReadAllTextAsync(path);
        Assert.That(content, Does.Contain("fresh content"));
        Assert.That(content, Does.Not.Contain("stale content"));
    }

    [Test]
    public async Task ExportSubstitutionChainAsync_ParentWeeklyMissing_DoesNotWriteFileAndDoesNotThrow()
    {
        // Arrange — parent lookup returns null; simulates an orphaned row (shouldn't happen via normal flow).
        EnableAutoExport();
        var chain = SuccessfulChainRun(Guid.NewGuid());
        _weeklyRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeeklySummaryRun?)null);

        // Act
        await _sut.ExportSubstitutionChainAsync(chain);

        // Assert
        Assert.That(Directory.Exists(_tempFolder) && Directory.GetFiles(_tempFolder).Length > 0, Is.False);
    }

    #endregion

    #region Manual export

    [Test]
    public void ExportAllForWeekAsync_ExportFolderMissing_ThrowsInvalidOperation()
    {
        // Arrange
        _settingsMock.SetupGet(s => s.ExportFolderPath).Returns((string?)null);

        // Act + Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ExportAllForWeekAsync(WeekStart));
    }

    [Test]
    public async Task ExportAllForWeekAsync_WithThreeReports_WritesAllThreeFiles()
    {
        // Arrange — manual export ignores AutoExportEnabled; only ExportFolderPath matters.
        _settingsMock.SetupGet(s => s.AutoExportEnabled).Returns(false);
        _settingsMock.SetupGet(s => s.ExportFolderPath).Returns(_tempFolder);

        var weekly = SuccessfulWeeklyRun("w body");
        var chain = SuccessfulChainRun(weekly.RunId, "c body");
        var scan = SuccessfulScanRun(chain.RunId, "s body");

        _weeklyRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { weekly });
        _chainRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { chain });
        _scanRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { scan });

        // Act
        var written = await _sut.ExportAllForWeekAsync(WeekStart);

        // Assert
        Assert.That(written, Has.Count.EqualTo(3));
        Assert.That(File.Exists(Path.Combine(_tempFolder, "2026-W15-weekly-summary.md")), Is.True);
        Assert.That(File.Exists(Path.Combine(_tempFolder, "2026-W15-substitution-chain.md")), Is.True);
        Assert.That(File.Exists(Path.Combine(_tempFolder, "2026-W15-rotation-targets.md")), Is.True);
    }

    [Test]
    public async Task ExportAllForWeekAsync_PicksMostRecentSuccessfulWeekly_IgnoresFailedAndOtherWeeks()
    {
        // Arrange
        _settingsMock.SetupGet(s => s.ExportFolderPath).Returns(_tempFolder);

        // Older successful run (same week), newer failed run (same week), unrelated week.
        var older = SuccessfulWeeklyRun("OLDER body");
        await Task.Delay(2); // ensure distinct Timestamp
        var newerSuccessful = SuccessfulWeeklyRun("NEWER body");
        var otherWeek = SuccessfulWeeklyRun("OTHER body");
        // Force the "other week" to a different ISO week.
        var otherWeekStart = WeekStart.AddDays(14);
        var otherWeekEnd = WeekEnd.AddDays(14);
        var other = WeeklySummaryRun.Rehydrate(
            Guid.NewGuid(), otherWeekStart, otherWeekEnd,
            DateTimeOffset.Now, TestModel.ModelId, TimeSpan.Zero, 0, 0, 0,
            RunStatus.Success, "{}", "OTHER body", MarketSentiment.Mixed, "", Array.Empty<WeeklySummaryTheme>());

        var failed = WeeklySummaryRun.Start(TestModel, WeekStart, WeekEnd);
        failed.Fail(TimeSpan.FromSeconds(1));
        failed.SetDisplayMarkdown("FAILED body");

        _weeklyRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { older, newerSuccessful, other, failed });
        _chainRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SubstitutionChainRun>());
        _scanRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<OpportunityScanRun>());

        // Act
        var written = await _sut.ExportAllForWeekAsync(WeekStart);

        // Assert
        Assert.That(written, Has.Count.EqualTo(1));
        var content = await File.ReadAllTextAsync(written[0]);
        Assert.That(content, Does.Contain("NEWER body"));
        Assert.That(content, Does.Not.Contain("OLDER body"));
        Assert.That(content, Does.Not.Contain("OTHER body"));
        Assert.That(content, Does.Not.Contain("FAILED body"));
    }

    [Test]
    public async Task ExportAllForWeekAsync_NoWeeklyRunForWeek_ReturnsEmptyAndWritesNothing()
    {
        // Arrange
        _settingsMock.SetupGet(s => s.ExportFolderPath).Returns(_tempFolder);
        _weeklyRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<WeeklySummaryRun>());
        _chainRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SubstitutionChainRun>());
        _scanRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<OpportunityScanRun>());

        // Act
        var written = await _sut.ExportAllForWeekAsync(WeekStart);

        // Assert
        Assert.That(written, Is.Empty);
        Assert.That(Directory.Exists(_tempFolder) && Directory.GetFiles(_tempFolder).Length > 0, Is.False);
    }

    #endregion
}
