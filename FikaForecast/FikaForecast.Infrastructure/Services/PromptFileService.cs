using System.Reflection;
using FikaForecast.Application.Interfaces;
using NLog;

namespace FikaForecast.Infrastructure.Services;

/// <summary>
/// Reads and writes prompt files from <c>%LocalAppData%\FikaForecast\Prompts\</c>.
/// Falls back to embedded resource defaults when a file is missing on disk.
/// </summary>
public class PromptFileService : IPromptFileService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private const string FileExtension = ".prompt.txt";
    private const string EmbeddedResourcePrefix = "FikaForecast.Application.Prompts.";

    private readonly string _promptsDirectory;
    private readonly Assembly _resourceAssembly;

    public PromptFileService()
    {
        _promptsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FikaForecast",
            "Prompts");

        _resourceAssembly = typeof(IPromptProvider).Assembly;
    }

    /// <inheritdoc />
    public string ReadPromptFile(string promptKey)
    {
        var filePath = GetFilePath(promptKey);

        if (!File.Exists(filePath))
        {
            Logger.Info("Prompt file '{PromptKey}' not found on disk, copying embedded default", promptKey);
            CopyEmbeddedDefault(promptKey, filePath);
        }

        return File.ReadAllText(filePath);
    }

    /// <inheritdoc />
    public void WritePromptFile(string promptKey, string content)
    {
        var filePath = GetFilePath(promptKey);
        Directory.CreateDirectory(_promptsDirectory);
        File.WriteAllText(filePath, content);

        Logger.Info("Prompt file '{PromptKey}' saved to disk", promptKey);
    }

    /// <inheritdoc />
    public void ResetToDefault(string promptKey)
    {
        var filePath = GetFilePath(promptKey);

        if (File.Exists(filePath))
            File.Delete(filePath);

        CopyEmbeddedDefault(promptKey, filePath);
        Logger.Info("Prompt file '{PromptKey}' reset to embedded default", promptKey);
    }

    private string GetFilePath(string promptKey) =>
        Path.Combine(_promptsDirectory, $"{promptKey}{FileExtension}");

    private void CopyEmbeddedDefault(string promptKey, string filePath)
    {
        var resourceName = $"{EmbeddedResourcePrefix}{promptKey}{FileExtension}";
        using var stream = _resourceAssembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            var available = string.Join(", ", _resourceAssembly.GetManifestResourceNames());
            throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' not found. Available: [{available}]");
        }

        Directory.CreateDirectory(_promptsDirectory);

        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        File.WriteAllText(filePath, content);
    }
}
