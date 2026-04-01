using System.Windows;
using MahApps.Metro.Controls;

namespace FikaForecast.Wpf.Views;

public partial class ConfirmationDialog : MetroWindow
{
    public string Message { get; set; } = string.Empty;

    public ConfirmationDialog(string message)
    {
        InitializeComponent();
        Message = message;
        DataContext = this;
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
