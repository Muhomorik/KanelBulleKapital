using DevExpress.Mvvm;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// Wraps a single prompt's editable state for settings UI binding.
/// Tracks dirty state by comparing current body to the last-saved snapshot.
/// </summary>
public class PromptSettingItem : ViewModelBase
{
    public string PromptKey { get; }
    public string DisplayName { get; }

    public string Body
    {
        get => GetValue<string>();
        set => SetValue(value, changedCallback: OnBodyChanged);
    }

    public string OriginalBody { get; private set; }

    public bool IsDirty => Body != OriginalBody;

    public PromptSettingItem(string promptKey, string displayName, string body)
    {
        PromptKey = promptKey;
        DisplayName = displayName;
        Body = body;
        OriginalBody = body;
    }

    private void OnBodyChanged() => RaisePropertyChanged(nameof(IsDirty));

    /// <summary>
    /// Updates the saved snapshot after a successful save or reset,
    /// clearing the dirty flag.
    /// </summary>
    public void ResetOriginal(string body)
    {
        OriginalBody = body;
        RaisePropertyChanged(nameof(IsDirty));
    }
}
