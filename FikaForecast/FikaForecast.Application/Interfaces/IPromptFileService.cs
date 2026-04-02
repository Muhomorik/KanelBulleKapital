namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Handles reading and writing prompt files from the local filesystem.
/// On first access, copies embedded default prompts to the user's AppData folder.
/// </summary>
public interface IPromptFileService
{
    /// <summary>
    /// Reads a prompt file by its key (e.g., "newsbrief", "evaluation", "comparison").
    /// If the file does not exist on disk, copies the embedded default first.
    /// </summary>
    string ReadPromptFile(string promptKey);

    /// <summary>
    /// Writes prompt content to the file for the given key.
    /// Used when users edit prompts via the UI.
    /// </summary>
    void WritePromptFile(string promptKey, string content);

    /// <summary>
    /// Resets a prompt file to its embedded default.
    /// </summary>
    void ResetToDefault(string promptKey);
}
