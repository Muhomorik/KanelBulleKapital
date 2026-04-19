namespace FikaForecast.Wpf.Services;

/// <summary>
/// Thin wrapper over the native save-file dialog so view models can be unit-tested.
/// </summary>
public interface IFilePicker
{
    /// <summary>
    /// Prompts the user for a file path. Returns the selected path, or <c>null</c> if cancelled.
    /// </summary>
    /// <param name="initialPath">Optional starting path; filename is used as default and its directory as <c>InitialDirectory</c>.</param>
    /// <param name="title">Dialog title.</param>
    /// <param name="filter">Win32 filter string (e.g. <c>"SQLite database (*.db)|*.db|All files (*.*)|*.*"</c>).</param>
    /// <param name="defaultFileName">Filename used when <paramref name="initialPath"/> is null or empty.</param>
    string? Pick(string? initialPath, string title, string filter, string defaultFileName);
}
