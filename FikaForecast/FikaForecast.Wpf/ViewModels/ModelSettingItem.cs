using DevExpress.Mvvm;
using FikaForecast.Domain.ValueObjects;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// Wraps a <see cref="ModelConfig"/> with an <see cref="IsEnabled"/> flag for settings UI binding.
/// </summary>
public class ModelSettingItem : ViewModelBase
{
    public ModelConfig Model { get; }

    public bool IsEnabled
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public ModelSettingItem(ModelConfig model, bool isEnabled)
    {
        Model = model;
        IsEnabled = isEnabled;
    }
}
