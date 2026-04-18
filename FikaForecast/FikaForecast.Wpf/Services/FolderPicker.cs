using System.IO;
using Microsoft.Win32;

namespace FikaForecast.Wpf.Services;

/// <summary>
/// WPF folder picker backed by <see cref="OpenFolderDialog"/> (available since .NET 8).
/// </summary>
public class FolderPicker : IFolderPicker
{
    public string? Pick(string? initialPath)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose export folder",
            InitialDirectory = !string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath)
                ? initialPath
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
