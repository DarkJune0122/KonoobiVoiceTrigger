using VoiceTrigger.Extensions;
using Wpf.Ui.Controls;

namespace VoiceTrigger.Services;

public sealed class SnackbarService : Wpf.Ui.SnackbarService
{
    public static SnackbarService Instance { get; } = new();
    public void ShowLocalized(string key, params object?[] args)
    {
        ShowLocalized(key, ControlAppearance.Primary, null, DefaultTimeOut, args);
    }

    public void ShowLocalized(string key, ControlAppearance appearance, params object?[] args)
    {
        ShowLocalized(key, appearance, null, DefaultTimeOut, args);
    }

    public void ShowLocalized(string key, ControlAppearance appearance, IconElement? icon, params object?[] args)
    {
        ShowLocalized(key, appearance, icon, DefaultTimeOut, args);
    }

    public void ShowLocalized(string key, ControlAppearance appearance, IconElement? icon, TimeSpan timeout, params object?[] args)
    {
        string title = LocalizationResourceManager.Instance.GetValue($"Warning_{key}_Title");
        string content = string.Format(LocalizationResourceManager.Instance.GetValue($"Warning_{key}"), args);
        Show(title, content, appearance, icon, timeout);
    }
}
