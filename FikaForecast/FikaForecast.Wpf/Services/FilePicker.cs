using System.IO;
using Microsoft.Win32;

namespace FikaForecast.Wpf.Services;

/// <summary>
/// WPF file picker backed by <see cref="SaveFileDialog"/> so the user can either
/// select an existing file or type a new filename in a chosen folder.
/// </summary>
public class FilePicker : IFilePicker
{
    public string? Pick(string? initialPath, string title, string filter, string defaultFileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = filter,
            FileName = !string.IsNullOrWhiteSpace(initialPath)
                ? Path.GetFileName(initialPath)
                : defaultFileName,
            InitialDirectory = GetInitialDirectory(initialPath),
            OverwritePrompt = false,
            CheckFileExists = false,
            CheckPathExists = true,
            AddExtension = true
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static string GetInitialDirectory(string? initialPath)
    {
        if (!string.IsNullOrWhiteSpace(initialPath))
        {
            var dir = Path.GetDirectoryName(initialPath);
            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                return dir;
        }
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
}
