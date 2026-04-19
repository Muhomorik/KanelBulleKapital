namespace FikaForecast.Wpf.Services;

/// <summary>
/// Thin wrapper over the native folder-browse dialog so view models can be unit-tested.
/// </summary>
public interface IFolderPicker
{
    /// <summary>
    /// Prompts the user for a folder. Returns the selected path, or <c>null</c> if cancelled.
    /// </summary>
    /// <param name="initialPath">Optional starting location; falls back to <c>MyDocuments</c> if null or missing.</param>
    string? Pick(string? initialPath);
}
